using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
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
                __result = TitleBehavior.nomenclatura.GetTitle(__instance.IsFemale, __instance.Culture.StringId, rank).SetTextVariable("NAME", __instance.FirstName).SetTextVariable("CLAN", __instance.Clan.Name);
            }
        }
        // TODO: Army Name
        // TODO: More macros
    }
    [HarmonyPatch(typeof(MissionConversationVM), nameof(MissionConversationVM.Refresh))]
    internal class MissionConversationVMModifyOverNetstedTextFormat
    {
        private static TextObject namePre = new();
        [HarmonyPrefix]
        private static void PrevesrveInitialAgentName(MissionConversationVM __instance, ref ConversationManager ____conversationManager)
        {
            namePre = ____conversationManager.SpeakerAgent.Character.Name;
        }
        [HarmonyPostfix]
        private static void EditOverNestedTitleFormatInConverastion(MissionConversationVM __instance)
        {
            __instance.CurrentCharacterNameLbl = namePre.ToString();
        }
    }
    // TODO: Is Patching GameTexts more clever? 
    [HarmonyPatch(typeof(Kingdom), nameof(Kingdom.EncyclopediaRulerTitle), MethodType.Getter)]
    internal class KingdomRulerTitlePatchNotUniformized
    {
        [HarmonyPostfix]
        private static void StandardizeTitle(Kingdom __instance, ref TextObject __result)
        {
            __result = TitleBehavior.nomenclatura.titleDb.GetKingTitle(__instance.Culture).MaleFormat;
        }
    }
    
}
