# ErtisAuth

### **Open Source Identity and Access Management API**

## Get Started

**ErtisAuth** is a free and open-source OpenID-Connect framework and high performer identity and access management API.
It's designed to provide a common way to authenticate requests to all of your applications, whether they're web, native, mobile, or Web API endpoints.
It is based on the RBAC (Role based access control) and UBAC (User based access control) models for authorization/access control.
ErtisAuth incorporates features needed to integrate token-based authentication, SSO and API access control in your applications for authorization and authentication.
It is licensed under MIT License (an OSI approved license).

## Documentation

For developer guide and API documentation, please visit [wiki page](https://github.com/ertugrulozcan/ErtisAuth/wiki).

## Installation

### Standalone Build & Installation on Linux, Windows or Mac OS

* Install the latest .NET 7 SDK
* Install Git
* Clone this repo
* Install MongoDB
* Set database configuration
* Build solution in the root of the cloned repo
* Start ErtisAuth Server
* Migrate
* Enjoy with ErtisAuth

## ErtisAuth on Docker

Make sure you have Docker installed.

### Standalone Running from Docker Image

If you have a mongo database, you just run ErtisAuth with docker image. Database configuration can be pass with environment variables on the docker run command.

From a terminal start ErtisAuth with the following command:

```shell
$ docker run -p 9716:80 ertugrulozcan/ertisauth:latest
```

For database configuration with environment variables;

```shell
$ docker run -p 9716:80 -e Database__ConnectionString=<ConnectionString> ertugrulozcan/ertisauth:latest
```

- `DB_SCHEME`: Specify name of the schema prefix string to use for DB that support schemas (optional, default is 'mongodb').
- `DB_HOST`: Specify hostname of the database (optional). ErtisAuth will append DB_PORT (if specify) to the hosts without port, otherwise it will append the default port 27017, again to the address without port only.
- `DB_PORT`: Specify port of the database (optional, default is 27017)
- `DB_USERNAME`: Specify user to use to authenticate to the database (optional, default is ``).
- `DB_PASSWORD`: Specify user's password to use to authenticate to the database (optional, default is ``).
- `DB_DATABASE`: Optional, default is 'ertisauth'. The authentication database to use if the connection string includes username:password@ authentication credentials but the authSource option is unspecified. If both authSource and defaultauthdb are unspecified, the client will attempt to authenticate the specified user to the admin database.

### Working with Docker Compose

* Download [docker-compose.yml](https://github.com/ertugrulozcan/ErtisAuth/blob/master/docker-compose.yml) file
* Run `docker-compose up -d` command on shell
* ErtisAuth is now running on public port 9716

```shell
$ docker-compose up -d
```

This command will start ErtisAuth API exposed on the local port 9716 along with a composed MongoDB in same container. It will also create an membership (realm) an initial admin user and an application (optional) for machine to machine communication.
If you wish, you can migrate whether membership, admin user, application etc resources later. For more information about the migration, look at the migration section in the documentation.

<br/>

## Migration

To start using ErtisAuth, you must have at least one membership and at least one user for generating tokens.
To use basic authentication method, you must have an application.
ErtisAuth incorporates a migration API endpoint to create basic resources such as membership and admin user and set administrator settings at the first installation.
The migrate command creates these resources and configures relations and settings.
The admin role will be created automatically by ErtisAuth.
The database connection string must be sent in headers to checking the database authorization.

<br/>

Request Model

```yaml
curl --location --request POST '{{base_url}}/api/v1/migrate' \
--header 'ConnectionString: {{db_connection_string}}' \
--header 'Content-Type: application/json' \
--data-raw '{
    "membership": {
        "name": "{{membership_name}}",
        "expires_in": {{token_life_time}},
        "refresh_token_expires_in": {{refresh_token_life_time}},
        "hash_algorithm": "{{hash_algorithm}}",
        "encoding": "UTF-8"
    },
    "user": {
        "username": "{{usernamme}}",
        "firstname": "{{first_name}}",
        "lastname": "{{last_name}}",
        "email_address": "{{email_address}}",
        "password": "{{password}}",
        "user_type": "User"
    },
    "application": {
        "name": "{{application_name}}",
        "role": "admin"
    }
}'
```

<br/>

## Health Check

Request Model

```yaml
curl --location --request GET '{{base_url}}/api/v1/healthcheck'
```

Successful Response

Status Code : `200 OK`

```json
{
    "status": "Healthy"
}
```

Failed Response

Status Code : `50X` (Sample unhealhty case)

```json
{
    "status": "Unhealthy",
    "message": "Database have not migrated yet"
}
```

<br/>

# Endpoints

<br/>

## 1. Tokens Endpoint &nbsp;&nbsp; *`/tokens`*

<br/>

| Routes                   | Method     | Headers                                                       | Query String                    | Body                        |
| ------------------------ | ---------- | ------------------------------------------------------------- | ------------------------------- | --------------------------- |
| `/tokens/me`             | GET        | Authorization Header                                          | -                               | -                           |
| `/tokens/whoami`         | GET        | Authorization Header                                          | -                               | -                           |
| `/tokens/generate-token` | POST       | X-Ertis-Alias, X-IpAddress (optional), X-UserAgent (optional) | -                               | Username & Password payload |                   |
| `/tokens/verify-token`   | GET        | Authorization Header                                          | -                               | -                           |
| `/tokens/verify-token`   | POST       | -                                                             | -                               | Token payload               |
| `/tokens/refresh-token`  | GET        | Authorization Header                                          | revoke=true (Default false)     | -                           |
| `/tokens/refresh-token`  | POST       | -                                                             | revoke=true (Default false)     | Token payload               |
| `/tokens/revoke-token`   | GET        | Authorization Header                                          | logout-all=true (Default false) | -                           |
| `/tokens/revoke-token`   | POST       | -                                                             | logout-all=true (Default false) | Token payload               |

<br/>

## Login / Generate Token

<br/>

Request Model

```yaml
curl --location --request POST '{{base_url}}/api/v1/generate-token' \
--header 'X-Ertis-Alias: {{membership_id}}' \
--header 'X-IpAddress: {{client_ip}}' \
--header 'X-UserAgent: {{user_agent}}' \
--header 'Content-Type: application/json' \
--data-raw '{
    "username": "{{username}}",
    "password": "{{password}}"
}'
```

Body Model

```json
{
    "username": "{{username}}",
    "password": "{{password}}"
}
```

Successful Response

Status Code : `201 Created`

```json
{
    "token_type": "bearer",
    "refresh_token": "{refresh_token}",
    "refresh_token_expires_in": 86400,
    "access_token": "{access_token}",
    "expires_in": 43200,
    "created_at": "2021-09-01T20:46:03.1354057+03:00"
}
```

Failed Response (Username or password incorrect)

Status Code : `401 Unauthorized`

```json
{
    "Message": "Username or password is wrong",
    "ErrorCode": "UsernameOrPasswordIsWrong",
    "StatusCode": 401
}
```

Failed Response (X-Ertis-Alias Header Missing)

Status Code : `400 Bad Request`

```json
{
    "message": "Membership id should be added in headers with 'X-Ertis-Alias' key.",
    "errorCode": "XErtisAliasMissing",
    "statusCode": 400
}
```

<hr>
<br/>

## Me / Whoami

<br/>

Request Model

```yaml
curl --location --request GET '{{base_url}}/api/v1/me' \
--header 'Authorization: Bearer {{access_token}}'
```

or

```yaml
curl --location --request GET '{{base_url}}/api/v1/whoami' \
--header 'Authorization: Bearer {{access_token}}'
```

Successful Response

Status Code : `200 OK`

```json
{
    "_id": "{{id}}",
    "firstname": "{{first_name}}",
    "lastname": "{{last_name}}",
    "username": "{{username}}",
    "email_address": "{{email}}",
    "role": "{{role}}",
    "permissions": [],
    "forbidden": [],
    "sys": {
        "created_at": "2021-06-03T20:29:22.545+03:00",
        "created_by": "{{created_by}}",
        "modified_at": "2021-09-01T18:31:57.932+03:00",
        "modified_by": "{{modified_by}}",
    },
    "membership_id": "{{membership_id}}"
}
```

Failed Response (Authorization Header Missing)

Status Code : `400 Bad Request`

```json
{
    "Message": "Authorization header missing or empty",
    "ErrorCode": "AuthorizationHeaderMissing",
    "StatusCode": 400
}
```

Failed Response (Ambiguous or unsupported token type)

Status Code : `400 Bad Request`

```json
{
   "Message": "Token type not supported. Token type must be one of Bearer or Basic",
   "ErrorCode": "TokenTypeNotSupported",
   "StatusCode": 400
}
```

Failed Response (Invalid token)

Status Code : `401 Unauthorized`

```json
{
   "Message": "Provided token is invalid",
   "ErrorCode": "InvalidToken",
   "StatusCode": 401
}
```

Failed Response (Expired token)

Status Code : `401 Unauthorized`

```json
{
   "Message": "Provided token was expired",
   "ErrorCode": "TokenWasExpired",
   "StatusCode": 401
}
```

Failed Response (Revoked token)

Status Code : `401 Unauthorized`

```json
{
   "Message": "Provided token was revoked",
   "ErrorCode": "TokenWasRevoked",
   "StatusCode": 401
}
```

<hr>
<br/>

## Verify Token

<br/>

Request Model

```yaml
curl --location --request GET '{{base_url}}/api/v1/verify-token' \
--header 'Authorization: Bearer {{access_token}}'
```

or

```yaml
curl --location --request POST '{{base_url}}/api/v1/verify-token' \
--header 'Content-Type: application/json' \
--data-raw '{
    "token": "Bearer {{access_token}}"
}'
```

Successful Response

Status Code : `200 OK`

```json
{
    "verified": true,
    "token": "{{access_token}}",
    "token_kind": "access_token",
    "remaining_time": {{remaining_time_seconds}}
}
```

<hr>
<br/>

## Refresh Token

<br/>

Request Model

```yaml
curl --location --request GET '{{base_url}}/api/v1/refresh-token' \
--header 'Authorization: Bearer {{refresh_token}}'
```

or

```yaml
curl --location --request POST '{{base_url}}/api/v1/refresh-token' \
--header 'Content-Type: application/json' \
--data-raw '{
    "token": "Bearer {{refresh_token}}"
}'
```

> &nbsp;
> If you want to revoke the current token besides the token refresh, add "?revoke=true" to the query string
> &nbsp;

<br/>

Successful Response

Status Code : `201 Created`

```json
{
    "token_type": "bearer",
    "refresh_token": "{refresh_token}",
    "refresh_token_expires_in": 86400,
    "access_token": "{{access_token}}",
    "expires_in": 43200,
    "created_at": "2021-09-01T20:46:03.1354057+03:00"
}
```

Failed Response (Expired refresh token)

Status Code : `401 Unauthorized`

```json
{
    "Message": "Provided refresh token was expired",
    "ErrorCode": "RefreshTokenWasExpired",
    "StatusCode": 401
}
```

<hr>
<br/>

## Logout / Revoke Token

<br/>

Request Model

```yaml
curl --location --request GET '{{base_url}}/api/v1/revoke-token' \
--header 'Authorization: Bearer {{access_token}}'
```

or

```yaml
curl --location --request POST '{{base_url}}/api/v1/revoke-token' \
--header 'Content-Type: application/json' \
--data-raw '{
    "token": "Bearer {{access_token}}"
}'
```

> &nbsp;
> If you want to log out of all sessions, add "?logout-all=true" to the query string. This operation will be signed-out of all sessions for the owner of the token on all devices.
> &nbsp;

<br/>

Successful Response

Status Code : `204 No Content`

<br/>

Failed Response (Invalid or already revoked token)

Status Code : `401 Unauthorized`

<hr>
<br/>

## Social Media Providers

ErtisAuth allows their customers to sign-up by using their email address and a password to create what's called a local account. Or, they can sign-up and sign in by using their Google, Facebook, or Microsoft accounts as their identity provider. By serving as the central authentication authority for your web applications, mobile apps, and APIs, ErtisAuth enables you to build a single sign-on (SSO) solution for them all. In addition to that, your customers can select sign in with their personal social media accounts. When users select social media login, they're redirected to a sign-in page hosted by provider. After the login process, ErtisAuth takes over the session and user management and resumes the transactions for you.

> &nbsp;
> In order to use social media providers, the necessary settings must be applied on the providers API and the provider must be activated. For details on the subject, see the providers section in the documentation.
> &nbsp;

### Login with Facebook

Request Model

```yaml
curl --location --request POST '{{base_url}}/api/v1/oauth/facebook/login' \
--header 'X-Ertis-Alias: {{membership_id}}' \
--header 'X-IpAddress: {{client_ip}}' \
--header 'X-UserAgent: {{user_agent}}' \
--header 'Content-Type: application/json' \
  --data-raw '{
    "user": {{facebookUserPayload}},
    "appId": "{{facebookAppId}}"
}'
```

Facebook User Model
```typescript
{
    id: string
    userID: string
    accessToken: string
    name?: string
    email?: string
    picture?: | {
        data: {
            height?: number
            is_silhouette?: boolean
            url?: string
            width?: number
        };
    }
}
```

<br/>

### Login with Google

Request Model

```yaml
curl --location --request POST '{{base_url}}/api/v1/oauth/google/login' \
--header 'X-Ertis-Alias: {{membership_id}}' \
--header 'X-IpAddress: {{client_ip}}' \
--header 'X-UserAgent: {{user_agent}}' \
--header 'Content-Type: application/json' \
  --data-raw '{
    "token": {{googleCredentialsPayload}},
    "clientId": "{{googleClientId}}"
}'
```

Google Credentials Model
```typescript
{
    idToken: string
    clientId: string
}
```

<br/>

### Login with Microsoft

Request Model

```yaml
curl --location --request POST '{{base_url}}/api/v1/oauth/microsoft/login' \
--header 'X-Ertis-Alias: {{membership_id}}' \
--header 'X-IpAddress: {{client_ip}}' \
--header 'X-UserAgent: {{user_agent}}' \
--header 'Content-Type: application/json' \
  --data-raw '{
    "token": {{microsoftCredentialsPayload}},
    "clientId": "{{microsoftClientId}}"
}'
```

Microsoft Credentials Model
- uniqueId               - `oid` or `sub` claim from ID token
- tenantId               - `tid` claim from ID token
- scopes                 - Scopes that are validated for the respective token
- account                - An account object representation of the currently signed-in user
- idToken                - Id token received as part of the response
- idTokenClaims          - MSAL-relevant ID token claims
- accessToken            - Access token or SSH certificate received as part of the response
- fromCache              - Boolean denoting whether token came from cache
- expiresOn              - Relative expiration date of access token
- extExpiresOn           - Extended relative expiration date of access token in case of server outage
- state                  - Value passed in by user in request
- familyId               - Family ID identifier, usually only used for refresh tokens
- requestId              - Request ID returned as part of the response
- homeAccountId          - Home account identifier for this account object
- environment            - Entity which issued the token represented by the domain of the issuer (e.g. login.microsoftonline.com)
- tenantId               - Full tenant or organizational id that this account belongs to
- username               - preferred_username claim of the id_token that represents this account
- localAccountId         - Local, tenant-specific account identifer for this account object, usually used in legacy cases
- name                   - Full name for the account, including given name and family name
- idToken                - Raw ID token
- idTokenClaims          - Object contains claims from ID token
- localAccountId         - The user's account ID
- nativeAccountId        - The user's native account ID

```typescript
{
    authority: string
    uniqueId: string
    tenantId: string
    scopes: string[]
    account: {
        homeAccountId: string
        environment: string
        tenantId: string
        username: string
        localAccountId: string
        name?: string
        idToken?: string
        idTokenClaims?: {
            aud?: string
            iss?: string
            iat?: number
            nbf?: number
            oid?: string
            sub?: string
            tid?: string
            ver?: string
            upn?: string
            preferred_username?: string
            login_hint?: string
            emails?: string[]
            name?: string
            nonce?: string
            exp?: number
            home_oid?: string
            sid?: string
            cloud_instance_host_name?: string
            cnf?: {
                kid: string
            }
            x5c_ca?: string[]
            ts?: number
            at?: string
            u?: string
            p?: string
            m?: string
            roles?: string[]
            amr?: string[]
            idp?: string
            auth_time?: number
        }
        nativeAccountId?: string
    }
    idToken: string
    idTokenClaims: object
    accessToken: string
    fromCache: boolean
    expiresOn: Date
    tokenType: string
    correlationId: string
    requestId?: string
    extExpiresOn?: Date
    state?: string
    familyId?: string
    cloudGraphHostName?: string
    msGraphHost?: string
    code?: string
    fromNativeBroker?: boolean
}
```