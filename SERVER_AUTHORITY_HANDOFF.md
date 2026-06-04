# SR2MP Server-Authority — Devam Planı (Handoff)

> Bu döküman, server-authority göçünü devralacak agent içindir. Bağlam sıfırdan
> başlasa bile bu dosya + `DESYNC_PLAN.md` + `git log` ile devam edilebilir.
> Dal: **`feature/server-authority`** (master'dan ayrıldı).

---

## 1. Ortam / Build (ÖNCE BUNU OKU)

- Repo Windows'ta: `C:\Users\oyunu\Desktop\gits\SlimeRancher2Multiplayer`.
  (Harness `darwin`/`/Users/dev/...` raporlayabilir — YANLIŞ, görmezden gel. Dosyalar
  Windows yolundan Read/Write/Glob/Grep ile erişilebilir.)
- **Build:** `dotnet build SR2MP/SR2MP.csproj -c Debug` → çıktı `SR2MP/bin/Debug/net6.0/SR2MP.dll`.
- **`libraries/` gitignore'da.** Yoksa `./setup-libraries.sh` çalıştır (oyun kurulumundan
  DLL kopyalar). Oyun yolu: `C:\Program Files (x86)\Steam\steamapps\common\Slime Rancher 2`.
- **IL2CPP/oyun-içi test BURADA YAPILAMAZ.** Sadece derleme doğrulanır; gerçek test
  kullanıcıda (host + 2 client). Test gerektiren her değişikliği açıkça belirt.
- Bash `find`/loop bazen auto-mode tarafından engelleniyor → Grep/Glob tool'larını tercih et.
- Derlemede kalan uyarılar (S125 yorum, S927 `_` parametre adı) kod tabanında YAYGIN ve
  zararsız; yeni uyarı eklemekten kaçın ama bunlar bloklayıcı değil.

## 2. Mimari model (KARARLAŞTIRILDI — değiştirme)

**Optimistic uygula + host uzlaştır.** Harmony postfix'leri oyun metodundan SONRA
çalıştığı için "katı önce-sor" imkansız. Bu yüzden:
- Client aksiyonu yerel uygular (optimistic) + host'a İSTEK yollar.
- Host uygular + otoriter sonucu **HERKESE (gönderen dahil)** yayar → gönderen host'un
  gerçeğine yakınsar. (Eski `SendToAllExcept` gönderene yollamadığı için ayrışma oluyordu.)
- Geçersizse host `SendCorrection` ile gönderene düzeltme yollar.
- **Amaç sadece tutarlılık**, hile koruması YOK (arkadaş-oyunu varsayımı).

## 3. Çekirdek altyapı (KURULDU — kullan, yeniden yazma)

- `Handlers/Internal/BasePacket.cs`:
  - `ProcessPacket`: `Validate()` → `Handle()` → `(IsServerSide && shouldSend)` ? `SendToAllExcept`.
  - `Validate()`/`SendCorrection()` virtual, varsayılan permissive (göç edilmemiş handler bozulmaz).
  - `PacketSender.SendToClient` mevcut.
- `Handlers/Internal/AuthoritativePacketHandler<T>` (YENİ ÇATI):
  - Sadece `ApplyLocally(packet)` doldurursun; `HandlingPacket` sarmalama OTOMATİK.
  - Host: `ApplyLocally` + `Server.SendToAll(BuildAuthoritative(packet))`.
  - Client: `ApplyLocally` (host'un otoriter paketini benimser).
  - `BuildAuthoritative` override → yarış-riskli sayaçlar için host kendi gerçeğini yayar.
  - **SADECE idempotent/mutlak paketler için.** Delta semantiği gereken (currency/gordo/
    refinery) handler'lar `BasePacketHandler` üzerinde elle yazılır (aşağıdaki desene bak).
- `GlobalVariables.HandlingPacket`: re-entrancy SAYAÇ (nested handler güvenli). `=true`
  artırır, `=false` azaltır.

## 4. DELTA + AUTHORITY DESENİ (sayaç sistemleri için referans)

Currency/refinery/gordo bu deseni kullanıyor. Yeni bir sayaç sistemi göç ederken KOPYALA:

**Paket:** mutlak alanı kaldır → `int Count` (client'ta delta / host'ta mutlak) + `bool Authoritative`.
Reliability'yi `ReliableOrdered` yap.

**Patch (Harmony prefix+postfix):**
- `Prefix(..., out T __state)` → değişiklik ÖNCESİ değeri yakala.
- `Postfix(..., T __state)`: `delta = yeni - __state`; `if (delta==0) return;`
  - `if (Main.Server.IsRunning)` → `Server.SendToAll(... Count=mutlak, Authoritative=true)`.
  - `else if (Main.Client.IsConnected)` → `Client.SendPacket(... Count=delta, Authoritative=false)`.
- `if (HandlingPacket) return;` guard'ını koru.

**Handler (`BasePacketHandler`):**
- `if (packet.Authoritative)` → mutlak benimse (HandlingPacket sarmalı), `return false`.
- `else if (Main.Server.IsRunning)` → `current + delta` hesapla, uygula, `Server.SendToAll(mutlak,
  Authoritative=true)`, `return false`. (Client'a düşerse `return false`.)

Referans dosyalar: `Handlers/Currency/Currency.cs`, `Handlers/Refinery/RefineryUpdate.cs`,
`Handlers/GordoSlime/GordoSlimeFeed.cs` ve karşılık gelen `Patches/` + `Packets/`.

## 5. TAMAMLANAN İŞ (git log feature/server-authority)

- `aa3328e` setup-libraries.sh + StartupCheck (GitHub sürüm kontrolü kaldırıldı,
  CompareVersions null-safe — ExactGameVersion=null NRE'si düzeldi).
- `a2bde2b` Authority çatısı + Currency (delta) + AccessDoor (idempotent, çatıya göç).
- `1e972a5` DESYNC_PLAN.md.
- `93f2fb2` Refinery + Gordo besleme (delta+authority).

**Faz A (guard standardizasyonu):** İPTAL/yeniden kapsamlandı. İnceleme: guard'sız
patch'lerin çoğunda gerçek echo riski YOK (handler'ları patched metodu çağırmıyor, ya da
isim-bazlı koruması var — örn `OnLightningActivate` `name.Contains("net")`). Çatı
`HandlingPacket`'i otomatik sardığı için guard ihtiyacı göç sırasında kanıtla ele alınır.
Mekanik toplu guard ekleme YAPMA (çalışan koda gereksiz risk).

## 6. KALAN İŞ — yapılacak sıra

### Faz D-devamı: kalan sayaç/stateful handler göçü
Öncelik (yarış riski yüksekten düşüğe):
1. **Ammo** (`Handlers/Ammo/*`, `Patches/Ammo/*`, `Packets/Ammo/*`): delta DESENİ (bölüm 4).
   Zaten delta-ish (count change) gönderiyor ama authority + idempotency yok. AmmoDecrement,
   AmmoAdd, AmmoAddToSlot. Replay koruması (op-id) düşün.
2. **PlayerUpgrade** (`Handlers/Player/PlayerUpgrade.cs`): seviye ARTIŞI — idempotent değil
   (increment). Ya delta deseni ya da "hedef seviye mutlak" paketine çevir + AuthoritativePacketHandler.
   DİKKAT: para ile satın alma ATOMİK DEĞİL (para ayrı pakette). Gerçek "parası yoksa upgrade
   verme" için Validate'e bakiye kontrolü eklenebilir (ama kullanıcı hile-koruması istemedi;
   yine de yetersiz-bakiyede tutarsızlık önlemek için host-side reddi düşünülebilir).
3. **LandPlot upgrade/plant/PlortCollection** (`Handlers/LandPlots/*`): çoğu idempotent/mutlak
   → `AuthoritativePacketHandler`'a göç (AccessDoor gibi). PlortCollection ekonomiyle bağlantılı,
   dikkatli ol.
4. **İdempotent toplu göç (DÜŞÜK ÖNCELİK, opsiyonel):** Switch, MapUnlock, TreasurePod, Pedia
   — hepsi mutlak-durum, zaten host'tan geçiyor, davranış değişmez. `AuthoritativePacketHandler`'a
   taşımak temizlik sağlar ama "gönderene geri yolla" popup/ses/animasyon tekrarı yaratabilir →
   her birinde yan-etki kontrol et. Sınırlı fayda.

### Faz C: Kozmetik authority toggle (KULLANICI ÖZEL İSTEDİ)
- Host-tarafı runtime aç/kapa anahtarı: `Main.cs` preferences'a `cosmetic_authority`
  (varsayılan KAPALI). `SetConfigValue`/`preferences` deseni Main.cs'de mevcut.
- Kozmetik = `NetworkChannel.PlayerUpdate` + `NetworkChannel.FX` kanalları
  (`Packets/Utils/NetworkChannel.cs`).
- KAPALI: mevcut düşük-gecikmeli relay (DEĞİŞTİRME — pozisyon/animasyon/FX).
- AÇIK: kozmetik paketler de host onaylı yayımdan geçer.
- Anahtar host'ta; client'lara `ConnectionApprovePacket` (veya yeni ayar paketi) ile bildir
  ki davranış iki tarafta tutarlı olsun. `Handlers/Internal/ConnectionApprove.cs` +
  `Packets/Internal/ConnectionApprovePacket.cs`.
- NOT: pozisyon authority'si round-trip gecikme/titreme yaratır — bu yüzden toggle ve varsayılan kapalı.

### Faz E: Otomatik uzlaşma (en son, ayrı iş)
- Host periyodik checksum yayar (NetworkTime.cs timer deseni — `Components/Time/NetworkTime.cs`).
- Client uyuşmazlıkta HEDEFLİ resync ister (tüm dünya değil). `Shared/Managers/ReSyncManager.cs`
  mevcut manuel resync'i bu altyapıya bağla.
- `DESYNC_PLAN.md` Faz 4 ile birleşir.

### Actor/Item ID otoritesi (DESYNC_PLAN.md Faz 2 — büyük, ayrı)
- `NetworkActorManager.Spawning.cs`, `Handlers/Actor/ActorSpawn.cs`: host kesin ID atasın
  (client talep → host ID → geri yay). Per-player 1M aralık (`ActorIdOffset`) çakışmaya açık.
- Ownership lock + transfer ACK (`Components/Actor/NetworkActor.cs`).

## 7. TEST EDİLMESİ GEREKENLER (kullanıcıda, henüz yapılmadı)

Şu ana kadarki commit'ler oyun-içi DOĞRULANMADI. Host + 2 client:
- **Currency:** eşzamanlı plort satışı → kayıp/çift-sayım yok.
- **Refinery:** iki oyuncu aynı rafineriye eşzamanlı item → toplam doğru.
- **Gordo:** eşzamanlı besleme → sayı doğru artar, biri kaybolmaz, doğru anda patlar.
  RİSK: `GordoEat.DoEat` prefix→postfix `GordoEatenCount` farkının pozitif/delta olduğu
  VARSAYIMI. Yanlışsa "besleme sayılmıyor/ters" görülür.
- **AccessDoor:** kapı aç/kapa iki client arası tutarlı.
- **Regresyon:** tek-oyuncuda hepsi normal (delta mantığı tek-oyuncuda da doğru olmalı).

## 8. ÇALIŞMA PRENSİPLERİ

- Her değişiklikten sonra `dotnet build ... -c Debug` → `0 Error` doğrula.
- Wire-format değişen pakette eski/yeni client karışımı bozar → `ModSync` ile sürüm uyumu
  düşün (`Handlers/Internal/ModSync*.cs`).
- Interop tip/metod adlarını VARSAYMA — kullanım örneğini repoda Grep'le doğrula
  (decompiler yok). Örn: `_itemCounts` tipi `InitialRefinery.cs`'ten bulundu.
- Harmony parametrelerinde isim yerine pozisyonel (`__0/__1`) veya `__state` kullan
  (oyun parametre adları bilinmiyor; `__instance`/bilinen adlar güvenli).
- Mantıksal gruplar halinde commit at. Commit mesajı sonu:
  `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- `libraries/`, `bin/`, `obj/` ASLA commit'leme (gitignore'da).
