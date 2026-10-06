# ErtisAuth

**Open Source Identity and Access Management API**

ErtisAuth is a free and open-source OpenID-Connect framework and high performer identity and access management (IAM) API. It's designed to provide a common way to authenticate requests to all of your applications, whether they're web, native, mobile, or Web API endpoints. It is based on the RBAC (Role based access control) and UBAC (User based access control) models for authorization/access control. ErtisAuth incorporates features needed to integrate token-based authentication, SSO and API access control in your applications for authorization and authentication. Memberships are isolated tenants with their own users, user types, roles, applications and token settings; most endpoints are scoped by the membership id in the route. It is licensed under MIT License (an OSI approved license)

---

## Table of contents

- [Why ErtisAuth](#why-ertisauth)
- [Features](#features)
- [Core concepts](#core-concepts)
- [Permission model](#permission-model)
- [Getting started](#getting-started)
- [SDK](#sdk)
- [Tests](#tests)
- [Documentation](#documentation)
- [License](#license)

---

## Why ErtisAuth

| | ErtisAuth |
|---|---|
| **Lightweight** | One .NET service and one MongoDB database. No JVM, no separate admin server, no SQL schema migrations. |
| **API first** | Everything (memberships, users, roles, providers, hooks) is managed through the same REST API that issues the tokens, so it is easy to automate. |
| **Multi-tenant by design** | Each *membership* is an isolated tenant with its own users, roles, applications, secret key and settings. A token of one membership can never be used on another one. |
| **Flexible user model** | User types are defined at runtime with a JSON schema: add custom fields, validation, unique fields and inheritance without changing code. |
| **Fine-grained permissions** | A four-part permission expression (`subject.resource.action.object`) with wildcards, per-user overrides and deny rules. |
| **Built for .NET** | A typed SDK and ASP.NET Core integration with attribute based authorization, plus a Roslyn analyzer that checks your permission attributes at compile time. |
| **Free and open** | MIT licensed. No per-user pricing, no feature tiers, no vendor lock-in. |

### How it compares

- **Keycloak** is a full-featured server, but it is a heavy Java application whose configuration lives mostly in its admin console. ErtisAuth keeps a similar tenant idea (a *membership* plays the role of a Keycloak *realm*) in a much smaller .NET service that is configured entirely through its API.
- **Auth0, Okta and other hosted services** are managed for you, but they are paid per active user and your user data lives on a third-party platform. ErtisAuth is self-hosted: the data stays in your own MongoDB.
- **Duende IdentityServer** is a framework you build your own server with, and it needs a commercial license for most companies. ErtisAuth is a ready-to-run server under the MIT license.
- **ASP.NET Core Identity** is a library that you embed in each application. ErtisAuth is a central service: many applications, written in any language, share the same users and permissions.

---

## Features

### Authentication

- **Username or email + password sign-in** that returns an access token and a refresh token.
- **Bearer tokens (JWT)** for users and **Basic tokens** for applications (machine-to-machine access), each application with its own secret that can be rotated.
- **Refresh tokens** that are usable once by default, and **token revocation**, for one session or for all devices of a user (`logout-all`).
- **Scoped tokens:** a signed-in user can request a token limited to a subset of their permissions, for example to hand it to a less trusted client.
- **Active token tracking:** every issued token is stored with the client's IP address and user agent, so active sessions can be listed and revoked.
- **Token verification endpoint** for services that prefer to ask ErtisAuth instead of validating tokens themselves.
- **Code flow for devices without a keyboard** (smart TVs, kiosks, CLI tools): the device shows a short code, a signed-in user approves it, and the device exchanges it for a token. The code format (length, letters/digits, lifetime) is set by a *code policy*.

### External Identity Providers

- Sign in or sign up with **Google**, **Apple** (web and native), **Facebook** (including Limited Login) and **Microsoft**.
- Provider tokens are validated on the server against the provider; the identity sent by the client is not trusted on its own.
- An existing account is linked by email address only when the provider reports the email as verified, or when the provider is explicitly configured to trust its emails (`trust_email`).
- Several providers of the same type can be configured in one membership (for example one Google client per app). Each has its own login URL: `oauth/{slug}/login`.
- Users who sign in through a provider get the provider's default role and user type, and they can have several connected accounts.

### Account Lifecycle

- **Email activation:** a membership can require new users to activate their account from an activation mail.
- **Password reset** by email with a single-use reset token.
- **One-time passwords (OTP)** with a configurable length, character set, lifetime and maximum number of attempts, generated with a cryptographically secure random number generator.
- **Password change** signs the user out on every other device.
- **Freeze** a user to deactivate the account and revoke all of their tokens at once.

### Password Storage

- Modern password hashing with **Argon2id** and **PBKDF2** (SHA-256 / SHA-512).
- The hash algorithm is chosen per membership. Older algorithms (SHA-2, SHA-3 and legacy ones) are still supported so that existing user databases can be imported without forcing every user to reset their password.

### Users and User Types

- **Dynamic user types:** define the custom fields of your users with a JSON schema (types, required fields, validation rules, default values), and the API validates every create and update request against it.
- **User type inheritance:** a user type can extend another one and inherit its fields.
- **Unique fields:** fields marked as unique are backed by MongoDB unique indexes per membership, created and removed automatically when the schema changes.
- **Rich querying:** list with pagination and sorting, full-text search, and MongoDB queries and aggregations through `_query` and `_aggregate` endpoints. Dangerous operators such as `$where` and `$function` are rejected, and hidden fields such as password hashes can never be read.

### Events, Webhooks and Mailhooks

- **Event log:** token, user, role, application, provider and hook operations are recorded as events with the user or application that caused them.
- **Webhooks:** call any HTTP endpoint when an event occurs, with custom headers and retry attempts. Calls are sent from a background queue, so they never slow down the API.
- **Mail hooks:** send templated emails on events (for example a welcome mail on `UserCreated`), to the user who triggered the event and/or fixed recipients.
- **Mail providers:** SMTP, SendGrid and Mailchimp (Mandrill), configured per membership.

### Operations

- **Interactive API reference** with OpenAPI and [Scalar](https://scalar.com), available at `/docs` in the development environment.
- **Prometheus metrics** at `/metrics` for HTTP traffic and outgoing calls.
- **Azure Application Insights** support through OpenTelemetry.
- **Health check** endpoint for load balancers and Kubernetes probes.
- **One-time setup endpoint** that creates the first membership, administrator role, user type, administrator user and (optionally) an application on a fresh installation.
- Brotli/Gzip response compression and graceful shutdown.

---

## Core Concepts

| Concept | Description |
|---|---|
| **Membership** | An isolated tenant, similar to a *realm* in Keycloak. It holds its own users, roles, applications, providers and hooks, and its own settings: token lifetimes, hash algorithm, secret key, mail providers, activation and OTP policies. All tenant endpoints live under `memberships/{membershipId}/...`. |
| **User** | A person who signs in. A user has a role, a user type, and optional extra permissions and forbidden permissions (UBAC). |
| **User type** | The schema of a user's custom fields. User types can inherit from each other. |
| **Role** | A named set of permissions and forbidden permissions, shared by users and applications. |
| **Application** | A machine client that authenticates with a Basic token (application id + secret). Applications also have a role and can have their own permissions. |
| **Provider** | An external identity provider configuration (Google, Apple, Facebook, Microsoft). |
| **Token code / code policy** | The short codes of the device sign-in flow, and the rules used to generate them. |
| **Event** | A record of something that happened in the membership. Webhooks and mail hooks are triggered by events. |

---

## Permission Model

A permission is an expression with four segments:

```
subject.resource.action.object
```

| Segment | Meaning | Example |
|---|---|---|
| `subject` | Who performs the action (a user or application id) | `*` |
| `resource` | The type of resource | `users` |
| `action` | `create`, `read`, `update`, `delete` or a custom action | `read` |
| `object` | The id of a single resource | `*` |

`*` matches anything, and shorter forms are allowed: `users` means every action on users, `users.read` means reading any user.

```json
{
	"name": "editor",
	"permissions": [ "*.users.read.*", "*.roles.read.*", "*.users.update.*" ],
	"forbidden": [ "*.users.delete.*" ]
}
```

When a request is authorized, ErtisAuth decides in this order:

1. A matching **user or application permission** (UBAC) decides, whether it grants or forbids.
2. Otherwise the **role** decides; a forbidden entry of the role always wins over a permission.
3. Otherwise users may still read and update **their own record**, unless the role forbids it.
4. Finally, if the token is a **scoped token**, its scopes must also cover the request.

Resources and actions are not limited to ErtisAuth's own endpoints: you can use the same permissions for your own APIs (see below).

---

## Getting Started

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [MongoDB](https://www.mongodb.com/) 7.0 or later

### Build and run from source

```shell
git clone https://github.com/ertugrulozcan/ErtisAuth.git
cd ErtisAuth
dotnet build
dotnet run --project ErtisAuth.WebAPI
```

The MongoDB connection is set with the `Database__ConnectionString` environment variable (or the `Database:ConnectionString` setting):

```shell
export Database__ConnectionString="mongodb://<username>:<password>@<host>:27017"
```

In the development environment the API listens on `http://localhost:9716` and the API reference opens at `http://localhost:9716/docs`.

### Run with Docker

```shell
docker run -p 9716:80 -e Database__ConnectionString=<connection_string> ertugrulozcan/ertisauth:latest
```

### Setup

A new installation has no membership and no user yet, so there is nothing to sign in with. The setup endpoint creates the first resources, and it is authorized with a setup token that you insert into the database yourself (database access proves that you are the owner). The setup can run only once.

1. Generate a token of at least 32 characters and insert it into the `setup` collection:

   ```shell
   openssl rand -hex 32
   ```

   ```javascript
   db.setup.insertOne({ token: "<setup_token>" })
   ```

2. Call the setup endpoint:

   ```shell
   curl -X POST '<base_url>/setup' \
     -H 'X-Setup-Token: <setup_token>' \
     -H 'Content-Type: application/json' \
     -d '{
       "membership": {
         "name": "my-membership",
         "expires_in": 43200,
         "refresh_token_expires_in": 86400,
         "hash_algorithm": "ARGON2ID",
         "encoding": "UTF-8"
       },
       "user": {
         "username": "admin",
         "firstname": "Admin",
         "lastname": "User",
         "email_address": "admin@example.com",
         "password": "<password>",
         "user_type": "User"
       },
       "application": {
         "name": "my-backend",
         "role": "admin"
       }
     }'
   ```

   When the setup completes, the `setup` collection is deleted.

### Get your first token

```shell
curl -X POST '<base_url>/generate-token' \
  -H 'X-Ertis-Alias: <membership_id>' \
  -H 'Content-Type: application/json' \
  -d '{ "username": "admin", "password": "<password>" }'
```

```json
{
	"token_type": "bearer",
	"access_token": "<access_token>",
	"expires_in": 43200,
	"refresh_token": "<refresh_token>",
	"refresh_token_expires_in": 86400,
	"created_at": "2026-01-01T12:00:00Z"
}
```

Use it on any endpoint:

```shell
curl '<base_url>/me' -H 'Authorization: Bearer <access_token>'
```

---

## SDK

### Protecting your own APIs with the ErtisAuth SDK

`ErtisAuth.Sdk` is a typed .NET client for the ErtisAuth API, and `ErtisAuth.Sdk.AspNetCore` adds authentication and permission checks to your own ASP.NET Core services.

```json
{
	"ErtisAuth": {
		"BaseUrl": "https://auth.example.com",
		"MembershipId": "<membership_id>"
	}
}
```

```csharp
builder.Services.AddErtisAuth();
```

```csharp
[Authorized]
[RbacResource("orders")]
[Route("orders")]
public class OrdersController : ControllerBase
{
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	public IActionResult Get(string id)
	{
		// Reached only when the caller's token grants "*.orders.read.{id}"
		...
	}
}
```

The placeholders in the rbac attributes can read route values (`{id}` or `{route::id}`), query parameters (`{query::id}`), headers (`{header::X-Name}`) and environment variables (`{env::NAME}`). The bundled Roslyn analyzer reports invalid placeholders at compile time, before they can turn into authorization bugs at runtime.

---

## Tests

Run the tests with:

```shell
dotnet test
```

---

## Documentation

The developer guide and the endpoint reference are on the [wiki](https://github.com/ertugrulozcan/ErtisAuth/wiki). The interactive API reference of a running instance is available at `/docs` in the development environment.

## License

ErtisAuth is released under the [MIT License](LICENSE). Copyright © 2020 Ertuğrul Özcan.
