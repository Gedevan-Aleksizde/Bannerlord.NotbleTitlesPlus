using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Localization;
using NobleTitlesPlus.DB;
using SandBox.ViewModelCollection.Nameplate;
using System.Diagnostics;

namespace NobleTitlesPlus.Patches
{
    [HarmonyPatch(typeof(Hero), nameof(Hero.Name), MethodType.Getter)]
    internal class HeroNamePatch
    {
        [HarmonyPostfix]
        private static void AppendTitles(Hero __instance, ref TextObject __result)
        {
            if (__instance.IsLord && __instance.IsAlive && !__instance.IsRebel && (__instance.IsKnownToPlayer || !TitleBehavior.options.FogOfWar || __instance
                .IsFactionLeader))
            {
                if (__instance?.Clan?.StringId == null)
                {
                    Util.Log.Print($">> [WARNING] Clan is null: when {__instance.FirstName} (clan={__instance?.Clan?.Name}) called");
                }
                if (__instance?.IsMinorFactionHero == null)
                {
                    Util.Log.Print($">> [WARNING] isMinorFactionHero is null: when {__instance.FirstName} (clan={__instance?.Clan?.Name}) called");
                }
                if (TitleBehavior.nomenclatura.HeroRank.TryGetValue(__instance, out TitleRank rank) && __instance.IsAlive)
                {
                    TextObject title = TitleBehavior.options.TitleSet.GetTitle(__instance, rank).SetTextVariable("NAME", __instance.FirstName)
                        .SetTextVariable("CLAN", __instance.Clan.Name)
                        .SetTextVariable("CLAN_SHORT", __instance.Clan.InformalName);
                    if (TitleBehavior.nomenclatura.FiefLists.TryGetValue(__instance.Clan, out TextObject fiefNames))
                    {
                        title = title.SetTextVariable("FIEFS", fiefNames);
                    }
                    __result = new TextObject(title.ToString());
                }
                else
                {
                    if (!__instance.IsHumanPlayerCharacter) Util.Log.Print($"[WARNIG] title not found when {__instance.FirstName} (clan={__instance?.Clan?.Name}/{__instance?.Clan?.StringId}) called");
                }
            }
        }
    }
    // fix the nameplate on the conversation UI
    [HarmonyPatch(typeof(MissionConversationVM), nameof(MissionConversationVM.Refresh))]
    internal class MissionConversationVMModifyOverNestedTextFormat
    {
        private static TextObject namePre = new();
        [HarmonyPrefix]
        private static void PrevesrveInitialAgentName(MissionConversationVM __instance, ref ConversationManager ____conversationManager)
        {
            if (____conversationManager.OneToOneConversationCharacter?.IsHero ?? false)
            {
                namePre = ____conversationManager.OneToOneConversationHero.Name;
            }
        }
        [HarmonyPostfix]
        private static void EditOverNestedTitleFormatInConverastion(MissionConversationVM __instance, ref ConversationManager ____conversationManager)
        {
            if (____conversationManager.OneToOneConversationCharacter?.IsHero ?? false)
            {
                __instance.CurrentCharacterNameLbl = namePre.ToString();
            }
        }
    }
    /* nameplate hover on the parties in the campaign map*/
    [HarmonyPatch(typeof(PartyNameplateVM), nameof(PartyNameplateVM.RefreshDynamicProperties))]
    internal class OverrideNameTitleOnParty
    {
        [HarmonyPostfix]
        private static void FormatTitle(PartyNameplateVM __instance)
        {
            /*
            // __instance.FullName
            if(!(__instance.Party.IsCaravan || __instance.IsBehind) && __instance.IsVisibleOnMap)
            {
                if (__instance.IsArmy)
                {

                }
                else
                {
                    // __instance.FullName = "aho";
                }
                // Util.Log.Print($"[TEST]party fullname={__instance.FullName}");
            }*/
        }
    }
    // TODO: Can patching GameTexts more clever? 
    [HarmonyPatch(typeof(Kingdom), nameof(Kingdom.EncyclopediaRulerTitle), MethodType.Getter)]
    internal class KingdomRulerTitlePatchNotUniformized
    {
        [HarmonyPostfix]
        private static void StandardizeTitle(Kingdom __instance, ref TextObject __result)
        {
            Util.Log.Print($"Kingdom.EncyclopediaRulerTitle called: {__instance.Name}");
            __result = TitleBehavior.options.TitleSet.GetTitle(false, __instance.Culture.StringId, __instance.Name.ToString(), TitleRank.King, Category.Default);
            // __result = TitleBehavior.nomenclatura.titleDb.GetKingTitle(__instance.Culture, Category.Default).MaleFormat;
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
