using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NobleTitlesPlus.Patches
{
    [HarmonyPatch(typeof(Hero), nameof(Hero.Name), MethodType.Getter)]
    internal class HeroNamePatch
    {
        [HarmonyPostfix]
        private static void AppendTitle(Hero __instance, ref TextObject __result)
        {
            /*if(TitleBehavior.nomenclatura.NameTitle.TryGetValue(__instance, out TextObject titleFormat) && __instance.IsAlive)
            {
                __result = titleFormat.SetTextVariable("NAME", __instance.FirstName);
            }*/
            if (TitleBehavior.nomenclatura.HeroRank.TryGetValue(__instance, out TitleRank rank) && __instance.IsAlive) 
            {
                __result = TitleBehavior.nomenclatura.GetTitle(__instance.IsFemale, __instance.Culture.StringId, rank).SetTextVariable("NAME", __instance.FirstName);
            }
        }
        // TODO: 軍隊の名前はどうなってるの?
        // TODO: 会話ダイアログの表示名で本来の称号が付け足される
        // TODO: 使用可能マクロを増やす
    }
}
