using UnityEngine.Localization.Tables;
namespace OceanariumFishing.Editor
{
    public static class CreatorTranslations
    {
        public static void Turkish(StringTable table)
        {
            void Slot(string slot,string translations){var ids=CosmeticCatalog.Options[slot];var values=translations.Split('|');if(ids.Length!=values.Length)throw new System.InvalidOperationException("Creator translation count: "+slot);for(int i=0;i<ids.Length;i++)table.AddEntry("creator."+ids[i],values[i]);}
            Slot("presentation","Maskülen|Feminen");
            Slot("hair","Klasik kısa|Dağınık kısa|Yana taralı|Kısa kıvırcık|Orta katlı|Uzun düz|Uzun dalgalı|Yüksek at kuyruğu|Alçak at kuyruğu|Çift at kuyruğu|Kısa küt|Dalgalı küt|Örgülü|Çift topuz|Omuz hizası katlı");
            Slot("hairColor","Siyah|Koyu kahve|Kahverengi|Açık kahve|Sarı|Platin sarısı|Bakır|Kestane|Beyaz|Gümüş|Pembe|Pastel pembe|Mavi|Pastel mavi|Mor|Nane yeşili");
            Slot("skin","Porselen|Fildişi|Şeftali|Sıcak bej|Altın|Bronz ten|Karamel|Koyu bronz|Toprak|Kakao|Koyu kahverengi|Abanoz");
            Slot("eyes","Yuvarlak|Yumuşak oval|Mutlu|Mahmur|İri|Nazik");Slot("eyeColor","Kahverengi|Koyu kahve|Ela|Yeşil|Açık yeşil|Mavi|Açık mavi|Gri|Kehribar|Menekşe");
            Slot("brows","Düz|Hafif kavisli|Kalın|İnce|Kalkık|Yuvarlak kavisli");Slot("mouth","Küçük gülümseme|Nötr|Neşeli|Hafif gülümseme|Aralık gülümseme|Rahat");Slot("nose","Düğme|Yumuşak|Sivri");Slot("detail","Yok|Çiller|Allık|Ben|Hafif yanak ayrıntısı");
            Slot("beard","Yok|Kirli sakal|Kısa sakal|Gür sakal|Bıyık|Keçi sakalı");Slot("top","Düz tişört|Çizgili tişört|Rahat kazak|Balıkçı gömleği|Kapüşonlu üst|Günlük ceket");Slot("bottom","Şort|Kot şort|Pantolon|Kargo pantolon|Kısa etek|Uzun etek");Slot("shoes","Spor ayakkabı|Kısa bot|Balıkçı çizmesi|Sandalet|Rahat ayakkabı");Slot("hat","Yok|Kep|Hasır şapka|Balıkçı şapkası|Bere|Kova şapka");Slot("glasses","Yok|Yuvarlak gözlük|Kare gözlük|Güneş gözlüğü");Slot("accessory","Yok|Sırt çantası|Çapraz çanta|Kolye|Bileklik");Slot("outfitColor","Deniz köpüğü|Mercan|Krem|Lacivert|Lavanta|Hardal");
        }
    }
}
