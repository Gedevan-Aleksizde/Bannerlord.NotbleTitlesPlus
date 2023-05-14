using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;

namespace NobleTitlesPlus.Patches
{
    [HarmonyPatch(typeof(Hero), nameof(Hero.Name), MethodType.Getter)]
    internal class HeroNamePatch
    {
        [HarmonyPostfix]
        private static void AppendTitles(Hero __instance, ref TextObject __result)
        {
            if (TitleBehavior.nomenclatura.HeroRank.TryGetValue(__instance, out TitleRank rank) && __instance.IsAlive) 
            {
                TextObject title = TitleBehavior.nomenclatura.GetTitle(
                    __instance.IsFemale,
                    __instance.IsMinorFactionHero ? __instance.Clan.StringId : __instance.Clan.Kingdom.Culture.StringId,
                    rank,
                    __instance.IsMinorFactionHero ? Category.MinorFaction : Category.Default
                    ).SetTextVariable("NAME", __instance.FirstName).SetTextVariable("CLAN", __instance.Clan.Name);
                if (TitleBehavior.nomenclatura.FiefLists.TryGetValue(__instance.Clan, out TextObject fiefNames))
                {
                    title = title.SetTextVariable("FIEFS", fiefNames);
                }
                __result = new TextObject(title.ToString());
            }
        }
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
            __result = TitleBehavior.nomenclatura.titleDb.GetKingTitle(__instance.Culture, Category.Default).MaleFormat;
        }
    }
    /*[HarmonyPatch(typeof(EncyclopediaListItemVM), nameof(EncyclopediaListItemVM.Name), MethodType.Getter)]
    internal class EncyclopediaName
    {
        [HarmonyPostfix]
        private static void Rename(string __instance, ref string __result)
        {
            Util.Log.Print($"Ency-VM.Name={__result}");
        }
    }*/
}
