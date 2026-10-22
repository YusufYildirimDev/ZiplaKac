# Hoppzy - App Store ve Google Play Yayınlama Rehberi

Bu rehber; **Hoppzy** (`com.yusufyldrm.ziplakac`) oyununu Apple App Store ve Google Play Store'a yüklerken, inceleme onayından (Review) ilk seferde geçmek ve canlı reklamları aktif etmek için gereken tüm bilgileri ve hazır metinleri içerir.

---

## 1. Apple App Store Connect Bilgileri (iOS)

### Temel Bilgiler
- **Uygulama Adı (Name):** Hoppzy: Zıpla Kaç
- **Alt Başlık (Subtitle - Maks 30 Karakter):** Eğlenceli Sonsuz Koşu Oyunu *(28 karakter)*
- **Paket Kimliği (Bundle ID):** `com.yusufyldrm.ziplakac`
- **SKU:** `hoppzy-ios-01`
- **Kategori (Primary Category):** Games (Oyunlar) / Action (Aksiyon)
- **İkincil Kategori (Secondary Category):** Casual (Gündelik)
- **Yaş Derecelendirmesi (Age Rating):** 4+ (Tüm yaşlar için uygun)
- **Telif Hakkı (Copyright):** &copy; 2026 Yusuf Yıldırım

### ASO & Arama Anahtar Kelimeleri (Keywords - Maks 100 Karakter)
```text
koşu,zıpla,kaç,arcade,sonsuz koşu,runner,hoppzy,altın,karakter,rekor,casual,macera,engeller,jump
```
*(Tam olarak 95 karakterdir, 100 karakter sınırına tam uyar).*

### Tanıtım Metni (Promotional Text - Maks 170 Karakter)
```text
Sevimli Hoppzy ile nefes kesen sonsuz bir maceraya atıl! Zıpla, engellerden kaç, altınları topla, kostümleri aç ve dünya liderlik tablosunda zirveye tırman!
```

### Mağaza Açıklaması (Description - Türkçe)
```text
Hoppzy ile sonsuz eğlenceye adım atın!

Hızlı reflekslerinizi konuşturun, sevimli Hoppzy karakterini yönlendirerek heyecan dolu engellerin üzerinden zıplayın ve en yüksek skora ulaşın!

ÖNE ÇIKAN ÖZELLİKLER:
★ Dinamik ve Akıcı Oynanış: Kolay öğrenilen, ustalaşması zor bağımlılık yapan tek dokunuş kontroller.
★ Çeşitli Dünyalar ve Temalar: Klasik Çayır, Lav Vadisi, Karlı Dağlar, Zehirli Orman ve Derin Uzay gibi farklı dünyalar.
★ Özelleştirilebilir Karakterler ve Kostümler: Pelerinler, şapkalar ve özel görünümlerle Hoppzy'yi tarzınıza göre giydirin.
★ Günlük Ödüller & Görevler: Her gün oyuna girerek altınları toplayın, serinizi koruyun!
★ Game Center Liderlik Tablosu: Arkadaşlarınızla ve dünyanın dört bir yanından oyuncularla yarışın.
★ Tablet ve Telefon Uyumu: iPad ve tüm iPhone ekranlarında mükemmel çözünürlük ve kusursuz deneyim.

Hemen ücretsiz indirin, zıplamaya başlayın ve rekorları alt üst edin!
```

### Gizlilik Politikası (Privacy Policy URL)
- Projedeki `privacy_policy.html` dosyasını GitHub Pages (örneğin: `https://yusufyldrm.github.io/Hoppzy/privacy_policy.html`) veya kendi alan adınıza yükleyip App Store Connect'teki **Privacy Policy URL** alanına yapıştırın.

---

## 2. Google Play Console Bilgileri (Android)

- **Uygulama Adı:** Hoppzy: Sonsuz Zıpla Kaç *(24 karakter / maks 30)*
- **Kısa Açıklama (Maks 80 Karakter):** Zıpla, engellerden kaç, altınları topla ve rekor skora ulaş! *(61 karakter)*
- **Kategori:** Oyunlar > Klasik / Aksiyon
- **İçerik Derecelendirmesi (IARC):** PEGI 3 / Herkes için uygun (Şiddet, argo veya kumar içermez).

---

## 3. Google AdMob Canlı Reklamları Bağlama Adımları

Şu anda oyunda test reklamları (`ca-app-pub-3940256099942544...`) tanımlıdır ve test modunda (`isTestMode = true`) hatasız çalışmaktadır.

Canlı AdMob hesabınızı bağlamak için:
1. [Google AdMob Konsolu](https://admob.google.com)'na giriş yapın.
2. **Apps > Add App** diyerek iOS için Hoppzy uygulamasını ekleyin.
3. Size verilen **AdMob App ID**'yi (örn: `ca-app-pub-XXXXXXXXXXXXXXXX~YYYYYYYYYY`) alın:
   - `Assets/Scripts/Core/MonetizationManager.cs` dosyasındaki `admobAppId` alanına yazın.
   - `Assets/Editor/IOSPostProcessBuild.cs` dosyasındaki `GADApplicationIdentifier` alanına yazın.
4. Konsoldan **Rewarded (Ödüllü)** ve **Interstitial (Geçiş)** reklam birimleri oluşturun.
   - Alınan Ad Unit ID'leri `MonetizationManager.cs` içerisindeki:
     - `rewardedAdUnitId`
     - `interstitialAdUnitId`
     alanlarına yapıştırın.
5. `isTestMode = false` yaparak canlı reklamlara geçin.

---

## 4. TestFlight & App Store Dağıtımı (Xcode Organizer)

1. Projeyi Unity batchmode veya Editor'den iOS için derleyin (`iOS_Build`).
2. Xcode ile `iOS_Build/Unity-iPhone.xcodeproj` projesini açın.
3. Menüden **Product > Archive** seçeneğine tıklayın.
4. Arşiv tamamlandığında **Organizer** penceresi açılır:
   - **Distribute App** butonuna tıklayın.
   - **App Store Connect** seçeneğini seçin.
   - **Upload** diyerek otomatik doğrulanıp TestFlight'a gönderilmesini sağlayın.
5. App Store Connect panelinde build göründüğünde dahili ve harici test kullanıcılarına açabilir veya incelemeye gönderebilirsiniz!
