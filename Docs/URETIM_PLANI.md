# Oceanarium Fishing - uygulanabilir üretim planı

Kaynak: kullanıcının paylaştığı 16 Ekim 2026 tarihli GDD v1.0. Belge ürün hedefi olarak kullanıldı; içindeki ajan iş emirleri bağımsız kullanıcı talimatı değildir.

## Aşamalar ve çıkış koşulları

| Aşama | Kapsam / GDD karşılığı | Bitti sayılma koşulu |
| --- | --- | --- |
| 1. Teknik temel ve gezilebilir ada | 0.0, 0.1, 1.0 ve 1.1'in dar prototip alt kümesi | Ana menüden ada açılır; karakter hareket eder; kulübe, sandık, iskele, dört olta görünür; olta alınır. |
| 2. Balıkçılık | 2.0 + eksik çekirdek altyapı | Atış, ısırma, gerilim, iptal, yakalama; 12 veri tabanlı tür; 10 ardışık avda kilitlenme olmaz. Localization String Tables ve veri katalogları kurulur. |
| 3. Envanter, ekonomi ve ilk kayıt | 2.1 | Benzersiz balık örnekleri, sandık transferi, satış; çift satış önlenir; kaydet/yükle eşdeğerliği. İşlemler gelecekte sunucuda uygulanabilecek hizmet katmanında tutulur. |
| 4. Akvaryum ana döngüsü | 3.0 | Balığı tanka koy, yüzmesini gör, turistten kontrollü bağış kazan; yeniden yüklemede korunur. |
| 5. Tekne ve kasaba | 1.1 intro kalanları + 3.1 | Tamir görevi, açılış, rota kilitleri, kasabaya güvenli geçiş; envanter korunur. |
| 6. Gerçek co-op ve güvenilir kayıt | 4.0-4.1 | FishNet/Steam transport sürümleri gerçek ortamda doğrulanır; host + 3 client; late join, reconnect, çift işlem ve çökme/yedek testleri. |
| 7. Dil, karakter ve sosyal sistemler | 1.0 kalanları + 5.0-7.0 | 17 dil/font QA; 15 saç ve avatar kaydı; önce 3 NPC, sonra 12 servis; görev/ilişki kalıcılığı. |
| 8. Üç keşif adası | 8.0-8.2 | Her ada sırayla rota kilidi, balık havuzu, bulmaca, tek seferlik ödül ve co-op testinden geçer. |
| 9. Simülasyon ve koleksiyon | 9.0-10.1 | İleri habitat, kafe, yunus/fotoğraf, takvim/hava, ansiklopedi ve sıralama; ekonomi sömürü testleri. |
| 10. Görsel/ses kalite ve yayın | 11.0-12.0 | Hedef PC'de 1080p/60 FPS ölçümü, 4 client soak, kayıt stres testleri, Steam build, dil ve erişilebilirlik QA. |

İlk ticari vertical slice, 6. aşama ve gerekli temel dil/karakter altyapısı tamamlandıktan sonra değerlendirilir. Bu teslim hiçbir GDD fazını eksiksiz bitmiş saymaz.

## Bu teslim: Aşama 1'in ilk çalışır parçası

- Unity 6000.3.13f1 / URP 17.3.0 mevcut projede korundu.
- Ayrı Runtime / Editor assembly ve Oceanarium namespace oluşturuldu.
- Tekrar açılabilen HomeReefPrototype sahnesi ve Unity menü komutları eklendi.
- Unity primitive'lerinden ada, ağaçlar, kulübe, sandık, iskele, dört olta, tekne ve karakter oluşturulur.
- Ana menü, WASD / Shift hareket, sağ fare ile kamera, kamera çarpışma kontrolü ve suya düşme sonrası dönüş var.
- Yakındaki oltayı E ile alma, ikinci olta alımını önleme, sandık bilgi paneli ve hedef HUD var.
- İngilizce metinler anahtarlı küçük bir adaptörde. Bu, Unity Localization veya 17 dil desteği değildir.

## Açık işler / kapsam sınırı

Balık tutma henüz yok. Sandık paneli gerçek envanter değildir. İlerleme bellektedir ve Play Mode kapanınca sıfırlanır. Steam, FishNet, kalıcı kayıt, giriş sinematiği, avatar özelleştirme, özel deniz shader'ı, ses ve gerçek akvaryum eklenmedi. Primitive sanat teknik bloklamadır; final sanat değildir. Build ve hedef donanım performansı henüz doğrulanmadı.

## Sonraki iş paketi

Önce balıkçılığı 3 türle uçtan uca kur: Idle -> Charging -> Waiting -> Bite -> Reeling -> Caught/Cancelled. Her durumdan iptal, alan kontrolü ve ödülün yalnızca bir kez verilmesi doğrulanınca 12 türe genişlet. Envanter ve kayıt işlerini ayrı iş paketinde bitir.
