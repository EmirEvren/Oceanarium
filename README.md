# Oceanarium Fishing

## Active milestone: ASTRA 0.0

The active production foundation is **Assets/OceanariumFishing**, built in the existing Unity 6000.3.13f1 project. Open `Assets/OceanariumFishing/Scenes/OF_HomeIsland.unity` and press Play. For the complete boot flow, open `OF_Boot.unity` instead.

Controls: WASD move, Shift sprint, hold right mouse to orbit, E interact, Esc settings. Settings offers persistent language selection and camera sensitivity. English is the default source; untranslated locales intentionally fall back to English.

Read [architecture and integration boundaries](Docs/ASTRA_00_ARCHITECTURE.md), [delivery inventory](Docs/ASTRA_00_DELIVERY.md), and [actual test results](Docs/ASTRA_00_TEST_RESULTS.txt).

Editor tools: `Oceanarium Fishing > Build ASTRA 0.0 Foundation` creates missing foundation assets and refuses to overwrite an existing HomeIsland. `Apply Ground Art Pass` updates generated ground assets. `Run Play Mode Checks` performs the targeted integration checks in Play Mode. `Capture Game View` saves a screenshot under Docs/Screenshots.

The following notes describe the earlier exploratory prototype only; they are retained as history. It is not the active visual design or architecture.

Unity 6000.3.13f1 ile geliştirilen solo keşif prototipi. Üretim sırası ve eksik işler: [Docs/URETIM_PLANI.md](Docs/URETIM_PLANI.md).

## Çalıştırma

1. Projeyi Unity 6000.3.13f1 ile açın.
2. `Oceanarium > Create or Open Prototype` (Ctrl+Alt+O).
3. Play'e basın; `Explore the island` seçin.
4. WASD: hareket, sol Shift: koşu, sağ fare basılı: kamera, E: etkileşim, Esc: menü/paneli kapatma.

İskeleye gidip bir olta alın; kulübe yanındaki sandığı inceleyin. Suya düşerseniz karakter iskele girişine döner. Bu teslimde balık tutma ve kayıt yoktur.

## Doğrulama

Play Mode'da `Oceanarium > Validate Running Prototype` açılış nesneleri, kamera/audio sayısı, olta adedi ve malzemeleri kontrol eder; sonucu `Docs/SmokeTestResult.txt` dosyasına yazar. Bu kontrol etkileşim ve performans testinin yerine geçmez.

Manuel kontrol: menüden başlat; yürü/koş; kamerayı döndür; kulübeye çarp; iskeleden olta al (4 -> 3); ikinci olta alamadığını kontrol et; sandığı aç/kapat; Esc ile duraklat/devam et; suya düşüp güvenli dönüşü gözle. Play'i kapat/aç: olta adedi yeniden 4 olmalı.

Sahne kendi kamera ve ışığını çalışma anında oluşturur. Mevcut SampleScene değiştirilmez. Dağıtım build'i hazırlarken HomeReefPrototype sahnesini build profiline ekleyin; bu teslimde bağımsız build alınmadı.

## Mevcut paketler

| Paket | Sürüm |
| --- | --- |
| Universal RP | 17.3.0 |
| Input System | 1.19.0 |
| Test Framework | 1.6.0 |
| Timeline | 1.8.12 |
| AI Navigation | 2.0.12 |
| Unity UI | 2.0.0 |

Kesin tam liste `Packages/manifest.json` ve `Packages/packages-lock.json` içindedir. Bu çalışmada paket kurulmadı. Cinemachine, Localization, FishNet ve Steam transport entegrasyonu yapılmadı; uyumluluk doğrulaması sonraki aşamaların işidir.

Unity `.meta` dosyalarını kaynaklarla birlikte sürüm kontrolüne alın. `Library`, `Temp`, `Logs` ve `UserSettings` mevcut `.gitignore` tarafından dışlanır.
