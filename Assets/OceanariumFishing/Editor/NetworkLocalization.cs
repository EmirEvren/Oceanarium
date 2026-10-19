using UnityEditor;
using UnityEngine.Localization.Tables;
namespace OceanariumFishing.Editor
{
    public static class NetworkLocalization
    {
        public static void Apply()
        {
            var rows=new (string key,string en,string tr)[]{
                ("solo","Play solo","Tek başına oyna"),("host","Host game","Oyun kur"),("join","Join game","Oyuna katıl"),("invite","Invite friends","Arkadaşlarını davet et"),("quit","Quit","Çıkış"),
                ("crew","Island crew","Ada ekibi"),("host_badge","Host","Ev sahibi"),("leave","Leave session","Oturumdan ayrıl"),("worlds","Your islands","Adaların"),("new_world","Create a new world","Yeni dünya oluştur"),("world_name","World name","Dünya adı"),("default_world","My island","Adam"),
                ("import","Copy my existing progress into this world","Mevcut ilerlememi bu dünyaya kopyala"),("privacy","Lobby privacy","Lobi görünürlüğü"),("friends_only","Friends only","Yalnızca arkadaşlar"),("friends_note","Friends can join through Steam invitations.","Arkadaşların Steam davetiyle katılabilir."),("public_note","This world appears in the public lobby list.","Bu dünya herkese açık lobi listesinde görünür."),
                ("enter","Enter the island","Adaya gir"),("refresh","Refresh lobbies","Lobileri yenile"),("invite_note","You can also accept an invitation in the Steam overlay.","Steam arayüzünden gelen davetleri de kabul edebilirsin."),
                ("foundation_note","Co-op foundation: guests can explore. World interactions are currently controlled by the host.","Eşli oyun temeli: misafirler keşfedebilir. Dünya etkileşimlerini şu an ev sahibi yönetir."),
                ("status.idle","Welcome to your island.","Adana hoş geldin."),("status.solo","Playing solo","Tek oyunculu"),("status.hosting","Creating your Steam lobby…","Steam lobisi oluşturuluyor…"),("status.joining","Joining the island…","Adaya katılınıyor…"),("status.loading","Preparing the island…","Ada hazırlanıyor…"),("status.connected","Connected","Bağlandı"),("status.left","You left the session.","Oturumdan ayrıldın."),
                ("status.full","This island already has four players.","Bu adada zaten dört oyuncu var."),("status.duplicate","This account already has an active connection.","Bu hesabın zaten etkin bir bağlantısı var."),("status.authentication","Steam identity verification failed. Please reconnect.","Steam kimliği doğrulanamadı. Lütfen yeniden bağlan."),("status.timeout","The connection timed out. Please try again.","Bağlantı zaman aşımına uğradı. Lütfen tekrar dene."),
                ("status.steam_config","Steam App ID is not configured correctly.","Steam App ID doğru yapılandırılmamış."),("status.steam_unavailable","Steam is unavailable. Start Steam to play online; solo still works.","Steam kullanılamıyor. Çevrimiçi oyun için Steam'i aç; tek oyunculu mod çalışır."),("status.steam_access","Use a Steam account with access to Oceanarium.","Oceanarium erişimi olan bir Steam hesabı kullan."),
                ("status.lobby_create","The Steam lobby could not be created.","Steam lobisi oluşturulamadı."),("status.lobby_join","The Steam lobby could not be joined.","Steam lobisine katılınamadı."),("status.lobby_list","The lobby list could not be loaded.","Lobi listesi yüklenemedi."),("status.lobby_invalid","This invitation is no longer valid.","Bu davet artık geçerli değil."),
                ("status.already_connected","Leave the current session before joining another.","Başka bir oturuma katılmadan önce mevcut oturumdan ayrıl."),("status.not_hosting","Join or host a Steam lobby first.","Önce bir Steam lobisine katıl veya lobi oluştur."),("status.host_left","The host left. Your island is now offline.","Ev sahibi ayrıldı. Ada artık çevrimdışı."),("status.connection_lost","Connection lost. You can rejoin to restore your saved character.","Bağlantı kesildi. Kayıtlı karakterini geri almak için yeniden katılabilirsin."),
                ("status.connection_failed","The network connection could not be started.","Ağ bağlantısı başlatılamadı."),("status.prefab_missing","Network assets are missing. Please verify the game files.","Ağ varlıkları eksik. Lütfen oyun dosyalarını doğrula."),("status.world_owner","This world belongs to another Steam account.","Bu dünya başka bir Steam hesabına ait."),
                ("status.save_invalid","A world save could not be read. Your original files have been preserved.","Bir dünya kaydı okunamadı. Özgün dosyaların korundu."),("status.save_failed","Saving failed. Check available disk space and retry.","Kayıt başarısız. Boş disk alanını kontrol edip tekrar dene."),("status.host_only","Only the host can change the shared world.","Ortak dünyayı yalnızca ev sahibi değiştirebilir."),("status.island_locked","This island is not unlocked in this world.","Bu adanın kilidi bu dünyada açılmamış.")
            };
            foreach(var code in LanguageService.Codes)
            {
                var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/OceanariumFishing/Localization/Tables/OF_UI_"+code+".asset");if(!table)continue;
                foreach(var row in rows)if(code=="en"||code=="tr"||string.IsNullOrEmpty(table.GetEntry("net."+row.key)?.LocalizedValue))table.AddEntry("net."+row.key,code=="tr"?row.tr:row.en);EditorUtility.SetDirty(table);EditorUtility.SetDirty(table.SharedData);
            }
        }
    }
}
