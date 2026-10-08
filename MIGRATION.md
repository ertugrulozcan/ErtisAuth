# Migrating to ErtisAuth on .NET 10

This guide covers the upgrade from the previous ErtisAuth release (.NET 9, `master` branch) to the .NET 10 release. It is written for three groups:

- **Operators / DevOps:** database migration, deployment and configuration ([sections 1-3](#1-before-you-deploy-database)).
- **Client application developers:** API behavior and contract changes ([section 4](#4-api-changes-for-client-applications)).
- **SDK users:** `ErtisAuth.Sdk` and the new `ErtisAuth.Sdk.AspNetCore` package ([section 5](#5-sdk-changes)).

The release is mainly a security and correctness release. Several fixes change behavior that clients may rely on, and some stored data must be migrated **before** the new version starts. Read sections 1 and 4 completely before you plan the rollout.

## Upgrade at a glance

1. **Back up the database.**
2. Run the [pre-deploy checks and migrations](#1-before-you-deploy-database). Some of them are required: without them provider logins, token refresh or role-less accounts fail after the deploy.
3. Update clients that are affected by the [API changes](#4-api-changes-for-client-applications). Some of them (device code flow, provider login routes, OTP) must ship together with the server.
4. Update the [deployment](#2-deployment-and-configuration): container port 8080, non-root user, health probes, new project paths.
5. Deploy, then work through the [post-deploy steps](#3-after-you-deploy).

Some side effects are accepted and need no action; see [Known side effects](#6-known-side-effects-of-the-upgrade).

---

## 1. Before you deploy: database

The scripts below are written for `mongosh` and run against the ErtisAuth database. Run the `find`/`aggregate` checks first and review their output before running any `update`/`delete`.

### 1.1 Required migrations

#### Memberships: `hash_algorithm` is required

There is no implicit SHA2-256 fallback anymore. Set `hash_algorithm` explicitly on every membership that lacks it, using the algorithm its existing password hashes were created with:

```js
db.memberships.find({ $or: [{ hash_algorithm: { $exists: false } }, { hash_algorithm: null }] }, { name: 1 })
```

#### Providers: discriminator `type`

Every provider document needs the discriminator field `type` (`Apple`, `AppleNative`, `Facebook`, `Google` or `Microsoft`). Without it, listing providers and logging in through them answer 500. If your provider names equal these values (verify first), copy them over:

```js
db.providers.find({ type: { $exists: false } }, { name: 1, membership_id: 1 })
db.providers.updateMany({ type: { $exists: false } }, [{ $set: { type: "$name" } }])
```

Providers are no longer created automatically for new memberships. Decide whether to delete inactive default provider documents created by older versions.

#### Providers: slugs

Provider login now goes through `POST /oauth/{slug}/login` ([details](#provider-login)). Give each provider a slug that is unique per membership. To keep existing web and Android clients working without changes, use `facebook`, `google`, `microsoft` and `apple`. The AppleNative provider needs its own slug, for example `applenative`. A unique index on `{ membership_id, slug }` is created at startup. If duplicates exist, the index is not created and an error is logged.

#### Providers: `trust_email`

Providers have a new flag, `trust_email` (missing = `false`). When an external login returns an email that the provider does not mark as verified, ErtisAuth links it to an existing user only if `trust_email` is `true`. Otherwise it answers `409 ProviderEmailNotTrusted`. Set `trust_email: true` on every provider whose email-based account linking must keep working. Microsoft and Facebook need it; for Google and Apple it is recommended if you want no behavior change.

#### Users: `connected_accounts` shape

`connected_accounts` entries change from `{ Provider, UserId, Token }` to `{ provider, slug, user_id, token }`. Login matches on provider + user id, and logout revokes by slug, so this migration must run **before** the deploy. Adjust the AppleNative slug to the one you chose above:

```js
db.users.updateMany(
	{ "connected_accounts.0": { $exists: true } },
	[{ $set: { connected_accounts: { $map: {
		input: "$connected_accounts",
		as: "a",
		in: {
			provider: "$$a.Provider",
			slug: { $cond: [{ $eq: ["$$a.Provider", "AppleNative"] }, "applenative", { $toLower: "$$a.Provider" }] },
			user_id: "$$a.UserId",
			token: "$$a.Token"
		}
	} } } }]
)
```

Older AppleNative logins also stored a duplicate `Apple` entry. You may remove `Apple` entries whose `user_id` equals the user's `AppleNative` entry.

#### User types: `connected_accounts` definition

Stored user types keep a copy of the base user fields, including the old `connected_accounts` definition. Users of those types answer 400 on provider login until the user types are migrated. Run this together with the users migration above:

```js
db["user-types"].updateMany(
	{ "properties.connected_accounts": { $exists: true } },
	{ $set: {
		"properties.connected_accounts.itemSchema.properties": {
			provider: { type: "string", isRequired: true },
			slug: { type: "string", isRequired: true },
			user_id: { type: "string", isRequired: true },
			token: { type: "string" }
		},
		"properties.connected_accounts.uniqueBy": ["slug"]
	} }
)
```

#### Users and applications without a role

Previously, a user or application with an empty role **skipped** the permission check, which gave it full access. Now it is denied with `403` on every authorized endpoint. Find these accounts and assign the right role before the deploy:

```js
db.users.find({ $or: [{ role: { $exists: false } }, { role: null }, { role: "" }] }, { username: 1, membership_id: 1 })
db.applications.find({ $or: [{ role: { $exists: false } }, { role: null }, { role: "" }] }, { name: 1, membership_id: 1 })
```

#### Active tokens: legacy geo location

`client_info.geo_location` was removed. Session documents that still contain it cannot be read, so refresh and "log out from all devices" answer 500 for sessions created before the deploy:

```js
db.active_tokens.updateMany({ "client_info.geo_location": { $exists: true } }, { $unset: { "client_info.geo_location": "" } })
```

#### Device codes and OTPs: drop the old collections

Both features were redesigned. Their records are short-lived, and the old documents and indexes are incompatible. The new indexes are created at startup.

```js
db.codes.drop()
db.otps.drop()
```

#### Events: `is_custom_event`

```js
db.events.updateMany({ is_custom_event: { $exists: true } }, { $unset: { is_custom_event: "" } })
```

### 1.2 Checks that may need data fixes

#### Duplicate emails, usernames and unique fields

At startup ErtisAuth creates the unique partial indexes `ux_email_address` and `ux_username` on `users` (per membership, non-empty strings only). It also creates one index per custom field marked `isUnique`. If duplicates exist, that index is **not** created: the error is logged, the application still starts, and only the application-level check protects the field. Find and clean duplicates first:

```js
db.users.aggregate([
	{ $match: { email_address: { $type: "string", $gt: "" } } },
	{ $group: { _id: { m: "$membership_id", v: "$email_address" }, n: { $sum: 1 }, ids: { $push: "$_id" } } },
	{ $match: { n: { $gt: 1 } } }
], { allowDiskUse: true })
```

Repeat with `username`. For custom unique fields, add `user_type: { $in: [<declaring type and its descendants>] }` to `$match`.

#### Device code policies

Policies are validated on create and update: `length` must be 5-12, `expires_in` 1-1800 seconds, and at least one of `contains_letters`/`contains_digits` must be set. Existing policies that break these rules keep working but cannot be saved until fixed:

```js
db["code-policies"].find({ $or: [
	{ length: { $lt: 5 } }, { length: { $gt: 12 } },
	{ expires_in: { $lt: 1 } }, { expires_in: { $gt: 1800 } },
	{ contains_letters: { $ne: true }, contains_digits: { $ne: true } }
] })
```

#### Orphaned user types

Look for user types whose `baseType` names a user type that no longer exists. A user type that is the base type of another one can no longer be deleted.

#### Mail hooks with missing fields

Mail hooks now require `mailSubject`, `fromName`, `fromAddress` and `recipients` (unless `sendToUtilizer` is set) on create as well as update. Existing mail hooks missing them never sent mail and could not be updated; fix or delete them.

#### Admin roles

New installations give the admin role CRUD permissions for `code-policies` and `otp`. Existing admin roles lack them. Add them if your admins manage these resources.

#### Roles that generate one-time passwords

`GET users/{id}/generate-otp` now checks `otp.create.{id}`, as documented. Before, it checked `users.create.{id}` by mistake, so every role that could create users could also generate the code of any user and reset their password.

Before you deploy, give `otp.create` to every role that generates one-time passwords, **including existing admin roles** (see above). Roles that only had `users.create` lose this access, which is intended. To find the roles that could generate codes until now:

```javascript
db.roles.find({ permissions: { $regex: "users\\.(create|\\*)" } }, { name: 1, slug: 1, membership_id: 1, permissions: 1 })
```

Users and applications can also hold `users.create` in their own `permissions`; check them the same way if you use per-user permissions.

### 1.3 Recommended cleanup

#### Events containing secrets

Events written by earlier versions may contain secrets:

| Event | Field |
|---|---|
| `UserPasswordChanged` | `prior.password_hash` |
| `TokenGenerated` | access and refresh tokens |
| `TokenVerified` | the bearer token, and `basicToken` (`appId:secret`; for legacy applications this is the **membership secret key**) |
| `TokenRefreshed` | the new access and refresh tokens, and the used refresh token |
| `TokenRevoked` | the access token |
| `Provider*` | `privateKey` |

New events carry token metadata only. Remove the secret fields from old events or delete those events, for example:

```js
db.events.updateMany({ event_type: { $in: ["TokenRefreshed", "TokenRevoked"] } }, { $unset: { document: "", prior: "" } })
```

If anyone who is not fully trusted could read the events (`events.read`, webhooks), rotate the affected membership secret keys and Apple keys.

#### Legacy token documents without `retain_until`

Legacy `active_tokens` and `revoked_tokens` documents have no `retain_until`, so the TTL index never removes them. Delete them or backfill `retain_until`.

#### Stale indexes

Index creation at startup only adds indexes. Drop indexes left over from the previous version, for example on `revoked_tokens` over `token.access_token`.

---

## 2. Deployment and configuration

### Container image

| | Previous | .NET 10 |
|---|---|---|
| Base image | `aspnet:9.0-alpine` | `aspnet:10.0` (Debian) |
| User | root | non-root `app` (uid 1654) |
| Port | 80 | **8080** |

- Update the Kubernetes `containerPort`/`targetPort` and the liveness/readiness probes (`GET /ping`, `GET /healthcheck`) to port 8080.
- The images are Debian-based on purpose: they ship ICU and tzdata, so culture, date and time zone handling behave as on a development machine. Do not switch to Alpine.
- `appsettings.Development.json` is not copied into the image.
- Two startup warnings are expected and harmless: DataProtection keys not persisted (ErtisAuth does not use data protection), and "Failed to determine the https port" (TLS terminates at the ingress).

### Repository layout

Projects moved into `src/` and test projects into `tests/`. The `Dockerfile` stays at the repository root and builds `src/ErtisAuth.WebAPI/ErtisAuth.WebAPI.csproj`. Update any pipeline step that refers to project paths directly.

### CI

`ErtisAuth.IntegrationTests` needs Docker (Testcontainers). Where Docker is not available, exclude the tests with the trait `Category=Integration`.

### Application secrets

Each application now has its own Basic secret. Existing memberships keep accepting the membership secret for applications through the legacy switch `AllowMembershipSecretForApplications` (missing = on). After the deploy:

1. Rotate the secret of every application with `POST /memberships/{membershipId}/applications/{id}/secret`.
2. Update the clients with the new secrets.
3. Turn the legacy switch off for the membership.

New applications receive their secret **only once**, in the create response.

### Membership secret key length

A membership `secret_key` must be at least 32 bytes in the membership's encoding (create, update and setup answer 400 otherwise). Existing memberships that can sign tokens already meet this.

### Setup endpoint

`POST /migrate` is now `POST /setup`. The `ConnectionString` header is replaced by `X-Setup-Token`: a token of at least 32 characters that the operator inserts into the `setup` collection beforehand. This only affects new installations and install scripts.

### Custom hosts

If you host the WebAPI from your own `Program.cs`, remove `app.UseProviders()`. The authenticator factory is a DI service now.

### MongoDB and Linux kernels

MongoDB 8.0 refuses to start on Linux kernels 6.19 and newer (SERVER-121912). Before upgrading node or host kernels, upgrade MongoDB (8.2 works).

### Rate limiting

Consider a rate limit at the ingress (for example Istio/Envoy) on the anonymous device token endpoint `POST /memberships/{membershipId}/codes/token`.

---

## 3. After you deploy

- **Check the startup log** for `could not be created on users collection` and other index errors. Index creation on `users` failed on every startup of the previous version, so missing indexes are created now. Text indexes are also built on `roles`, `applications` and `memberships`; large collections take time.
- **Webhooks resume.** Before this release no webhook was sent on .NET 10 builds, and custom mail hooks sent unresolved `{{document.x}}` placeholders. Make sure webhook receivers are ready for traffic.
- **Backfill `sys`.** Roles, applications, memberships, user types, webhooks, mail hooks, providers and code policies created or updated by pre-release .NET 10 builds may have `sys: null`. For documents without `sys.created_at`, set `created_at` from the `_id` ObjectId timestamp and `created_by` to `"system"`.
- **Rotate application secrets** and turn off the legacy switch ([Application secrets](#application-secrets)).
- **Clean up events** that contain secrets ([1.3](#events-containing-secrets)), if you have not done so before the deploy.

---

## 4. API changes for client applications

### Authentication and error responses

- A missing `Authorization` header answers **401** `AuthorizationHeaderMissing` (was 400). Every 401 carries `WWW-Authenticate: Bearer realm="ErtisAuth"`.
- Database or infrastructure errors during authentication answer **500** (were 401).
- 500 responses carry a generic message ("An unexpected error occurred"); details are only in the server logs.
- The multi-field validation error (`errorCode: "ValidationException"`, for example invalid fields or a duplicate username, email or unique field) is camelCase like every other error:
  ```json
  { "message": "...", "errorCode": "ValidationException", "statusCode": 400, "errors": [{ "message": "...", "fieldName": "...", "fieldPath": "..." }] }
  ```
  It was `Message`, `ErrorCode`, `StatusCode`, `Errors`.
- Removed error codes: `InvalidTokenCode`, `ApplicationSecretMismatch`, `UserTypePropertiesRequired`, `CommandRequired`. Update clients that check for them.
- Changed error codes:

  | Situation | Before | Now |
  |---|---|---|
  | Unknown device code | 401 `InvalidTokenCode` | 404 `TokenCodeNotFound` |
  | Delete missing membership | `RoleNotFound` | `MembershipNotFound` |
  | Delete missing application | `RoleNotFound` | `ApplicationNotFound` |
  | Get missing provider | `ApplicationNotFound` | `ProviderNotFound` |
  | Role with the same permission in permitted and forbidden | 400 `ModelValidationError` | 409 `RbacsConflicted` |
  | Invalid JSON body | 500 | 400 |

### Membership isolation and authorization

- Requests whose route membership differs from the token's membership answer **403**.
- `_query` filters are always limited to the route membership.
- Users and applications without a role are denied with 403 ([1.1](#users-and-applications-without-a-role)).
- Scoped bearer tokens are now actually limited to their scopes on ErtisAuth endpoints; calls outside the scopes answer 403.
- `GET roles/check-permission` only needs a valid token (no `roles.read`). An application can read its own record without `applications.read`.

### Tokens and sessions

- The refresh token JWT claim is a JSON boolean, `"refresh_token": true` (was the string `"True"`). Clients that decode the JWT and compare strings must adapt. Old tokens are still accepted.
- `change-password` and `set-password` (reset flow) revoke all of the user's access and refresh tokens. A user who changes their own password keeps the calling session. Expect re-login on other devices.
- `active-tokens/_aggregate` accepts only an allowlisted set of pipeline stages; other stages answer 400.

### Password reset and account activation

- `reset-password` must send the host in the `X-Host` header.
- Reset password and activation links expire exactly at their expiry time (they were accepted up to 5 minutes longer).
- `resend-activation-mail` answers `200 { emailAddress }` on success (the previous behavior was inverted and answered 401 on success).
- Reusing the activation link of an already active user answers `UserAlreadyActive`.

### One-time passwords (OTP)

- `GET users/{id}/generate-otp` requires `otp.create.{id}` (was `users.create.{id}` by mistake; see [Roles that generate one-time passwords](#roles-that-generate-one-time-passwords)).
- `GET users/{id}/generate-otp` no longer returns `token`. The response is `{ _id, user_id, email_address, username, password, expires_in, created_at, expire_time, membership_id }`.
- The reset token is issued by `POST verify-otp` (response unchanged). Its lifetime starts at verification.
- A code works **once**: a second verify answers `401 InvalidCredentials`.
- `generate-otp` always issues a new code and invalidates the previous one (before, it returned the active code again).
- Codes are case-insensitive. Letter-and-digit codes no longer contain `0`, `O`, `1` or `I`.
- Membership OTP policies have `max_attempts` (default 5).
- Rotating the membership secret invalidates active OTPs.

### Device code flow

The flow was redesigned along the lines of RFC 8628. The old routes are **removed**; device apps and the approval page must switch together with the server.

| Step | Before | Now |
|---|---|---|
| Create code | `POST codes` → 200 `{ code, ... }` | `POST codes` → **201** `{ user_code, device_code, status, expires_in, interval, created_at, expire_time, client_info }` |
| Device polls | `GET codes/generate-token/{code}` | `POST codes/token` with `{ "device_code": "..." }` every `interval` seconds |
| Show code info | | `GET codes/{user_code}` |
| Approve | `GET codes/approve/{code}` | `POST codes/{user_code}/approve` |
| Deny | | `POST codes/{user_code}/deny` |

All routes are under `/memberships/{membershipId}/`.

- `device_code` is returned only on creation, and the token is handed out once. Device apps must store it on the first success.
- New errors: `400 TokenCodeSlowDown` (polling too fast), `401 TokenCodeDenied`, `401 UserInactive`, `401 TokenCodeExpired`, `409 TokenCodeAlreadyAuthorized`.
- Case, dashes and spaces in the user code are ignored.
- If a backend creates codes on behalf of a device, forward the device's `X-IpAddress` and `X-UserAgent`.
- A code approved with a scoped token gives the device a token with the same scopes (lifetime `scoped_token_expires_in`; refresh keeps the scopes).
- A code policy in use by the membership cannot be deleted (`409 TokenCodePolicyInUse`) and keeps its slug when renamed.

### Provider login

- The login route is `POST /oauth/{slug}/login`. The fixed routes `oauth/facebook|google|microsoft|apple/login` and the `?platform=` parameter are removed. `limited_flow=true` stays for Facebook.
- **iOS apps** must call the AppleNative provider's slug (for example `oauth/applenative/login`). A client that still sends `oauth/apple/login?platform=ios` is not rejected: the parameter is ignored and the login goes to the web Apple provider, where the native code exchange fails (401/501).
- Web and Android clients can drop `?platform=`; it is ignored.
- Facebook classic login reads the profile from the Graph API, so the email comes from Facebook, not from the client. Users who did not grant the email permission have no email: creating a new user fails for them, while already linked users are unaffected.
- Apple identity comes from Apple's `id_token`, not from the client.
- Google: an invalid token answers 401 (was 500). A missing email or name answers 401 `ProviderProfileIncomplete`. Clients must request the `email` and `profile` scopes.
- Apple: `503 ProviderUnavailable` when Apple cannot be reached, and `ProviderNotConfiguredCorrectly` for provider configuration or credential errors (both were 401).
- `409 ProviderEmailNotTrusted` when an unverified email from an untrusted provider matches an existing user ([trust_email](#providers-trust_email)).
- New `400 InvalidProviderLoginRequest`, also for an unreadable Apple `id_token` (was 500). `UnknownPlatform` was removed.
- `users.connected_accounts` entries are `{ provider, slug, user_id, token }` in `/users` and `/me` responses as well.

### Provider management (admin)

- `POST /providers` creates a provider and answers 201. The JSON field `type` selects the provider type and is required (`400 ProviderTypeRequired`, `400 UnsupportedProvider`).
- The slug cannot be changed (`400 ProviderSlugCannotBeChanged`) and is unique per membership.
- `PUT providers/{id}` without `privateKey` keeps the stored key (it was cleared).
- `active-providers` returns `slug` and `type`.
- Error codes `ProviderNameRequired` and `UnknownProvider` were removed.

### Queries (`_query`, `_aggregate`, search)

- The `_query` body is parsed as **strict JSON**. Relaxed syntax that was accepted before, such as unquoted names (`{ where: { age: { $gt: 18 } } }`) or single-quoted strings, answers `400 InvalidQuery`. The output of `Ertis.MongoDB.Queries` `QueryBuilder.ToString()` is MongoDB shell syntax and is rejected too.
- `users` and `applications` queries cannot filter or sort on `password_hash`/`secret_hash`, or use `$expr`/`$jsonSchema`.
- `$where`, `$function` and `$accumulator` are rejected everywhere.
- Role and application search is limited to the membership and case-insensitive. `GET /memberships/search` works again.

### Users and user types

- A user type that is the base type of another one cannot be deleted (`400 UserTypeCanNotBeDelete`) and keeps its slug when renamed.
- Making a field unique while duplicates exist answers `409 UniqueFieldHasDuplicates`, and the user type is not saved.
- Custom unique fields are unique among the declaring type and its descendants (before: among all users of the membership).
- Concurrent duplicate writes answer a 400 unique validation error.
- A multiple reference field without `contentType` keeps the given ids (it was saved as `[]`). Ids lost this way cannot be recovered from the database.

### REST resources

- Create and update of code policies, mail hooks and user types no longer require `_id`/`membership_id` in the body; they come from the route.
- `PUT mailhooks/{id}` and `PUT memberships/{id}` update the route id; a body `_id` is ignored. `sys` in the membership body is ignored.
- `POST /memberships` does not require `_id`.
- Mail hooks require `mailSubject`, `fromName`, `fromAddress` and `recipients` on create (400 `ModelValidationError`).

### Events, webhooks and mail hooks

- `document` and `prior` of stored events are plain JSON: `_id` is a string and dates are ISO strings (not `{"$oid"}`/`{"$date"}` wrappers).
- Token events carry metadata only:
  - `TokenGenerated`/`TokenVerified`: `token` has `token_type`, expiry fields and `application_id`.
  - `TokenRefreshed`: `document` is `{ user, token: { token_type, expires_in, refresh_token_expires_in, created_at } }`, and `prior` is empty.
  - `TokenRevoked`: `document` is `{ token: { token_type, expire_time } }`.

  Templates and webhooks that read token strings must change.
- `UserPasswordReset` no longer contains `membership`; use the event's `membership_id`.
- `WebhookRequestSent`/`WebhookRequestFailed`: `exception` is `{ type, message }` (no stack trace), and `response` is `{ isSuccess, statusCode, body }`. Network failures are retried and recorded.
- Mail hooks: values inserted into the mail body are HTML-encoded, and line breaks are removed from subjects.
- Webhooks: header values are sent unencoded, values in templated URLs are URL-escaped, and body values cannot change the JSON structure.

---

## 5. SDK changes

`ErtisAuth.Sdk` gets a new major version.

- **Sync methods were removed.** Use the async versions.
- **New package `ErtisAuth.Sdk.AspNetCore`.** The ASP.NET Core integration moved there (namespaces `ErtisAuth.Sdk.AspNetCore.*`).
- **`ResetPasswordAsync(email, host, token)`:** the `server` parameter was removed, the method returns `IResponseResult`, and the host is sent in the `X-Host` header (the old SDK always failed here).
- **`GetActiveTokensAsync`/`GetRevokedTokensAsync`** work against the .NET 10 API (they answered 400).
- **`RoleService.CheckPermission*`** throws on 5xx instead of returning `false`.
- **The authorization scheme** is matched case-insensitively.

### Client applications using `ErtisAuth.Sdk.AspNetCore`

- A missing `Authorization` header answers **401** `AuthorizationHeaderMissing`.
- When ErtisAuth is unreachable or answers 5xx, the client application answers **503** `AuthenticationServiceUnavailable` (was 401/403).
- **Rbac placeholders** in the `RbacSubject`, `RbacResource`, `RbacAction` and `RbacObject` attributes now support `{route::x}`, `{query::x}`, `{header::X}` and `{env::X}`. Unprefixed `{x}` is a route value, as before.
  - A missing or empty `{env::X}` now answers **403**. Before, it was silently removed: `{env::organization}:advertisers` became `:advertisers`. Make sure every environment variable used in rbac attributes is defined in each deployment.
  - An unknown prefix answers 403 and is reported by analyzer `ERTISAUTH602`.
  - `RbacSubject` with a query or header source answers 403 and is reported by `ERTISAUTH603`.
  - The analyzers ship in the package.
  - Resolved placeholder values are rejected with 403 if they are exactly `*` or `__all__`, contain `%2E` (any case), or contain whitespace or control characters. This applies to ErtisAuth's own endpoints as well, so check clients whose route, query or header values may contain spaces.
- **`AddErtisAuth()` reads its settings from the configuration of the host** (`builder.Configuration`), so every source of the application now applies: user secrets, command line arguments, Azure Key Vault, and `appsettings.{environment}.json` for the host's environment (also `DOTNET_ENVIRONMENT`). Before, the SDK read only `appsettings.json`, `appsettings.{ASPNETCORE_ENVIRONMENT}.json` from the output directory, and environment variables. Invalid settings now fail when the application starts instead of at `AddErtisAuth()`. Applications without a host keep the old behavior. If the settings live only in `appsettings.json` or in environment variables, nothing changes.
- **New overload `AddErtisAuth(IConfiguration)`** reads a given section, for example `builder.Services.AddErtisAuth(builder.Configuration.GetSection("Identity"))`.
- **`GetUtilizer()` returns only the caller authenticated by ErtisAuth.** The `fallbackByToken` parameter was removed. On endpoints that are not authenticated (`[Unauthorized]`, or no ErtisAuth attribute), `GetUtilizer()` now returns `null`. Before, it read the token of the request without verifying it, so anyone could claim any identity with a self-made token. Code that needs the old behavior must call **`GetUnverifiedUtilizer()`**, which works like `GetUtilizer(fallbackByToken: true)` did. Never use its result for authorization decisions. Calls with `fallbackByToken: false` no longer compile; remove the argument.
- **An rbac attribute of an action overrides the same attribute of its controller.** Before, the controller's `[RbacResource]` won, so an action with its own `[RbacResource]` was checked against the controller's resource. Check the permissions of the roles that call such actions.
- **`[Unauthorized]` on a controller no longer wins over its actions.** The most specific of `[Authorized]`, `[SelfAuthorized]` and `[Unauthorized]` applies, so a `[SelfAuthorized]` action of an `[Unauthorized]` controller now requires a token. Before, such an action was public. Clients that called it without a token now get **401**.
- **New analyzer rules** report authorization mistakes while you build:
  - `ERTISAUTH610` (warning): an action has rbac attributes, but neither the action nor its controller has `[Authorized]` or `[SelfAuthorized]`, so the endpoint is public.
  - `ERTISAUTH611` (warning): conflicting `[Authorized]`, `[SelfAuthorized]` and `[Unauthorized]` on the same level.
  - `ERTISAUTH612` (info): the rbac attributes of a `[SelfAuthorized]` or `[Unauthorized]` action are not checked.
  - `ERTISAUTH613` (warning): an action that is not authenticated calls `GetUtilizer()`, which always returns `null` there. It finds the calls affected by the `GetUtilizer()` change below.

---

## 6. Known side effects of the upgrade

These are accepted and need no action beyond informing users:

- Reset password and activation links issued before the deploy stop working. Users must request new links.
- Tokens revoked before the deploy are valid again until they expire.
- Device codes and OTPs that are open at deploy time are lost (the collections are dropped).
- Users must log in again on other devices after changing their password.
