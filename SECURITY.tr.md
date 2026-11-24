# Güvenlik Politikası

[English](SECURITY.md) | **Türkçe**

## Desteklenen sürümler

Güvenlik düzeltmeleri yalnızca en son sürüm için yapılır. Daha eski bir sürüm kullanıyorsanız önce yükseltin; [MIGRATION.tr.md](MIGRATION.tr.md) .NET 10 sürümüne geçişi anlatır.

| Sürüm | Destekleniyor |
|---|---|
| En son sürüm (.NET 10) | Evet |
| Önceki sürümler (.NET 9 ve öncesi) | Hayır |

## Güvenlik açığı bildirme

**Lütfen güvenlik açıklarını herkese açık issue'larda, discussion'larda veya pull request'lerde bildirmeyin.**

Açıkları GitHub üzerinden gizli olarak bildirin: reponun [Security sekmesini](https://github.com/ertugrulozcan/ErtisAuth/security) açın ve **Report a vulnerability** seçeneğini seçin. Bildirimi yalnızca proje sorumluları görebilir.

Lütfen şunları ekleyin:

- Etkilenen sürüm veya commit.
- Açığın ve etkisinin açıklaması: bir saldırganın neler yapabileceği ve hangi koşullarda (örneğin hangi role veya ne tür bir token'a ihtiyaç duyduğu).
- Tekrar üretme adımları, mümkünse açığı gösteren isteklerle birlikte.
- Bilinen bir geçici çözüm varsa o.

## Sizi neler bekliyor

- Bildiriminizin alındığı onaylanır ve açık incelenip düzeltilirken bilgilendirilirsiniz.
- Bir düzeltme yayınlandığında açık, bir GitHub security advisory olarak yayınlanır. Anonim kalmayı tercih etmediğiniz sürece advisory'de adınız anılır.
- Açığı herkese açık olarak duyurmadan önce lütfen bir düzeltme yayınlamamız için makul bir süre tanıyın.

## Kapsam

Kapsam dahilinde:

- ErtisAuth sunucusu (`ErtisAuth.WebAPI` ve onu oluşturan kütüphaneler).
- İstemci paketleri `ErtisAuth.Sdk` ve `ErtisAuth.Sdk.AspNetCore`.

Kapsam dışında:

- Sunucuyu, veritabanını veya bir yönetici hesabını zaten kontrol eden bir saldırgan gerektiren durumlar.
- Belirli bir deployment'ın zayıflıkları; örneğin eksik bir TLS konfigürasyonu, dışarıya açık bir MongoDB veya gateway'de rate limiting olmaması. Aşağıdaki önerilere bakın.
- İstek hacmiyle yapılan hizmet engelleme (denial of service).

## Deployment önerileri

ErtisAuth birkaç korumayı üzerinde çalıştığı altyapıya bırakır:

- **TLS:** HTTPS'i ingress'te veya reverse proxy'de sonlandırın; ErtisAuth'un kendisi düz HTTP dinler.
- **Rate limiting:** `generate-token`, OTP ve şifre sıfırlama endpoint'leri ile cihaz token endpoint'i gibi anonim endpoint'leri gateway'de sınırlayın.
- **MongoDB:** veritabanını özel bir ağda tutun ve ErtisAuth'a yalnızca kendi veritabanına erişimi olan bir kullanıcı verin.
- **Secret'lar:** en az 32 baytlık bir membership secret key kullanın, uygulama secret'larını bilen biri ayrıldığında yenileyin ve tek seferlik setup token'ını loglardan ve script'lerden uzak tutun.
