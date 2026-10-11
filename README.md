# Zıpla Kaç — Unity 2D Mobil Endless Runner Projesi

**"Zıpla Kaç"**, iOS ve Android platformları için ticari yayınlama standartlarına uygun olarak C# ve Unity 2D (Physics2D + Canvas UI) mimarisiyle geliştirilmiş tam donanımlı bir endless runner mobil oyunudur.

---

## 🌟 Öne Çıkan Özellikler

1. **Tek Dokunuş & Çift Zıplama (Double Jump) Fiziği**:
   - Ekrana dokunarak veya klavyeden `Boşluk` (Space) tuşuna basarak zıplama.
   - Havadayken 2. (daha kısa) zıplama hakkı. Yere değince zıplama hakkı otomatik sıfırlanır.
   - `Rigidbody2D` ve `OverlapCircle` zemin algılama mimarisi.

2. **4 Dinamik Zorluk Kademesi (Tier 0–3) ve Tema Geçişleri**:
   - **Tier 0 (Skor 0–39)**: Gündüz Teması (Açık Mavi). Kaktüs (%70) ve Kaya (%30).
   - **Tier 1 (Skor 40–99)**: Günbatımı Teması (Turuncu/Kızıl). Yeni: Sivri Diken (%25).
   - **Tier 2 (Skor 100–199)**: Alacakaranlık Teması (Mor/Lacivert). Yeni: Alçak Kuş (%20).
   - **Tier 3 (Skor 200+)**: Yıldızlı Gece Teması (Siyah/Lacivert). Yeni: Yüksek Kuş (%20).
   - Skor ve hız arttıkça engel spawn aralığı dinamik olarak kısalır (minimum alt sınır korumalı).

3. **Güçlendirme (Power-up) Sistemi**:
   - **Kalkan (Shield - S)**: Bir sonraki darbeyi emer, karaktere parlak halka ekler, engeli yok eder ve ~0.3sn dokunulmazlık verir.
   - **Yavaşlatma (Slow - T)**: 4 saniye boyunca engellerin/güçlendirmelerin ekrandaki hızını %50 azaltır (skor hızını düşürmez).
   - **x2 Skor (Multi - 2X)**: 5 saniye boyunca kazanılan puanı 2 katına çıkarır.
   - Ekranda aynı anda en fazla 1 adet aktif güçlendirme belirir. HUD üzerinde kalan süre sayaçlı rozetler yer alır.

4. **Kombo Sistemi**:
   - Art arda engel geçildikçe kombo artar. Her 5 engelde bir **+10 Bonus Puan** verilir ve ekranda `"Kombo xN! +10"` floating text animasyonu belirir.
   - Kalkan harcandığında kombo sıfırlanır.

5. **5 Varyantlı Karakter Galerisi (PlayerPrefs Kayıtlı)**:
   - Klasik (Sarı), Ateş (Turuncu), Buz (Mavi), Zehir (Yeşil) ve Gece (Mor) karakter varyantları.
   - Vurgulu çerçeve ve ölçekleme animasyonu. Dokunulduğunda oyunu başlatmaz, seçimi günceller ve cihazda saklar.
   - `CharacterData` ScriptableObject yapısı ile IAP ve Reklam kilitlerine tam uyumluluk.

6. **Ses, Haptik ve Skor Kaydı**:
   - Prosedürel sentetik ses sentezleyicisi (Harici ses dosyası olmasa dahi Zıplama, Çift Zıplama, PowerUp, Kalkan Darbesi, Kombo, Oyun Bitti ve Yeni Rekor seslerini üreterek oynatır!).
   - Mobil cihazlarda `Handheld.Vibrate()` haptik titreşim desteği.
   - `PlayerPrefs` tabanlı en yüksek skor ve son 5 oyun skor geçmişi kaydı.

7. **Ticari Monetization Mimarisi**:
   - Google AdMob (Ödüllü Reklam ile "Devam Et / Reklam İzle" ve Geçiş Reklamları) ve Unity IAP (Karakter Kilidi Açma / Reklamsız Paket) için modüler `MonetizationManager` stub sınıfı.

---

## 🚀 Projeyi Unity'de Açma ve Çalıştırma

1. **Unity Hub** uygulamasını açın.
2. **Open / Add project from disk** butonuna tıklayın ve bu klasörü (`ZiplaKac/`) seçin.
3. Projeyi **Unity 2022.3 LTS** veya daha yeni bir Unity 2D sürümü ile açın.
4. Herhangi bir boş sahne açıp `Play` butonuna basın! Proje içindeki `GameBootstrap` betiği, tüm sahneleri, kameraları, UI Canvas'ı, nesne havuzlarını ve karakterleri çalışma zamanında otomatik olarak oluşturacaktır.

---

## 🛠 Proje Dosya Yapısı

```text
ZiplaKac/
├── Assets/
│   └── Scripts/
│       ├── Core/
│       │   ├── GameManager.cs         # Oyun durum makinesi, skor, tier ve kombo hesabı
│       │   ├── ObjectPooler.cs        # Nesne havuzlama (Instantiate/Destroy sıfır amaca yönelik)
│       │   ├── SaveSystem.cs          # PlayerPrefs tabanlı skor geçmişi ve seçim kaydı
│       │   ├── AudioManager.cs        # Prosedürel ses sentezleyici ve haptik titreşim
│       │   └── MonetizationManager.cs # AdMob & IAP ticari altyapı stübu
│       ├── Player/
│       │   ├── CharacterData.cs       # Karakter ScriptableObject verisi
│       │   └── PlayerController.cs    # Zıplama, çift zıplama ve kalkan mantığı
│       ├── Obstacles/
│       │   ├── ObstacleData.cs        # Engel parametreleri
│       │   ├── TierData.cs            # Kademe eşikleri ve ağırlıklı rastgele havuz
│       │   ├── Obstacle.cs            # Ekran içi kayma ve kombo algılama
│       │   └── ObstacleSpawner.cs     # Zorluğa göre dinamik spawn döngüsü
│       ├── PowerUps/
│       │   ├── PowerUpData.cs         # Güçlendirme verileri
│       │   ├── PowerUp.cs             # Yüzen (bobbing) daire hareketi ve toplama
│       │   └── PowerUpSpawner.cs      # 8-14sn periyotlu max 1 aktif nesne üretici
│       ├── UI/
│       │   ├── UIManager.cs           # Panel geçişleri ve dinamik arka plan temaları
│       │   ├── CharacterSelectorUI.cs # Karakter galerisi UI
│       │   ├── HUDController.cs       # Oyun içi skor ve güçlendirme rozetleri
│       │   ├── GameOverUI.cs          # Final skor, son 5 skor ve yeni rekor rozeti
│       │   └── FloatingText.cs        # Kombo floating metin efekti
│       └── Utils/
│           ├── ProceduralSpriteGenerator.cs # 2D Şekil ve görsel üretici
│           └── GameBootstrap.cs             # Otomatik sahne kurulum betiği
└── README.md
```

---

## ➕ Yeni İçerik Ekleme Rehberi

### Yeni Bir Karakter Ekleme
1. `Assets/Scripts/Player/CharacterData.cs` sınıfından türetilen yeni bir `CharacterData` ScriptableObject oluşturun (veya `PlayerController.cs` içindeki `LoadCharacterData` switch blokuna yeni `characterId` ekleyin).
2. Gövde rengini (`bodyColor`), parlama rengini (`glowColor`) ve kilit açma yöntemini (`unlockMethod`) belirleyin.
3. `CharacterSelectorUI.cs` içindeki `characterCards` listesine yeni kartı tanımlayın.

### Yeni Bir Engel Ekleme
1. `ObstacleType` enum yapısına yeni engel adını (örn. `RollingStone`) ekleyin.
2. `ObstacleSpawner.cs` içinde ilgili `poolTag` ve Y ofset değerini belirleyin.
3. İlgili zorluk seviyesindeki `TierData` ağırlık listesine (`obstacleWeights`) yeni engeli ve ihtimal ağırlığını (weight) ekleyin.

---

## 📱 Mobil Derleme (Build) Talimatları

### Android
1. `File -> Build Settings` menüsünden platformu **Android** olarak değiştirin (`Switch Platform`).
2. `Player Settings -> Identification` altında Package Name bilgisini girin (örn. `com.company.ziplakac`).
3. **Build and Run** butonuna basarak APK veya AAB çıktısı alın.

### iOS
1. Platformu **iOS** olarak değiştirin.
2. `Build Settings -> Build` butonuna basarak Xcode projesini oluşturun.
3. Xcode üzerinden sertifika ve provisioning profile tanımlayarak cihazınıza derleyin.
