using HarmonyLib;
using NobleTitlesPlus.DB;
using NobleTitlesPlus.json;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Localization;

namespace NobleTitlesPlus.Patches
{
    [HarmonyPatch(typeof(Hero), nameof(Hero.Name), MethodType.Getter)]
    [HarmonyPatchCategory("NameChangerCore")]
    internal class HeroNameChanger
    {
        public static TextObject ReplaceName(Hero hero, TextObject name)
        {
            if (hero.IsLord && !hero.IsRebel)
            {
                if (hero?.Clan?.StringId == null)
                {
                    Util.Log.Print($">> [WARNING] Clan is null: when {name} (clan={hero?.Clan?.Name}) called");
                }
                if (hero?.IsMinorFactionHero == null)
                {
                    Util.Log.Print($">> [WARNING] isMinorFactionHero is null: when {name} (clan={hero?.Clan?.Name}) called");
                }
                TextObject title = new("{NAME}");
                if (!hero.IsAlive)
                {
                    title = TitleBehavior.options.TitleSet.GetMatchedTitle(hero, TitleRank.None);
                }
                else if ((hero.IsKnownToPlayer || !TitleBehavior.options.FogOfWar || hero.IsFactionLeader))
                {
                    bool hasRank = TitleBehavior.nomenclatura.HeroRank.TryGetValue(hero, out TitleRank rank);

                    title = TitleBehavior.options.TitleSet.GetMatchedTitle(hero, hasRank ? rank : TitleRank.None);
                }
                else
                {
                    title = TitleBehavior.options.TitleSet.GetMatchedTitle(hero, TitleRank.None);
                }
                title = title.SetTextVariable("NAME", name)
                            .SetTextVariable("CLAN", hero?.Clan?.Name)
                            .SetTextVariable("CLAN_SHORT", hero?.Clan?.InformalName);
                if (TitleBehavior.nomenclatura.ClanAttrs.TryGetValue(hero?.Clan, out (TextObject strFief, TextObject shokuhoProv, ClanNamePair clanNamesPair) fiefNames))
                {
                    title = title.SetTextVariable("FIEFS", fiefNames.strFief).SetTextVariable("PROVINCE_SHO", fiefNames.shokuhoProv);
                }
                return new TextObject(title.ToString());
            }
            return name;
        }
        [HarmonyPostfix]
        private static void ReplaceNameFormat(Hero __instance, ref TextObject ____name, ref TextObject __result)
        {

            __result = ReplaceName(__instance, ____name);
        }
    }
    [HarmonyPatch(typeof(CharacterObject), nameof(CharacterObject.Name), MethodType.Getter)]
    [HarmonyPatchCategory("NameChangerCore")]
    internal class CONameChanger
    {
        [HarmonyPostfix]
        private static void TWaho(CharacterObject __instance, ref TextObject __result)
        {
            if (__instance.IsHero)
            {
                __result = __instance.HeroObject.Name; // It's just overwritten as the original code. I'm afraid Bannerlord script is terrible messy.

            }
        }

    }
    [HarmonyPatch(typeof(Hero), nameof(Hero.FirstName), MethodType.Getter)]
    [HarmonyPatchCategory("NameChangerCore")]
    internal class HeroFirstNameChanger
    {
        [HarmonyPostfix]
        private static void ReplaceFirstNameFormat(Hero __instance, ref TextObject ____firstName, TextObject __result)
        {
            __result = HeroNameChanger.ReplaceName(__instance, ____firstName);
        }
    }
    // fix the nameplate on the conversation UI
    [HarmonyPatch(typeof(MissionConversationVM), nameof(MissionConversationVM.Refresh))]
    [HarmonyPatchCategory("Converstation")]
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
    /// nameplate hover on the parties in the campaign map
    /// FIXME: This patch does nothing, but the titles disappear from alternative party nameplate on the campaign map if erased this method. WHY??? UPDATE: now not working. WHY?????? 
    /// </summary>
    [HarmonyPatch(typeof(PartyNameplateVM), nameof(PartyNameplateVM.RefreshDynamicProperties))]
    [HarmonyPatchCategory("PartyPopUp")]
    internal class ModifyTitleOnPartyNamePlateVM
    {
        [HarmonyPostfix]
        private static void ChangeTitle(PartyNameplateVM __instance)
        {
            /*__instance.FullName = "TEST";
            if (!(__instance.Party.IsCaravan || __instance.IsBehind) && __instance.IsVisibleOnMap)
            {
                if (__instance.IsArmy)
                {
                }
                else
                {
                    __instance.FullName = "TEST";
                }
                // Util.Log.Print($"[TEST]party fullname={__instance.FullName}");
            }*/
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
            Util.Log.Print($"Kingdom.EncyclopediaRulerTitle called: {__instance.Name}");
            __result = TitleBehavior.options.TitleSet.GetMatchedTitle(false, __instance.Culture.StringId, __instance.Name.ToString(), TitleRank.King, Category.Default);
            // __result = TitleBehavior.nomenclatura.GetKingTitle(__instance.Culture, Category.Default).MaleFormat;
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
