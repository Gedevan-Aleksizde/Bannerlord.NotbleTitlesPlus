using HarmonyLib;
using NobleTitlesPlus.DB;
using NobleTitlesPlus.json;
using NobleTitlesPlus.MCMSettings;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NobleTitlesPlus.Patches
{
    [HarmonyPatch(typeof(Hero), nameof(Hero.Name), MethodType.Getter)]
    [HarmonyPatchCategory("NameChangerCore")]
    internal class HeroNameChanger
    {
        public static TextObject ReplaceName(Hero hero, TextObject name)
        {
            if (hero?.Clan is null)
            {
                return name;
            }
            if (hero.IsLord && !hero.IsRebel)
            {
                if (hero.Clan?.StringId == null)
                {
                    Util.Log.Print($"Clan is null: when {name} (clan={hero.Clan?.Name}) called", LogCategory.Warning);
                }
                if (hero?.IsMinorFactionHero == null)
                {
                    Util.Log.Print($"isMinorFactionHero is null: when {name} (clan={hero.Clan?.Name}) called", LogCategory.Warning);
                }
                TextObject title = new("{NAME}");
                TextObject suffNumText = new("");
                if (!hero.IsAlive)
                {
                    title = MCMRuntimeSettings.Instance.Options.TitleSet.GetMostMatchedTitle(hero, TitleRank.None);
                }
                else if ((hero.IsKnownToPlayer || !MCMRuntimeSettings.Instance.Options.FogOfWar || hero.IsFactionLeader))
                {
                    bool hasRank = MCMRuntimeSettings.Instance.Nomenclatura.HeroProfiles.TryGetValue(hero, out HeroProfile hp);
                    title = MCMRuntimeSettings.Instance.Options.TitleSet.GetMostMatchedTitle(hero, hasRank ? hp.TitleRank : TitleRank.None);
                    if (hasRank) { suffNumText = hp.GenSuffixText; }
                }
                else
                {
                    title = MCMRuntimeSettings.Instance.Options.TitleSet.GetMostMatchedTitle(hero, TitleRank.None);
                }
                title = title.SetTextVariable("NAME", name)
                            .SetTextVariable("CLAN", hero.Clan?.Name)
                            .SetTextVariable("CLAN_SHORT", hero.Clan?.InformalName)
                            .SetTextVariable("SUFF_NUM", suffNumText);
                if (MCMRuntimeSettings.Instance.Nomenclatura.ClanAttrs.TryGetValue(hero.Clan, out (TextObject strFief, TextObject shokuhoProv, ClanNamePair clanNamesPair) fiefNames))
                {
                    title = title.SetTextVariable("FIEFS", fiefNames.strFief).SetTextVariable("PROVINCE_SHO", fiefNames.shokuhoProv);
                }
                return new TextObject(title.ToString());
            }
            return name;
        }
        [HarmonyPostfix]
        private static void ReplaceNameFormat(Hero __instance, ref TextObject ____name, ref TextObject ____firstName, ref TextObject __result)
        {
            if (__instance.IsWanderer || __instance.IsNotable) { return; }
            __result = ReplaceName(__instance, ____firstName);
        }
    }
    [HarmonyPatch(typeof(Hero), nameof(Hero.FirstName), MethodType.Getter)]
    [HarmonyPatchCategory("NameChangerCore")]
    internal class HeroFirstNameChanger
    {
        [HarmonyPostfix]
        private static void ReplaceFirstNameFormat(Hero __instance, ref TextObject ____firstName, TextObject __result)
        {
            if (__instance.IsWanderer || __instance.IsNotable) { return; }
            __result = HeroNameChanger.ReplaceName(__instance, ____firstName);
        }
    }
    // fix the nameplate on the conversation UI
    [HarmonyPatch(typeof(MissionConversationVM), nameof(MissionConversationVM.Refresh))]
    [HarmonyPatchCategory("Conversation")]
    internal class ModifyTitleOnMissionConversationVM
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
    /// <summary>
    /// This just overwrites the same as the original code. I'm afraid Bannerlord's terrible messy script.
    /// </summary>
    [HarmonyPatch(typeof(CharacterObject), nameof(CharacterObject.Name), MethodType.Getter)]
    [HarmonyPatchCategory("Why")]
    internal class CharacterObjectNameChanger
    {
        [HarmonyPostfix]
        private static void TWwf(CharacterObject __instance, ref TextObject __result)
        {
            if (__instance.IsHero)
            {
                __result = __instance.HeroObject.Name;

            }
        }

    }
    /// <summary>
    /// This just overrides the original method with the same thing. I'm afraid Bannerlord's terrible messy script.
    /// </summary>
    [HarmonyPatch(typeof(Hero), nameof(Hero.EncyclopediaLinkWithName), MethodType.Getter)]
    [HarmonyPatchCategory("Why")]
    internal class WhyNeededModifyEncyclopediaLink
    {
        [HarmonyPrefix]
        private static bool Override(Hero __instance, ref TextObject __result)
        {
            __result = HyperlinkTexts.GetHeroHyperlinkText(__instance.EncyclopediaLink, __instance.Name);
            return false;
        }
    }
    /// <summary>
    /// nameplate hover on the parties in the campaign map
    /// TODO: This patch does nothing, but some names not changed without this void patching. WHY?????
    /// </summary>
    [HarmonyPatch(typeof(PartyNameplateVM), nameof(PartyNameplateVM.RefreshDynamicProperties))]
    [HarmonyPatchCategory("PartyPopUp")]
    internal class ModifyTitleOnPartyNamePlateVM
    {
        [HarmonyPostfix]
        private static void ChangeTitle(PartyNameplateVM __instance)
        {
        }
    }
    /// <summary>
    /// TODO: is it possible to do patching GameTexts more clever?  
    /// </summary>
    [HarmonyPatch(typeof(Kingdom), nameof(Kingdom.EncyclopediaRulerTitle), MethodType.Getter)]
    [HarmonyPatchCategory("Encyclopedia")]
    internal class ModifyTitleOnKingdomEncyclopediaRuler
    {
        [HarmonyPostfix]
        private static void StandardizeTitle(Kingdom __instance, ref TextObject __result)
        {
            Util.Log.Print($"Kingdom.EncyclopediaRulerTitle called: {__instance.Culture.StringId}, {__instance.Name}");
            __result = MCMRuntimeSettings.Instance.Options.TitleSet.GetMostMatchedTitle(false, __instance.Culture.StringId, __instance.Name.ToString(), TitleRank.King, Category.Default);
        }
    }
    /* used by SettlementMenuOverlayVM.CharacterList and so on*/
    /// <summary>
    /// Shows titles on the nameplates at the top of the settlement panel
    /// </summary>
    [HarmonyPatch(typeof(GameMenuPartyItemVM), nameof(GameMenuPartyItemVM.RefreshProperties))]
    [HarmonyPatchCategory("SettlementPanel")]
    internal class ModifyTitleOnPartyItemVMName
    {
        [HarmonyPostfix]
        private static void FormatTitle(GameMenuPartyItemVM __instance)
        {
            if (__instance?.Party != null)
            {
            }
            else if (__instance?.Character != null)
            {
                if (__instance.Character.IsHero)
                {
                    string name = __instance.Character.HeroObject.Name.ToString();
                    __instance.NameText = name;
                }
            }
        }
    }
    [HarmonyPatch(typeof(Kingdom), nameof(Kingdom.Name), MethodType.Getter)]
    [HarmonyPatchCategory("KingdomAbbreviated")]
    internal class KingdomName2Short
    {
        [HarmonyPrefix]
        private static bool Override(Kingdom __instance, ref TextObject __result)
        {
            __result = __instance.InformalName;
            return false;
        }
    }
    [HarmonyPatch(typeof(Kingdom), nameof(Kingdom.EncyclopediaTitle), MethodType.Getter)]
    [HarmonyPatchCategory("KingdomAbbreviated")]
    internal class KingdomTitle2Short
    {
        [HarmonyPrefix]
        private static bool Override(Kingdom __instance, ref TextObject __result)
        {
            __result = __instance.InformalName;
            return false;
        }
    }
    [HarmonyPatch(typeof(Kingdom), nameof(Kingdom.InformalName), MethodType.Getter)]
    [HarmonyPatchCategory("KingdomFull")]
    internal class KingdomInformalName2Full
    {
        [HarmonyPrefix]
        private static bool Override(Kingdom __instance, ref TextObject __result)
        {
            __result = __instance.EncyclopediaTitle;
            return false;
        }
    }
    [HarmonyPatch(typeof(Kingdom), nameof(Kingdom.Name), MethodType.Getter)]
    [HarmonyPatchCategory("KingdomFull")]
    internal class KingdomName2Full
    {
        [HarmonyPrefix]
        private static bool Override(Kingdom __instance, ref TextObject __result)
        {
            __result = __instance.EncyclopediaTitle;
            return false;
        }
    }

}
