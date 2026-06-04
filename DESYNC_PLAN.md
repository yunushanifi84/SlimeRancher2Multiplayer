# SR2MP — Server-Authority'ye Geçiş & Desync Eliminasyon Planı

> Amaç: Oyuncuların **aynı dünyayı** deneyimlemesi. Mevcut "best-effort relay"
> modelinden, **host'un tek doğruluk kaynağı (authoritative)** olduğu +
> **kendini düzelten (self-healing)** bir modele geçiş.

## Kapsam kararları (kullanıcı onaylı)
- **İnvazivlik:** Köklü mimari değişiklik serbest.
- **Öncelik alanları:** Ekonomi · Item/Actor tutarlılığı · Otomatik kendini düzeltme · Slime davranışı (hepsi).
- **Otorite modeli:** Tam server-authority'ye geçiş.

---

## Mevcut durumun temel kusurları (kodla doğrulandı)
1. **Server otorite değil** — `Handlers/Internal/BasePacket.cs:24-31`: handler `true`
   dönerse körü körüne `SendToAllExcept` yapıyor, sıfır doğrulama. Kötü niyetli/buggy
   client ne gönderirse uygulanıyor.
2. **Mutlak değer ezme** — currency/gordo/refinery "yeni toplam" gönderiyor (delta değil).
   Eşzamanlı işlemde son yazan kazanıyor, diğer işlem kayboluyor.
   - `Handlers/Currency/Currency.cs`, `Handlers/GordoSlime/GordoSlimeFeed.cs`,
     `Handlers/Refinery/RefineryUpdate.cs`
3. **Reconciliation yok** — sadece manuel resync (`ReSyncManager.cs`, 2dk cooldown).
   Kaçan tek olay = manuel resync'e kadar kalıcı sapma. Otomatik periyodik düzeltme yok.
4. **Actor ID atomik değil** — player başına 1M aralık (`ActorIdOffset`), ama spawn
   anında cross-client atomik rezervasyon yok → çakışma mümkün.
   - `Shared/Managers/NetworkActorManager.Spawning.cs`, `ReSyncManager.cs:309-339`
5. **`HandlingPacket` thread-safe değil** — global tek `bool` (`GlobalVariables.cs:60`),
   eşzamanlı handler'da echo-loop koruması teorik olarak delinebilir.

### Doğrulanmış sağlam taraflar (korunacak)
- Player pozisyon/animasyon: 8Hz, Ordered, snap-threshold'lu interpolasyon (`NetworkPlayer.cs`).
  **Authority'ye taşınmayacak** — mevcut akış doğru.
- Zaman senkronu host-authoritative (`NetworkTime.cs`, 0.85s, Unreliable).
- Reliability katmanı (ACK/resend/reorder) sağlam (`ReliabilityManager.cs`).

---

## FAZ 0 — Altyapı: Authority & Reconciliation çatısı
*Diğer her şeyin üstüne kurulacağı temel. Davranış değişikliği minimum.*

- **0.1 — Sunucu doğrulama kancası.** `BasePacket.cs`'e `Handle()` öncesi
  `Validate(packet, clientEp)` adımı. Server tarafında geçersizse: uygulama, yayma,
  ve isteyen client'a **düzeltici (corrective) paket** geri gönder. Mevcut handler'lar
  varsayılan `Validate => true` ile bozulmadan çalışır (kademeli geçiş).
- **0.2 — `HandlingPacket` → re-entrancy-safe.** Global `bool` yerine `[ThreadStatic]`
  veya re-entrancy sayacı.
- **0.3 — `StateVersion` / dirty-tracking iskeleti.** Her kritik alt-sistem için host'ta
  monoton artan sürüm numarası. Reconciliation kararının temeli.
- **0.4 — Periyodik reconciliation tick'i.** `NetworkTime.cs` timer desenini örnek alarak
  host'ta düşük frekanslı (5-10sn) "delta reconcile" döngüsü. Tüm dünyayı değil, sadece
  **değişen/uyuşmayan** parçayı gönderir.

## FAZ 1 — Ekonomi otoritesi (en yüksek öncelik)
- **1.1 — Currency: delta + authority.** `CurrencyPacket` "mutlak toplam" yerine
  **delta talebi**. Client talep eder; **host kendi toplamına uygular, resmi toplamı yayar**.
  Eşzamanlı satışta kayıp ortadan kalkar.
  - `Packets/Economy/CurrencyPacket.cs`, `Handlers/Currency/Currency.cs`,
    `Patches/Economy/CurrencyPatch.cs`
- **1.2 — Plort satış/toplama atomik.** Plort parası **sadece host'ta** kaydedilsin;
  client'lar host onayıyla görsün. Çift-sayım kapanır.
  - `Handlers/LandPlots/PlortCollection.cs`, `Patches/LandPlots/OnPlortCollection.cs`
- **1.3 — Market fiyatları.** Zaten read-only authoritative; sürüm damgası ile tutarlılık.

## FAZ 2 — Actor/Item tutarlılığı
- **2.1 — Authoritative ID tahsisi.** Spawn'da client "talep" gönderir, **host kesin ID
  atayıp geri yayar**. Çakışma imkansız.
  - `NetworkActorManager.Spawning.cs`, `Handlers/Actor/ActorSpawn.cs`, `ActorSpawnPacket`
- **2.2 — Ownership lock + transfer ACK.** Devirde çift-sahiplik penceresini host-aracılı
  kilitle kapat. Hibernation/ownership yarışını çöz (`Components/Actor/NetworkActor.cs`).
- **2.3 — Periyodik actor seti reconcile.** Faz 0.4 tick'i: host "yaşaması gereken actor
  ID seti"ni özetler (hash); client'ta eksik/fazla düzeltilir → hayalet/kayıp item temizlenir.

## FAZ 3 — Sayaç-tabanlı sistemler (gordo, refinery, ammo)
- **3.1 — Gordo besleme delta + authority.** "+N yedirme talebi"; host sayar, yayar.
  (`Handlers/GordoSlime/GordoSlimeFeed.cs`)
- **3.2 — Refinery delta + authority.** Aynı desen. (`Handlers/Refinery/RefineryUpdate.cs`)
- **3.3 — Ammo idempotency.** Delta var ama replay koruması yok → op-id ekle.
  (`Handlers/Ammo/*`)

## FAZ 4 — Otomatik kendini düzeltme
- **4.1 — Per-subsystem checksum yayını.** Host her tick alt-sistem özetlerini yayar
  (para hash, actor-set hash, gordo durumları). Client uyuşmazlıkta o alt-sistem için
  **hedefli resync** ister — tüm dünya değil.
- **4.2 — Manuel resync'i bu altyapıya bağla.** `ReSyncManager` "uyuşmayanı gönder"
  kullanır; manuel resync acil fallback olarak kalır.

## FAZ 5 — Slime davranışı (en zor, en sona)
- **5.1 — Slime ownership modeli.** Actor ownership desenini slime'lara genişlet:
  her slime tek authoritative peer'da simüle, diğerleri interpolasyon.
- **5.2 — Diyet/largo/üreme senkronu.** Şu an hiç senkron değil; host-otoriteli olaylara bağla.
- **5.3 — Kademeli rollout.** Önce 1-2 tür (Dervish/Yolky deseni gibi), sonra genişlet.

---

## Çalışma prensipleri
- **Geriye dönük uyumlu geçiş:** Faz 0 `Validate` varsayılan-geç → handler'lar tek tek
  taşınır; her faz bağımsız test edilebilir.
- **Protokol sürümü:** Wire-format değişen her pakette versiyon uyumsuzluğunu `ModSync`
  ile engelle (eski/yeni client karışmasın).
- **Test stratejisi:** IL2CPP/Unity → otomatik test sınırlı. Her faz host+2 client
  senaryosuyla manuel doğrulanacak (eşzamanlı satış, eşzamanlı gordo besleme, item drop
  yarışı). Test edilemeyen yerler açıkça belirtilecek.
- **Risk:** Authority gecikme ekler (client host onayını bekler). Para için kabul edilebilir;
  **pozisyon/animasyon authority'ye taşınmaz** (mevcut interpolasyonlu unreliable akış kalır).

## Başlangıç önerisi
**Faz 0 + Faz 1 (ekonomi)** ilk iş paketi — en yüksek değer, izole, geri kalanın
altyapısını kurar.

## Açık sorular
- Build/test: IL2CPP modu burada derlenip oyunda test edilemez → derleme/oyun-içi test
  kullanıcıda. Statik/derleme doğrulaması yapılabilirse yapılacak.
- Teslim biçimi: ayrı PR'lar mı, tek dal mı? (Öneri: faz başına PR)
