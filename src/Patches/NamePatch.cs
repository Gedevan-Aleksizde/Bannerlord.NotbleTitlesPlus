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
            if(TitleBehavior.nomenclatura.NameTitle.TryGetValue(__instance, out string title))
            {
                __result = new TextObject(title).SetTextVariable("NAME", __instance.FirstName);
            }
        }
        // TODO: 軍隊の名前はどうなってるの?
        // TODO: 会話ダイアログの表示名で本来の称号が付け足される
        // TODO: パフォーマンス改善
        // TODO: 使用可能マクロを増やす
    }
}
