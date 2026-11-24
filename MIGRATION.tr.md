# ErtisAuth .NET 10 sürümüne geçiş

[English](MIGRATION.md) | **Türkçe**

Bu rehber, önceki ErtisAuth sürümünden (.NET 9, `net9` branch'i) .NET 10 sürümüne geçişi anlatır. Üç gruba yöneliktir:

- **Operatörler / DevOps:** veritabanı migration'ı, deployment ve konfigürasyon ([1-3. bölümler](#1-deploy-öncesi-veritabanı)).
- **İstemci uygulama geliştiricileri:** API davranışındaki ve sözleşmesindeki değişiklikler ([4. bölüm](#4-api-değişiklikleri-istemci-uygulamalar)).
- **SDK kullanıcıları:** `ErtisAuth.Sdk` ve yeni `ErtisAuth.Sdk.AspNetCore` paketi ([5. bölüm](#5-sdk-değişiklikleri)).

Bu sürüm ağırlıklı olarak bir güvenlik ve doğruluk sürümüdür. Bazı düzeltmeler istemcilerin dayanıyor olabileceği davranışları değiştirir ve kayıtlı verilerin bir kısmı yeni sürüm başlamadan **önce** migrate edilmelidir. Geçişi planlamadan önce 1. ve 4. bölümleri eksiksiz okuyun.

## Özetle geçiş adımları

1. **Veritabanının yedeğini alın.**
2. [Deploy öncesi kontrolleri ve migration'ları](#1-deploy-öncesi-veritabanı) çalıştırın. Bir kısmı zorunludur: bunlar yapılmazsa deploy'dan sonra sağlayıcı ile girişler, token yenileme veya rolü olmayan hesaplar hata verir.
3. [API değişikliklerinden](#4-api-değişiklikleri-istemci-uygulamalar) etkilenen istemcileri güncelleyin. Bazıları (cihaz kodu akışı, sağlayıcı ile giriş route'ları, OTP) sunucuyla birlikte yayına alınmalıdır.
4. [Deployment'ı](#2-deployment-ve-konfigürasyon) güncelleyin: container portu 8080, root olmayan kullanıcı, health probe'ları, yeni proje yolları.
5. Deploy edin, ardından [deploy sonrası adımları](#3-deploy-sonrası) uygulayın.

Bazı yan etkiler kabul edilmiştir ve işlem gerektirmez; bkz. [Bilinen yan etkiler](#6-geçişin-bilinen-yan-etkileri).

---

## 1. Deploy öncesi: veritabanı

Aşağıdaki script'ler `mongosh` için yazılmıştır ve ErtisAuth veritabanında çalıştırılır. Herhangi bir `update`/`delete` çalıştırmadan önce `find`/`aggregate` kontrollerini çalıştırıp çıktılarını inceleyin.

### 1.1 Zorunlu migration'lar

#### Membership'ler: `hash_algorithm` zorunlu

Artık örtük bir SHA2-256 varsayılanı yok. `hash_algorithm` alanı olmayan her membership'e, mevcut şifre hash'lerinin oluşturulduğu algoritmayı açıkça yazın:

```js
db.memberships.find({ $or: [{ hash_algorithm: { $exists: false } }, { hash_algorithm: null }] }, { name: 1 })
```

#### Sağlayıcılar: `type` discriminator'ı

Her sağlayıcı belgesinin `type` discriminator alanına (`Apple`, `AppleNative`, `Facebook`, `Google` veya `Microsoft`) ihtiyacı vardır. Bu alan olmadan sağlayıcıların listelenmesi ve onlarla giriş 500 döner. Sağlayıcı adlarınız bu değerlerle aynıysa (önce doğrulayın) adları kopyalayın:

```js
db.providers.find({ type: { $exists: false } }, { name: 1, membership_id: 1 })
db.providers.updateMany({ type: { $exists: false } }, [{ $set: { type: "$name" } }])
```

Yeni membership'ler için sağlayıcılar artık otomatik oluşturulmuyor. Eski sürümlerin oluşturduğu pasif varsayılan sağlayıcı belgelerini silip silmeyeceğinize karar verin.

#### Sağlayıcılar: slug'lar

Sağlayıcı ile giriş artık `POST /oauth/{slug}/login` üzerinden yapılır ([ayrıntılar](#sağlayıcı-ile-giriş)). Her sağlayıcıya membership içinde benzersiz bir slug verin. Mevcut web ve Android istemcilerinin değişiklik yapmadan çalışmaya devam etmesi için `facebook`, `google`, `microsoft` ve `apple` kullanın. AppleNative sağlayıcısının kendine ait bir slug'a ihtiyacı vardır, örneğin `applenative`. Açılışta `{ membership_id, slug }` üzerinde unique bir index oluşturulur. Tekrarlanan değerler varsa index oluşturulmaz ve bir hata loglanır.

#### Sağlayıcılar: `trust_email`

Sağlayıcılarda yeni bir bayrak var: `trust_email` (yoksa `false`). Harici bir giriş, sağlayıcının doğrulanmış olarak işaretlemediği bir e-posta döndürdüğünde ErtisAuth bu e-postayı mevcut bir kullanıcıya yalnızca `trust_email` `true` ise bağlar. Aksi halde `409 ProviderEmailNotTrusted` döner. E-posta üzerinden hesap bağlamanın çalışmaya devam etmesi gereken her sağlayıcıda `trust_email: true` ayarlayın. Microsoft ve Facebook için bu gereklidir; Google ve Apple için davranışın değişmemesini istiyorsanız önerilir.

#### Kullanıcılar: `connected_accounts` yapısı

`connected_accounts` öğeleri `{ Provider, UserId, Token }` yapısından `{ provider, slug, user_id, token }` yapısına geçer. Giriş, sağlayıcı + kullanıcı id'si ile eşleşir ve çıkış slug'a göre iptal eder; bu yüzden bu migration deploy'dan **önce** çalışmalıdır. AppleNative slug'ını yukarıda seçtiğiniz değere göre düzenleyin:

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

Eski AppleNative girişleri ayrıca yinelenen bir `Apple` öğesi de kaydediyordu. `user_id` değeri kullanıcının `AppleNative` öğesiyle aynı olan `Apple` öğelerini silebilirsiniz.

#### Kullanıcı tipleri: `connected_accounts` tanımı

Kayıtlı kullanıcı tipleri, eski `connected_accounts` tanımı dahil temel kullanıcı alanlarının bir kopyasını tutar. Bu tiplerin kullanıcıları, kullanıcı tipleri migrate edilene kadar sağlayıcı ile girişte 400 alır. Bunu yukarıdaki kullanıcı migration'ı ile birlikte çalıştırın:

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

#### Rolü olmayan kullanıcılar ve uygulamalar

Daha önce rolü boş olan bir kullanıcı veya uygulama yetki kontrolünü **atlıyordu** ve bu da ona tam erişim veriyordu. Artık yetki gerektiren her endpoint'te `403` ile reddedilir. Bu hesapları bulun ve deploy'dan önce doğru rolü atayın:

```js
db.users.find({ $or: [{ role: { $exists: false } }, { role: null }, { role: "" }] }, { username: 1, membership_id: 1 })
db.applications.find({ $or: [{ role: { $exists: false } }, { role: null }, { role: "" }] }, { name: 1, membership_id: 1 })
```

#### Kullanıcılar: küçük harfli kullanıcı adları ve e-posta adresleri

`username` ve `email_address` artık baştaki ve sondaki boşluklar kırpılmış ve küçük harfe çevrilmiş (Unicode NFC) olarak saklanır; giriş, şifre sıfırlama, aktivasyon e-postası ve sağlayıcı ile giriş de bunları aynı şekilde arar. Kayıtlı kullanıcı adında veya e-posta adresinde büyük harf ya da baş/son boşluk olan bir kullanıcı, bu migration çalışana kadar **giriş yapamaz**.

Önce normalize edildiğinde birbirine eşit hale gelen değerleri bulun. Aşağıdaki update bunlarda (unique index nedeniyle) hata verir; bu yüzden bunları elle çözün:

```js
["username", "email_address"].forEach(f => printjson(db.users.aggregate([
	{ $match: { [f]: { $type: "string", $gt: "" } } },
	{ $group: { _id: { m: "$membership_id", v: { $toLower: { $trim: { input: "$" + f } } } }, n: { $sum: 1 }, ids: { $push: "$_id" } } },
	{ $match: { n: { $gt: 1 } } }
], { allowDiskUse: true }).toArray()))
```

Ardından normalize edin:

```js
const needsNormalization = /[A-Z]|^\s|\s$/
db.users.updateMany(
	{ $or: [{ username: needsNormalization }, { email_address: needsNormalization }] },
	[{ $set: {
		username: { $cond: [{ $eq: [{ $type: "$username" }, "string"] }, { $toLower: { $trim: { input: "$username" } } }, "$username"] },
		email_address: { $cond: [{ $eq: [{ $type: "$email_address" }, "string"] }, { $toLower: { $trim: { input: "$email_address" } } }, "$email_address"] }
	} }]
)
```

`$toLower` yalnızca ASCII harfleri küçültür. Aşağıdaki sorgu ASCII dışı karakter içeren değerleri listeler; büyük harf içerenleri (örneğin `İrem`) elle düzeltin:

```js
db.users.find({ $or: [{ username: /[^\x00-\x7f]/ }, { email_address: /[^\x00-\x7f]/ }] }, { username: 1, email_address: 1, membership_id: 1 })
```

Sonrasında [1.2](#tekrarlanan-e-postalar-kullanıcı-adları-ve-unique-alanlar) bölümündeki kullanıcı adı / e-posta adresi çakışma kontrolünü çalıştırın.

#### Aktif token'lar: eski konum bilgisi

`client_info.geo_location` kaldırıldı. Bu alanı hâlâ içeren oturum belgeleri okunamaz; bu yüzden deploy'dan önce oluşturulmuş oturumlarda token yenileme ve "tüm cihazlardan çıkış" 500 döner:

```js
db.active_tokens.updateMany({ "client_info.geo_location": { $exists: true } }, { $unset: { "client_info.geo_location": "" } })
```

#### Cihaz kodları ve OTP'ler: eski koleksiyonları silin

İki özellik de yeniden tasarlandı. Kayıtları kısa ömürlüdür ve eski belgeler ile index'ler uyumsuzdur. Yeni index'ler açılışta oluşturulur.

```js
db.codes.drop()
db.otps.drop()
```

#### Olaylar: `is_custom_event`

```js
db.events.updateMany({ is_custom_event: { $exists: true } }, { $unset: { is_custom_event: "" } })
```

### 1.2 Veri düzeltmesi gerektirebilecek kontroller

#### Tekrarlanan e-postalar, kullanıcı adları ve unique alanlar

ErtisAuth açılışta `users` üzerinde `ux_email_address` ve `ux_username` unique partial index'lerini oluşturur (membership bazında, yalnızca boş olmayan string'ler). Ayrıca `isUnique` olarak işaretlenmiş her özel alan için bir index oluşturur. Tekrarlanan değerler varsa o index **oluşturulmaz**: hata loglanır, uygulama yine de başlar ve alanı yalnızca uygulama seviyesindeki kontrol korur. Önce tekrarlananları bulup temizleyin:

```js
db.users.aggregate([
	{ $match: { email_address: { $type: "string", $gt: "" } } },
	{ $group: { _id: { m: "$membership_id", v: "$email_address" }, n: { $sum: 1 }, ids: { $push: "$_id" } } },
	{ $match: { n: { $gt: 1 } } }
], { allowDiskUse: true })
```

Aynısını `username` ile tekrarlayın. Özel unique alanlar için `$match` aşamasına `user_type: { $in: [<alanı tanımlayan tip ve ondan türeyen tipler>] }` ekleyin.

Hem kullanıcı adı hem e-posta adresi giriş adı olarak kabul edildiği için bir kullanıcı adı artık membership'teki başka bir kullanıcının e-posta adresi olamaz (tersi de geçerli). Bunun için bir index yoktur. Zaten çakışan kullanıcılar çalışmaya devam eder ve güncellenebilir, ancak değeri bir kez değiştirdiklerinde çakışan bir değeri tekrar alamazlar. Mevcut çakışmaları bulmak için bunu [normalizasyon migration'ından](#kullanıcılar-küçük-harfli-kullanıcı-adları-ve-e-posta-adresleri) sonra çalıştırın:

```js
db.users.aggregate([
	{ $match: { username: { $type: "string", $gt: "" } } },
	{ $lookup: {
		from: "users",
		let: { m: "$membership_id", u: "$username", id: "$_id" },
		pipeline: [{ $match: { $expr: { $and: [{ $eq: ["$membership_id", "$$m"] }, { $eq: ["$email_address", "$$u"] }, { $ne: ["$_id", "$$id"] }] } } }, { $project: { _id: 1 } }],
		as: "conflicts"
	} },
	{ $match: { "conflicts.0": { $exists: true } } },
	{ $project: { membership_id: 1, username: 1, conflicts: 1 } }
], { allowDiskUse: true })
```

#### Cihaz kodu politikaları

Politikalar oluşturma ve güncellemede doğrulanır: `length` 5-12 arasında, `expires_in` 1-1800 saniye arasında olmalı ve `contains_letters`/`contains_digits` alanlarından en az biri ayarlanmış olmalıdır. Bu kurallara uymayan mevcut politikalar çalışmaya devam eder, ancak düzeltilene kadar kaydedilemez:

```js
db["code-policies"].find({ $or: [
	{ length: { $lt: 5 } }, { length: { $gt: 12 } },
	{ expires_in: { $lt: 1 } }, { expires_in: { $gt: 1800 } },
	{ contains_letters: { $ne: true }, contains_digits: { $ne: true } }
] })
```

#### Sahipsiz kullanıcı tipleri

`baseType` alanı artık var olmayan bir kullanıcı tipini gösteren kullanıcı tiplerini arayın. Başka bir tipin temel tipi olan bir kullanıcı tipi artık silinemez.

#### Eksik alanlı mail hook'lar

Mail hook'lar artık güncellemenin yanı sıra oluşturmada da `mailSubject`, `fromName`, `fromAddress` ve `recipients` alanlarını (`sendToUtilizer` ayarlı değilse) zorunlu tutar. Bunları içermeyen mevcut mail hook'lar hiçbir zaman e-posta göndermedi ve güncellenemiyordu; düzeltin veya silin.

#### Admin rolleri

Yeni kurulumlar admin rolüne `code-policies` ve `otp` için CRUD yetkileri verir. Mevcut admin rollerinde bu yetkiler yoktur. Admin'leriniz bu kaynakları yönetiyorsa ekleyin.

#### Tek kullanımlık şifre üreten roller

`GET users/{id}/generate-otp` artık dokümante edildiği gibi `otp.create.{id}` yetkisini kontrol ediyor. Önceden yanlışlıkla `users.create.{id}` kontrol ediliyordu; bu yüzden kullanıcı oluşturabilen her rol, herhangi bir kullanıcının kodunu üretip şifresini sıfırlayabiliyordu.

Deploy'dan önce tek kullanımlık şifre üreten her role, **mevcut admin rolleri dahil** (yukarıya bakın), `otp.create` yetkisini verin. Yalnızca `users.create` yetkisi olan roller bu erişimi kaybeder; bu kasıtlıdır. Şimdiye kadar kod üretebilen rolleri bulmak için:

```javascript
db.roles.find({ permissions: { $regex: "users\\.(create|\\*)" } }, { name: 1, slug: 1, membership_id: 1, permissions: 1 })
```

Kullanıcılar ve uygulamalar da kendi `permissions` alanlarında `users.create` tutabilir; kullanıcı bazlı yetkiler kullanıyorsanız onları da aynı şekilde kontrol edin.

### 1.3 Önerilen temizlik

#### Secret içeren olaylar

Önceki sürümlerin yazdığı olaylar secret içerebilir:

| Olay | Alan |
|---|---|
| `UserPasswordChanged` | `prior.password_hash` |
| `TokenGenerated` | access ve refresh token'lar |
| `TokenVerified` | bearer token ve `basicToken` (`appId:secret`; legacy uygulamalarda bu, **membership'in secret key'idir**) |
| `TokenRefreshed` | yeni access ve refresh token'lar ile kullanılan refresh token |
| `TokenRevoked` | access token |
| `Provider*` | `privateKey` |

Yeni olaylar yalnızca token meta verisi taşır. Eski olaylardan secret alanlarını kaldırın veya bu olayları silin, örneğin:

```js
db.events.updateMany({ event_type: { $in: ["TokenRefreshed", "TokenRevoked"] } }, { $unset: { document: "", prior: "" } })
```

Tam olarak güvenilmeyen biri olayları okuyabildiyse (`events.read`, webhook'lar), etkilenen membership secret key'lerini ve Apple anahtarlarını yenileyin.

#### `retain_until` alanı olmayan eski token belgeleri

Eski `active_tokens` ve `revoked_tokens` belgelerinde `retain_until` yoktur; bu yüzden TTL index'i onları hiçbir zaman silmez. Bunları silin veya `retain_until` alanını doldurun.

#### Eskimiş index'ler

Açılıştaki index oluşturma yalnızca index ekler. Önceki sürümden kalan index'leri silin, örneğin `revoked_tokens` üzerindeki `token.access_token` index'i.

---

## 2. Deployment ve konfigürasyon

### Container imajı

| | Önceki | .NET 10 |
|---|---|---|
| Base imaj | `aspnet:9.0-alpine` | `aspnet:10.0` (Debian) |
| Kullanıcı | root | root olmayan `app` (uid 1654) |
| Port | 80 | **8080** |

- Kubernetes `containerPort`/`targetPort` değerlerini ve liveness/readiness probe'larını (`GET /ping`, `GET /healthcheck`) 8080 portuna güncelleyin.
- İmajlar bilerek Debian tabanlıdır: ICU ve tzdata içerirler, böylece kültür, tarih ve saat dilimi işlemleri bir geliştirme makinesindeki gibi davranır. Alpine'e geçmeyin.
- `appsettings.Development.json` imaja kopyalanmaz.
- Açılışta iki uyarı beklenir ve zararsızdır: DataProtection anahtarlarının kalıcı olmadığı (ErtisAuth data protection kullanmaz) ve "Failed to determine the https port" (TLS ingress'te sonlanır).

### Repo yapısı

Projeler `src/`, test projeleri `tests/` altına taşındı. `Dockerfile` repo kökünde kalır ve `src/ErtisAuth.WebAPI/ErtisAuth.WebAPI.csproj` projesini derler. Proje yollarına doğrudan başvuran pipeline adımlarını güncelleyin.

### CI

`ErtisAuth.IntegrationTests` Docker'a (Testcontainers) ihtiyaç duyar. Docker'ın olmadığı ortamlarda testleri `Category=Integration` trait'i ile hariç tutun.

### Uygulama secret'ları

Her uygulamanın artık kendine ait bir Basic secret'ı var. Mevcut membership'ler, `AllowMembershipSecretForApplications` legacy anahtarı (yoksa açık) üzerinden uygulamalar için membership secret'ını kabul etmeye devam eder. Deploy'dan sonra:

1. Her uygulamanın secret'ını `POST /memberships/{membershipId}/applications/{id}/secret` ile yenileyin.
2. İstemcileri yeni secret'larla güncelleyin.
3. Membership için legacy anahtarını kapatın.

Yeni uygulamalar secret'larını oluşturma yanıtında **yalnızca bir kez** alır.

### Membership secret key uzunluğu

Bir membership'in `secret_key` değeri, membership'in encoding'inde en az 32 bayt olmalıdır (aksi halde oluşturma, güncelleme ve setup 400 döner). Token imzalayabilen mevcut membership'ler bu koşulu zaten sağlar.

### Setup endpoint'i

`POST /migrate` artık `POST /setup`. `ConnectionString` header'ının yerini `X-Setup-Token` aldı: operatörün önceden `setup` koleksiyonuna eklediği, en az 32 karakterlik bir token. Bu yalnızca yeni kurulumları ve kurulum script'lerini etkiler.

### Kendi host'unuz

WebAPI'yi kendi `Program.cs` dosyanızdan host ediyorsanız `app.UseProviders()` çağrısını kaldırın. Authenticator factory artık bir DI servisidir.

### MongoDB ve Linux çekirdekleri

MongoDB 8.0, 6.19 ve üzeri Linux çekirdeklerinde başlamayı reddeder (SERVER-121912). Node veya host çekirdeklerini yükseltmeden önce MongoDB'yi yükseltin (8.2 çalışır).

### Rate limiting

Anonim cihaz token endpoint'i `POST /memberships/{membershipId}/codes/token` için ingress'te (örneğin Istio/Envoy) bir rate limit uygulamayı değerlendirin.

---

## 3. Deploy sonrası

- **Açılış logunu kontrol edin:** `could not be created on users collection` ve diğer index hatalarını arayın. Önceki sürümde `users` üzerindeki index oluşturma her açılışta başarısız oluyordu; bu yüzden eksik index'ler şimdi oluşturulur. Ayrıca `roles`, `applications` ve `memberships` üzerinde text index'leri ve `active_tokens` üzerinde bir `refresh_token` index'i kurulur; büyük koleksiyonlarda bu zaman alır.
- **Webhook'lar yeniden çalışır.** Bu sürümden önce .NET 10 build'lerinde hiçbir webhook gönderilmedi ve özel mail hook'lar çözümlenmemiş `{{document.x}}` placeholder'ları gönderdi. Webhook alıcılarının trafiğe hazır olduğundan emin olun.
- **`sys` alanını doldurun.** Yayın öncesi .NET 10 build'lerinin oluşturduğu veya güncellediği roller, uygulamalar, membership'ler, kullanıcı tipleri, webhook'lar, mail hook'lar, sağlayıcılar ve kod politikaları `sys: null` olabilir. `sys.created_at` alanı olmayan belgelerde `created_at` değerini `_id` ObjectId zaman damgasından alın ve `created_by` değerini `"system"` yapın.
- **Uygulama secret'larını yenileyin** ve legacy anahtarını kapatın ([Uygulama secret'ları](#uygulama-secretları)).
- Deploy'dan önce yapmadıysanız **secret içeren olayları temizleyin** ([1.3](#secret-içeren-olaylar)).

---

## 4. API değişiklikleri (istemci uygulamalar)

### Kimlik doğrulama ve hata yanıtları

- Eksik `Authorization` header'ı **401** `AuthorizationHeaderMissing` döner (önceden 400). Her 401 yanıtı `WWW-Authenticate: Bearer realm="ErtisAuth"` taşır.
- Kimlik doğrulama sırasındaki veritabanı veya altyapı hataları **500** döner (önceden 401).
- 500 yanıtları genel bir mesaj taşır ("An unexpected error occurred"); ayrıntılar yalnızca sunucu loglarındadır.
- Çok alanlı doğrulama hatası (`errorCode: "ValidationException"`, örneğin geçersiz alanlar veya tekrarlanan bir kullanıcı adı, e-posta ya da unique alan) diğer tüm hatalar gibi camelCase'tir:
  ```json
  { "message": "...", "errorCode": "ValidationException", "statusCode": 400, "errors": [{ "message": "...", "fieldName": "...", "fieldPath": "..." }] }
  ```
  Önceden `Message`, `ErrorCode`, `StatusCode`, `Errors` idi.
- Kaldırılan hata kodları: `InvalidTokenCode`, `ApplicationSecretMismatch`, `UserTypePropertiesRequired`, `CommandRequired`. Bunları kontrol eden istemcileri güncelleyin.
- Değişen hata kodları:

  | Durum | Önce | Şimdi |
  |---|---|---|
  | Bilinmeyen cihaz kodu | 401 `InvalidTokenCode` | 404 `TokenCodeNotFound` |
  | Olmayan bir membership'i silme | `RoleNotFound` | `MembershipNotFound` |
  | Olmayan bir uygulamayı silme | `RoleNotFound` | `ApplicationNotFound` |
  | Olmayan bir sağlayıcıyı getirme | `ApplicationNotFound` | `ProviderNotFound` |
  | Aynı yetkiyi hem permitted hem forbidden içinde tutan rol | 400 `ModelValidationError` | 409 `RbacsConflicted` |
  | Geçersiz JSON gövdesi | 500 | 400 |

### Membership izolasyonu ve yetkilendirme

- Route'taki membership'i token'ın membership'inden farklı olan istekler **403** döner.
- `_query` filtreleri her zaman route'taki membership ile sınırlanır.
- Rolü olmayan kullanıcılar ve uygulamalar 403 ile reddedilir ([1.1](#rolü-olmayan-kullanıcılar-ve-uygulamalar)).
- Scoped bearer token'lar artık ErtisAuth endpoint'lerinde gerçekten scope'larıyla sınırlıdır; scope dışındaki çağrılar 403 döner.
- `GET roles/check-permission` yalnızca geçerli bir token gerektirir (`roles.read` gerekmez). Bir uygulama kendi kaydını `applications.read` olmadan okuyabilir.

### Token'lar ve oturumlar

- Refresh token JWT claim'i bir JSON boolean'dır: `"refresh_token": true` (önceden `"True"` string'i). JWT'yi decode edip string karşılaştıran istemciler uyarlanmalıdır. Eski token'lar hâlâ kabul edilir.
- `change-password` ve `set-password` (sıfırlama akışı), kullanıcının tüm access ve refresh token'larını iptal eder. Kendi şifresini değiştiren kullanıcı, isteği yaptığı oturumu korur. Diğer cihazlarda yeniden giriş gerekeceğini öngörün.
- `active-tokens/_aggregate` yalnızca izin verilen pipeline aşamalarını kabul eder; diğer aşamalar 400 döner.

### Şifre sıfırlama ve hesap aktivasyonu

- `reset-password`, host'u `X-Host` header'ında göndermelidir.
- Şifre sıfırlama ve aktivasyon linklerinin süresi tam olarak bitiş zamanında dolar (önceden 5 dakikaya kadar daha uzun kabul ediliyordu).
- `resend-activation-mail` başarıda `200 { emailAddress }` döner (önceki davranış tersti ve başarıda 401 dönüyordu).
- Zaten aktif bir kullanıcının aktivasyon linkini tekrar kullanmak `UserAlreadyActive` döner.

### Tek kullanımlık şifreler (OTP)

- `GET users/{id}/generate-otp`, `otp.create.{id}` yetkisi gerektirir (önceden yanlışlıkla `users.create.{id}`; bkz. [Tek kullanımlık şifre üreten roller](#tek-kullanımlık-şifre-üreten-roller)).
- `GET users/{id}/generate-otp` artık `token` döndürmez. Yanıt `{ _id, user_id, email_address, username, password, expires_in, created_at, expire_time, membership_id }` şeklindedir.
- Sıfırlama token'ını `POST verify-otp` verir (yanıt değişmedi). Token'ın ömrü doğrulama anında başlar.
- Bir kod **bir kez** çalışır: ikinci doğrulama `401 InvalidCredentials` döner.
- `generate-otp` her zaman yeni bir kod üretir ve öncekini geçersiz kılar (önceden aktif kodu tekrar döndürüyordu).
- Kodlar büyük/küçük harfe duyarsızdır. Harf ve rakam içeren kodlarda artık `0`, `O`, `1` veya `I` bulunmaz.
- `verify-otp`, kullanıcıyı güncel kullanıcı adı veya e-posta adresiyle (büyük/küçük harfe duyarsız) bulur. Ad değişikliğinden önce üretilmiş bir kod yeni adla doğrulanır; `generate-otp` yanıtındaki `username` ve `email_address` üretim anındaki değerlerdir.
- Membership OTP politikalarında `max_attempts` var (varsayılan 5).
- Membership secret'ını yenilemek aktif OTP'leri geçersiz kılar.

### Cihaz kodu akışı

Akış RFC 8628 çizgisinde yeniden tasarlandı. Eski route'lar **kaldırıldı**; cihaz uygulamaları ve onay sayfası sunucuyla birlikte geçiş yapmalıdır.

| Adım | Önce | Şimdi |
|---|---|---|
| Kod oluşturma | `POST codes` → 200 `{ code, ... }` | `POST codes` → **201** `{ user_code, device_code, status, expires_in, interval, created_at, expire_time, client_info }` |
| Cihazın sorgulaması | `GET codes/generate-token/{code}` | Her `interval` saniyede bir `{ "device_code": "..." }` ile `POST codes/token` |
| Kod bilgisini gösterme | | `GET codes/{user_code}` |
| Onaylama | `GET codes/approve/{code}` | `POST codes/{user_code}/approve` |
| Reddetme | | `POST codes/{user_code}/deny` |

Tüm route'lar `/memberships/{membershipId}/` altındadır.

- `device_code` yalnızca oluşturmada döner ve token bir kez verilir. Cihaz uygulamaları onu ilk başarılı yanıtta saklamalıdır.
- Yeni hatalar: `400 TokenCodeSlowDown` (çok sık sorgulama), `401 TokenCodeDenied`, `401 UserInactive`, `401 TokenCodeExpired`, `409 TokenCodeAlreadyAuthorized`.
- Kullanıcı kodundaki büyük/küçük harf, tire ve boşluklar yok sayılır.
- Harf ve rakam içeren kodlarda artık `0`, `O`, `1` veya `I` bulunmaz.
- Kodları bir cihaz adına bir backend oluşturuyorsa cihazın `X-IpAddress` ve `X-UserAgent` değerlerini iletin.
- Scoped bir token ile onaylanan kod, cihaza aynı scope'lara sahip bir token verir (ömrü `scoped_token_expires_in`; yenileme scope'ları korur).
- Membership'in kullandığı bir kod politikası silinemez (`409 TokenCodePolicyInUse`) ve yeniden adlandırıldığında slug'ını korur.

### Sağlayıcı ile giriş

- Giriş route'u `POST /oauth/{slug}/login`. Sabit `oauth/facebook|google|microsoft|apple/login` route'ları ve `?platform=` parametresi kaldırıldı. Facebook için `limited_flow=true` korunuyor.
- **iOS uygulamaları** AppleNative sağlayıcısının slug'ını çağırmalıdır (örneğin `oauth/applenative/login`). Hâlâ `oauth/apple/login?platform=ios` gönderen bir istemci reddedilmez: parametre yok sayılır ve giriş web Apple sağlayıcısına gider; orada native kodun değişimi başarısız olur (401/501).
- Web ve Android istemcileri `?platform=` parametresini kaldırabilir; yok sayılır.
- Facebook klasik girişi profili Graph API'den okur; bu yüzden e-posta istemciden değil Facebook'tan gelir. E-posta iznini vermemiş kullanıcıların e-postası olmaz: onlar için yeni kullanıcı oluşturma başarısız olur, zaten bağlı kullanıcılar etkilenmez.
- Apple kimliği istemciden değil Apple'ın `id_token`'ından gelir.
- Google: geçersiz bir token 401 döner (önceden 500). Eksik e-posta veya ad `401 ProviderProfileIncomplete` döner. İstemciler `email` ve `profile` scope'larını istemelidir.
- Apple: Apple'a ulaşılamadığında `503 ProviderUnavailable`, sağlayıcı konfigürasyonu veya kimlik bilgisi hatalarında `ProviderNotConfiguredCorrectly` döner (ikisi de önceden 401).
- Güvenilmeyen bir sağlayıcıdan gelen doğrulanmamış bir e-posta mevcut bir kullanıcıyla eşleştiğinde `409 ProviderEmailNotTrusted` döner ([trust_email](#sağlayıcılar-trust_email)).
- Yeni `400 InvalidProviderLoginRequest`; okunamayan bir Apple `id_token`'ı için de döner (önceden 500). `UnknownPlatform` kaldırıldı.
- `users.connected_accounts` öğeleri `/users` ve `/me` yanıtlarında da `{ provider, slug, user_id, token }` şeklindedir.

### Sağlayıcı yönetimi (admin)

- `POST /providers` bir sağlayıcı oluşturur ve 201 döner. JSON alanı `type` sağlayıcı tipini seçer ve zorunludur (`400 ProviderTypeRequired`, `400 UnsupportedProvider`).
- Slug değiştirilemez (`400 ProviderSlugCannotBeChanged`) ve membership içinde benzersizdir.
- `privateKey` olmadan yapılan `PUT providers/{id}` kayıtlı anahtarı korur (önceden siliniyordu).
- `active-providers`, `slug` ve `type` döndürür.
- `ProviderNameRequired` ve `UnknownProvider` hata kodları kaldırıldı.

### Sorgular (`_query`, `_aggregate`, arama)

- `_query` gövdesi **strict JSON** olarak parse edilir. Önceden kabul edilen esnek sözdizimi, örneğin tırnaksız alan adları (`{ where: { age: { $gt: 18 } } }`) veya tek tırnaklı string'ler, `400 InvalidQuery` döner. `Ertis.MongoDB.Queries` `QueryBuilder.ToString()` çıktısı MongoDB shell sözdizimindedir ve o da reddedilir.
- `users` ve `applications` sorguları `password_hash`/`secret_hash` üzerinde filtreleme veya sıralama yapamaz, `$expr`/`$jsonSchema` kullanamaz.
- `$where`, `$function` ve `$accumulator` her yerde reddedilir.
- Rol ve uygulama araması membership ile sınırlıdır ve büyük/küçük harfe duyarsızdır. `GET /memberships/search` yeniden çalışıyor.

### Kullanıcılar ve kullanıcı tipleri

- Başka bir tipin temel tipi olan kullanıcı tipi silinemez (`400 UserTypeCanNotBeDelete`) ve yeniden adlandırıldığında slug'ını korur.
- Tekrarlanan değerler varken bir alanı unique yapmak `409 UniqueFieldHasDuplicates` döner ve kullanıcı tipi kaydedilmez.
- Özel unique alanlar, alanı tanımlayan tip ve ondan türeyen tipler arasında benzersizdir (önceden: membership'in tüm kullanıcıları arasında).
- Unique alan içeren bir kullanıcı tipini oluşturmak veya güncellemek alanın index'ini kurar; büyük `users` koleksiyonlarında bu zaman alır.
- Eşzamanlı tekrarlanan yazmalar 400 unique doğrulama hatası döner.
- `username` ve `email_address` kırpılmış ve küçük harfle saklanır; giriş bunlarda büyük/küçük harfi yok sayar. Bu alanlar üzerindeki filtreler (`_query`) küçük harfli değeri kullanmalıdır.
- Başka bir kullanıcının e-posta adresi olan bir kullanıcı adı veya başka bir kullanıcının kullanıcı adı olan bir e-posta adresi 400 `ValidationException` ile reddedilir (`The 'username' field can not be the email_address of another user.`). Bir kullanıcının kullanıcı adı kendi e-posta adresi olabilir.
- `contentType` olmayan çoklu bir referans alanı verilen id'leri korur (önceden `[]` olarak kaydediliyordu). Bu şekilde kaybolan id'ler veritabanından geri getirilemez.

### REST kaynakları

- Kod politikaları, mail hook'lar ve kullanıcı tiplerinin oluşturulması ve güncellenmesi artık gövdede `_id`/`membership_id` gerektirmez; bunlar route'tan alınır.
- `PUT mailhooks/{id}` ve `PUT memberships/{id}` route'taki id'yi günceller; gövdedeki `_id` yok sayılır. Membership gövdesindeki `sys` yok sayılır.
- `POST /memberships` `_id` gerektirmez.
- Mail hook'lar oluşturmada `mailSubject`, `fromName`, `fromAddress` ve `recipients` gerektirir (400 `ModelValidationError`).

### Olaylar, webhook'lar ve mail hook'lar

- Kayıtlı olayların `document` ve `prior` alanları düz JSON'dur: `_id` bir string, tarihler ISO string'leridir (`{"$oid"}`/`{"$date"}` sarmalayıcıları değil).
- Token olayları yalnızca meta veri taşır:
  - `TokenGenerated`/`TokenVerified`: `token` alanında `token_type`, süre alanları ve `application_id` bulunur.
  - `TokenRefreshed`: `document` `{ user, token: { token_type, expires_in, refresh_token_expires_in, created_at } }` şeklindedir ve `prior` boştur.
  - `TokenRevoked`: `document` `{ token: { token_type, expire_time } }` şeklindedir.

  Token string'lerini okuyan şablonlar ve webhook'lar değişmelidir.
- `UserPasswordReset` artık `membership` içermez; olayın `membership_id` alanını kullanın.
- `WebhookRequestSent`/`WebhookRequestFailed`: `exception` `{ type, message }` (stack trace yok), `response` `{ isSuccess, statusCode, body }` şeklindedir. Ağ hataları yeniden denenir ve kaydedilir.
- Mail hook'lar: e-posta gövdesine eklenen değerler HTML-encode edilir ve konulardaki satır sonları kaldırılır.
- Webhook'lar: header değerleri encode edilmeden gönderilir, şablonlu URL'lerdeki değerler URL-escape edilir ve gövde değerleri JSON yapısını değiştiremez.

---

## 5. SDK değişiklikleri

`ErtisAuth.Sdk` yeni bir major sürüm alır.

- **Senkron metodlar kaldırıldı.** Async versiyonlarını kullanın.
- **Yeni paket `ErtisAuth.Sdk.AspNetCore`.** ASP.NET Core entegrasyonu oraya taşındı (namespace'ler `ErtisAuth.Sdk.AspNetCore.*`).
- **`ResetPasswordAsync(email, host, token)`:** `server` parametresi kaldırıldı, metod `IResponseResult` döndürür ve host `X-Host` header'ında gönderilir (eski SDK burada her zaman başarısız oluyordu).
- **`GetActiveTokensAsync`/`GetRevokedTokensAsync`** .NET 10 API'si ile çalışır (önceden 400 dönüyordu).
- **`RoleService.CheckPermission*`** 5xx yanıtlarında `false` döndürmek yerine exception fırlatır.
- **Authorization şeması** büyük/küçük harfe duyarsız eşleştirilir.

### `ErtisAuth.Sdk.AspNetCore` kullanan istemci uygulamalar

- Eksik `Authorization` header'ı **401** `AuthorizationHeaderMissing` döner.
- ErtisAuth'a ulaşılamadığında veya 5xx döndüğünde istemci uygulama **503** `AuthenticationServiceUnavailable` döner (önceden 401/403).
- `RbacSubject`, `RbacResource`, `RbacAction` ve `RbacObject` attribute'larındaki **rbac placeholder'ları** artık `{route::x}`, `{query::x}`, `{header::X}` ve `{env::X}` destekliyor. Öneksiz `{x}` eskisi gibi bir route değeridir.
  - Eksik veya boş bir `{env::X}` artık **403** döner. Önceden sessizce kaldırılıyordu: `{env::organization}:advertisers` ifadesi `:advertisers` oluyordu. Rbac attribute'larında kullanılan her environment variable'ın her deployment'ta tanımlı olduğundan emin olun.
  - Action'ın hiçbir route parametresiyle eşleşmeyen öneksiz bir placeholder `ERTISAUTH601` analyzer'ı tarafından raporlanır (warning).
  - Bilinmeyen bir önek 403 döner ve `ERTISAUTH602` analyzer'ı tarafından raporlanır.
  - Query veya header kaynaklı `RbacSubject` 403 döner ve `ERTISAUTH603` tarafından raporlanır.
  - Analyzer'lar pakete dahildir.
  - Çözümlenen placeholder değerleri tam olarak `*` veya `__all__` ise, `%2E` (büyük/küçük harf fark etmeksizin) içeriyorsa ya da boşluk veya kontrol karakteri içeriyorsa 403 ile reddedilir. Bu ErtisAuth'un kendi endpoint'leri için de geçerlidir; route, query veya header değerlerinde boşluk olabilecek istemcileri kontrol edin.
- **`AddErtisAuth()` ayarlarını host'un konfigürasyonundan** (`builder.Configuration`) okur; böylece uygulamanın tüm kaynakları geçerli olur: user secrets, komut satırı argümanları, Azure Key Vault ve host'un ortamı için `appsettings.{environment}.json` (`DOTNET_ENVIRONMENT` de dahil). Önceden SDK yalnızca çıktı dizinindeki `appsettings.json` ve `appsettings.{ASPNETCORE_ENVIRONMENT}.json` dosyalarını ve environment variable'ları okuyordu. Geçersiz ayarlar artık `AddErtisAuth()` çağrısında değil, uygulama başlarken hata verir. Host'u olmayan uygulamalar eski davranışı korur. Ayarlar yalnızca `appsettings.json` içinde veya environment variable'larda duruyorsa hiçbir şey değişmez.
- **Yeni overload `AddErtisAuth(IConfiguration)`** verilen bir section'ı okur, örneğin `builder.Services.AddErtisAuth(builder.Configuration.GetSection("Identity"))`.
- **`GetUtilizer()` yalnızca ErtisAuth tarafından kimliği doğrulanmış çağıranı döndürür.** `fallbackByToken` parametresi kaldırıldı. Kimlik doğrulaması yapılmayan endpoint'lerde (`[Unauthorized]` veya ErtisAuth attribute'u olmayan) `GetUtilizer()` artık `null` döner. Önceden isteğin token'ını doğrulamadan okuyordu; bu yüzden herkes kendi ürettiği bir token ile istediği kimliği iddia edebiliyordu. Eski davranışa ihtiyaç duyan kod, `GetUtilizer(fallbackByToken: true)` gibi çalışan **`GetUnverifiedUtilizer()`** metodunu çağırmalıdır. Sonucunu asla yetkilendirme kararları için kullanmayın. `fallbackByToken: false` ile yapılan çağrılar artık derlenmez; argümanı kaldırın.
- **Bir action'ın rbac attribute'u, controller'ının aynı attribute'unu geçersiz kılar.** Önceden controller'ın `[RbacResource]` attribute'u kazanıyordu; bu yüzden kendi `[RbacResource]` attribute'u olan bir action, controller'ın kaynağına göre kontrol ediliyordu. Bu tür action'ları çağıran rollerin yetkilerini kontrol edin.
- **Controller üzerindeki `[Unauthorized]` artık action'larını ezmiyor.** `[Authorized]`, `[SelfAuthorized]` ve `[Unauthorized]` arasından en spesifik olan uygulanır; bu yüzden `[Unauthorized]` bir controller'ın `[SelfAuthorized]` action'ı artık token gerektirir. Önceden böyle bir action herkese açıktı. Onu token olmadan çağıran istemciler artık **401** alır.
- **Yeni analyzer kuralları** derleme sırasında yetkilendirme hatalarını raporlar:
  - `ERTISAUTH610` (warning): bir action'ın rbac attribute'ları var, ancak ne action'da ne de controller'ında `[Authorized]` veya `[SelfAuthorized]` var; yani endpoint herkese açık.
  - `ERTISAUTH611` (warning): aynı seviyede çakışan `[Authorized]`, `[SelfAuthorized]` ve `[Unauthorized]`.
  - `ERTISAUTH612` (info): bir `[SelfAuthorized]` veya `[Unauthorized]` action'ın rbac attribute'ları kontrol edilmez.
  - `ERTISAUTH613` (warning): kimlik doğrulaması yapılmayan bir action `GetUtilizer()` çağırıyor; bu metod orada her zaman `null` döner. Yukarıdaki `GetUtilizer()` değişikliğinden etkilenen çağrıları bulur.

---

## 6. Geçişin bilinen yan etkileri

Bunlar kabul edilmiştir ve kullanıcıları bilgilendirmek dışında işlem gerektirmez:

- Deploy'dan önce gönderilen şifre sıfırlama ve aktivasyon linkleri çalışmaz. Kullanıcılar yeni link istemelidir.
- Deploy'dan önce iptal edilen token'lar, süreleri dolana kadar yeniden geçerli olur.
- Deploy anında açık olan cihaz kodları ve OTP'ler kaybolur (koleksiyonlar silinir).
- Kullanıcılar şifrelerini değiştirdikten sonra diğer cihazlarda yeniden giriş yapmalıdır.
