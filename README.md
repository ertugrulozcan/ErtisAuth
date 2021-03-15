# ErtisAuth
## Open Source & Generic Identity and Access Management Service

## 1. Giriş
ErtisAuth, kimlik ve yetkilendirme yönetimi sağlayan, OpenID-Connect protokollerine bağlı, claim temsil protolokü olarak JWT tabanlı bir OAuth2.0 implementasyonu, role tabanlı bir authentication hizmetidir. Yetki/erişim denetimi için RBAC (Role based access control) modelini baz alır.

## 2. Project Structure

### Ertis Project

Data
1. Ertis.Data
2. Ertis.MongoDB

Extensions
3. Ertis.Extensions.AspNetCore

Network
4. Ertis.Net 

Security
5. Ertis.Security

Shared
6. Ertis.Core


### ErtisAuth

Api
1. ErtisAuth.WebAPI

Business
2. ErtisAuth.Infrastructure

Data
3. ErtisAuth.Dao
4. ErtisAuth.Dto

Identity
5. ErtisAuth.Identity

Shared
6. ErtisAuth.Abstractions
7. ErtisAuth.Core

## 3. API

### Documentation

Postman Collection => https://www.getpostman.com/collections/185db33e4db8d129d493

Swagger UI => /swagger/index.html

### Authentication / Authorization
Healthcheck ve api-map endpoint'leri haricindeki tüm endpoint'ler authorization'a tabidir.

API kullanıcıları için bearer token ve/veya makineden makineye B2B operasyonlarda kullanılmak üzere basic token ile kimlik ibrazı zorunludur. Yetki denetimi token sahibi utilizer (user/application) rolü üzerinden yapılır. Token type (Bearer/Basic) token başına eklenmek zorundadır, aksi halde 'UnsupportedTokenType' cevabı ile karşılaşılacaktır. 

Token iletmek için iki farklı yöntem kullanılabilir; (bkz rfc6750/4)

1. URI Query Parameter

Access token bilgisi 'access_token' anahtarı ile query string üzerinden iletilir.

`/resource?access_token={{access_token}}`

2. Authorization Request Header Field

Access token bilgisi 'Authorization' header'ı ile iletilir.

Authorization için tanımlanan request/response challenge aşağıda şematize edilmiştir.


     +--------+                               +---------------+
     |        |--(A)- Authorization Request ->|   Resource    |
     |        |                               |     Owner     |
     |        |<-(B)-- Authorization Grant ---|               |
     |        |                               +---------------+
     |        |
     |        |                               +---------------+
     |        |--(C)-- Authorization Grant -->| Authorization |
     | Client |                               |     Server    |
     |        |<-(D)----- Access Token -------|               |
     |        |                               +---------------+
     |        |
     |        |                               +---------------+
     |        |--(E)----- Access Token ------>|    Resource   |
     |        |                               |     Server    |
     |        |<-(F)--- Protected Resource ---|               |
     +--------+                               +---------------+

Başarısız bir authentication sürecinde alınabilecek olası cevaplar aşağıdaki gibi özetlenebilir.

Token iletilmediği durumda;
```json
{
    "Message": "Authorization header missing or empty",
    "ErrorCode": "AuthorizationHeaderMissing",
    "StatusCode": 400
}
```

Token type eksik olduğu durumda;
```json
{
   "Message": "Token type not supported. Token type must be one of Bearer or Basic",
   "ErrorCode": "TokenTypeNotSupported",
   "StatusCode": 400
}
```

Geçersiz bir token gönderildiği durumda;
```json
{
   "Message": "Provided token is invalid",
   "ErrorCode": "InvalidToken",
   "StatusCode": 401
}
```

Token'ın yaşam süresi dolduğu durumda;
```json
{
   "Message": "Provided token was expired",
   "ErrorCode": "TokenWasExpired",
   "StatusCode": 401
}
```

İptal edilmiş bir token gönderildiği durumda;
```json
{
   "Message": "Provided token was revoked",
   "ErrorCode": "TokenWasRevoked",
   "StatusCode": 401
}
```



### a. Register

Register işlemi için `/users` endpoint'ine POST ile gidilir. Ayrıntılı bilgi için Users başlığını inceleyiniz.

**Örnek Request**
```
curl --location --request POST '{{base_url}}/api/v1/memberships/{{membership_id}}/users' \
   --header 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhenAiOiI1ZmQxMTY1ZTkzMzU2ZTZhMGJlNjQ5ZjYiLCJpYXQiOiIxNjEwMDUyODUzIiwic3ViIjoiNWZkMTE2NWU5MzM1NmU2YTBiZTY0OWY2IiwianRpIjoiMzc2YTk3N2ItM2YzZi00ODBhLWI1MjEtMWNkNTU3ZDEzNmQxIiwicHJuIjoiNWZjZjZkYTE0NzkwYTFlMDMxYmU1MzI1IiwiZ2l2ZW5fbmFtZSI6IkVydHXEn3J1bCIsImZhbWlseV9uYW1lIjoiw5Z6Y2FuIiwiZW1haWwiOiJlcnR1Z3J1bC5vemNhbkBiaWwub211LmVkdS50ciIsImV4cCI6MTYxMDA3NDQ1MywiaXNzIjoia2FyaXllcm5ldCIsImF1ZCI6IjVmZDExNjVlOTMzNTZlNmEwYmU2NDlmNiJ9.zenHCr-bdzfb9bUFALN358NllqcPq_mlqG1esYEVDuc' \
   --header 'Content-Type: application/json' \
   --data-raw '{
      "firstname": "<first_name>",
      "lastname": "<last_name>",
      "username": "<username>",
      "email_address": "<email>",
      "role": "<role>",
      "password": "<password>"
   }'
```

**Response**

Başarılı Status Code : 201 Created
```json
{
    "firstname": "John",
    "lastname": "Smith",
    "username": "johnsmith",
    "email_address": "john.smith@gmail.com",
    "role": "enduser",
    "sys": {
        "created_at": "2021-01-08T02:15:48.6753356+03:00",
        "created_by": "ertugrul.ozcan@ertis.io"
    },
    "membership_id": "5fcf6da14790a1e031be5325",
    "_id": "5ff79624531d1c67765efb54"
}
```

**Olası Başarısız Durumlar**

Status Code : 400 BadRequest
```json
{
   "Data": [
      "username is a required field",
      "email_address is a required field"
   ],
   "Message": "Some fields are not validated, invalid or missing. Check response detail.",
   "ErrorCode": "ModelValidationError",
   "StatusCode": 400
}
```

Status Code : 400 BadRequest
```json
{
   "Data": [
      "Role is invalid. There is no role named 'foobar'"
   ],
   "Message": "Some fields are not validated, invalid or missing. Check response detail.",
   "ErrorCode": "ModelValidationError",
   "StatusCode": 400
}
```

Status Code : 409 Conflict
```json
{
    "Message": "The user with same username or email is already exists ('jonhsmith', 'jonh.smith@gmail.com')",
    "ErrorCode": "UserWithSameUsernameAlreadyExists",
    "StatusCode": 409
}
```


### b. Login / Generate Token

Token almak için `/generate-token` endpoint'ine POST ile gidilir. Body'de username (veya email adresi) ve password bilgileri gönderilir.
Header'da da 'X-Ertis-Alias' anahtarı ile kullanıcının bağlı ollduğu realm olan membership_id bilgisinin iletilmesi gerekmektedir.

**Örnek Request**
```
curl --location --request POST '{{base_url}}/api/v1/generate-token' \
   --header 'X-Ertis-Alias: {{membership_id}}' \
   --header 'Content-Type: application/json' \
   --data-raw '{
       "username": "<username>",
       "password": "<password>"
   }'
```

Başarılı Status Code : 201 Created
```json
{
   "token_type": "bearer",
   "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhenAiOiI1ZmQxMTY1ZTkzMzU2ZTZhMGJlNjQ5ZjYiLCJpYXQiOiIxNjEwMDUyODUzIiwic3ViIjoiNWZkMTE2NWU5MzM1NmU2YTBiZTY0OWY2IiwianRpIjoiMzc2YTk3N2ItM2YzZi00ODBhLWI1MjEtMWNkNTU3ZDEzNmQxIiwicHJuIjoiNWZjZjZkYTE0NzkwYTFlMDMxYmU1MzI1IiwiZ2l2ZW5fbmFtZSI6IkVydHXEn3J1bCIsImZhbWlseV9uYW1lIjoiw5Z6Y2FuIiwiZW1haWwiOiJlcnR1Z3J1bC5vemNhbkBiaWwub211LmVkdS50ciIsInJlZnJlc2hfdG9rZW4iOiJUcnVlIiwiZXhwIjoxNjEwMDc0NDUzLCJpc3MiOiJrYXJpeWVybmV0IiwiYXVkIjoiNWZkMTE2NWU5MzM1NmU2YTBiZTY0OWY2In0.n_AiWRnHOnp2WPbhZCKabcozebcl5Lw6UmkNgq9c3f4",
   "refresh_token_expires_in": 21600,
   "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhenAiOiI1ZmQxMTY1ZTkzMzU2ZTZhMGJlNjQ5ZjYiLCJpYXQiOiIxNjEwMDUyODUzIiwic3ViIjoiNWZkMTE2NWU5MzM1NmU2YTBiZTY0OWY2IiwianRpIjoiMzc2YTk3N2ItM2YzZi00ODBhLWI1MjEtMWNkNTU3ZDEzNmQxIiwicHJuIjoiNWZjZjZkYTE0NzkwYTFlMDMxYmU1MzI1IiwiZ2l2ZW5fbmFtZSI6IkVydHXEn3J1bCIsImZhbWlseV9uYW1lIjoiw5Z6Y2FuIiwiZW1haWwiOiJlcnR1Z3J1bC5vemNhbkBiaWwub211LmVkdS50ciIsImV4cCI6MTYxMDA3NDQ1MywiaXNzIjoia2FyaXllcm5ldCIsImF1ZCI6IjVmZDExNjVlOTMzNTZlNmEwYmU2NDlmNiJ9.zenHCr-bdzfb9bUFALN358NllqcPq_mlqG1esYEVDuc",
   "expires_in": 21600,
   "created_at": "2021-01-07T23:54:13.5556795+03:00"
}
```

Status Code : 401 Unauthorized
```json
{
    "Message": "Username or password is wrong",
    "ErrorCode": "UsernameOrPasswordIsWrong",
    "StatusCode": 401
}
```

### c. Me / WhoAmI

Token sahibi kullanıcıyı öğrenmek için `/me` veya `/whoami` endpoint^lerinden birine GET ile gidilir. Authentication header'da token bilgisi gönderilir.

**Örnek Request**
```
curl --location --request GET 'http://localhost:5000/api/v1/me' \
--header 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhenAiOiI1ZmQxMTY1ZTkzMzU2ZTZhMGJlNjQ5ZjYiLCJpYXQiOiIxNjEwMDYyODYzIiwic3ViIjoiNWZkMTE2NWU5MzM1NmU2YTBiZTY0OWY2IiwianRpIjoiMjFjYmU3YmQtMGI1Yi00NjY4LTkwYTUtMmRhNmU3YjMzMGU4IiwicHJuIjoiNWZjZjZkYTE0NzkwYTFlMDMxYmU1MzI1IiwiZ2l2ZW5fbmFtZSI6IkVydHXEn3J1bCIsImZhbWlseV9uYW1lIjoiw5Z6Y2FuIiwiZW1haWwiOiJlcnR1Z3J1bC5vemNhbkBiaWwub211LmVkdS50ciIsImV4cCI6MTYxMDA4NDQ2MywiaXNzIjoia2FyaXllcm5ldCIsImF1ZCI6IjVmZDExNjVlOTMzNTZlNmEwYmU2NDlmNiJ9.A0oKaq5NdNPlR4mIygBUi8vO4bmmn6MSiIf7G6R_AV8'
```

Başarılı Status Code : 201 Created
```json
{
   "firstname": "Ertuğrul",
   "lastname": "Özcan",
   "username": "ertugrul.ozcan",
   "email_address": "ertugrul.ozcan@ertis.io",
   "role": "admin",
   "sys": {
      "created_at": "2020-12-28T17:41:05.571Z",
      "modified_at": "2021-01-06T14:03:47.786Z",
      "modified_by": "ertugrul.ozcan@ertis.io"
   },
   "membership_id": "5fcf6da14790a1e031be5325",
   "_id": "5fd1165e93356e6a0be649f6"
}
```

## Referanslar
* https://tools.ietf.org/html/rfc6749
* https://tools.ietf.org/html/rfc7519
* https://tools.ietf.org/id/draft-ietf-regext-rdap-openid-03.html
* https://www.ietf.org/rfc/rfc6750.txt
* https://openid.net/specs/openid-connect-core-1_0.html

docker run -d --name mongodb --mount source=mongodb_volume,target=/ertisauth -p 27017:27107 mongodb:latest
