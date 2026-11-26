using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Encyclopedia.Pages;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Localization;

namespace NobleTitlesPlus.Patches
{
    [HarmonyPatch]
    [HarmonyPatchCategory("DisplayLocations")]
    internal static class DisplayLocationScopes
    {
        private static readonly Dictionary<MethodBase, NameDisplayLocation> locations = new();

        internal static IEnumerable<MethodBase> TargetMethods()
        {
            locations.Clear();
            Add("TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages.EncyclopediaHeroPageVM",
                NameDisplayLocation.EncyclopediaHero, "Refresh", "RefreshValues", "GetName", "GetNavigationBarURL");
            // In 1.5.7 the kingdom page constructs a HeroVM for its ruler here.
            // Format that UI name under the encyclopedia setting, never the saved
            // Kingdom.EncyclopediaRulerTitle property.
            Add("TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages.EncyclopediaFactionPageVM",
                NameDisplayLocation.EncyclopediaHero, "Refresh", "RefreshValues");
            Add("TaleWorlds.CampaignSystem.ViewModelCollection.Conversation.MissionConversationVM",
                NameDisplayLocation.Conversation, "Refresh");
            Add("TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay.GameMenuPartyItemVM",
                NameDisplayLocation.Settlement, "RefreshProperties");
            Add("SandBox.ViewModelCollection.Nameplate.PartyNameplateVM",
                NameDisplayLocation.Map, "RefreshDynamicProperties");
            Add("TaleWorlds.CampaignSystem.ViewModelCollection.TooltipRefresherCollection",
                NameDisplayLocation.Map, "RefreshMobilePartyTooltip", "RefreshArmyTooltip");
            return locations.Keys;
        }

        private static void Add(string typeName, NameDisplayLocation location, params string[] names)
        {
            Type type = AccessTools.TypeByName(typeName) ?? throw new MissingMemberException(typeName);
            foreach (string name in names)
            {
                MethodInfo method = AccessTools.DeclaredMethod(type, name) ?? throw new MissingMethodException(typeName, name);
                locations[method] = location;
            }
        }

        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void Begin(MethodBase __originalMethod, out IDisposable __state)
            => __state = NameDisplay.Enter(locations[__originalMethod]);

        [HarmonyFinalizer]
        private static Exception? End(Exception? __exception, IDisposable? __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(EncyclopediaPage), nameof(EncyclopediaPage.GetListItems))]
    [HarmonyPatchCategory("DisplayLocations")]
    internal static class EncyclopediaHeroListNames
    {
        [HarmonyPostfix]
        private static void Format(EncyclopediaPage __instance, ref IEnumerable<EncyclopediaListItem> __result)
        {
            if (__instance is DefaultEncyclopediaHeroPage) __result = FormatItems(__result);
        }

        internal static IEnumerable<EncyclopediaListItem> FormatItems(IEnumerable<EncyclopediaListItem> source)
        {
            // The engine builds this list lazily. Scope each MoveNext, not the iterator factory.
            using var iterator = source.GetEnumerator();
            while (true)
            {
                EncyclopediaListItem item;
                using (NameDisplay.Enter(NameDisplayLocation.Original))
                {
                    if (!iterator.MoveNext()) yield break;
                    item = iterator.Current;
                }
                using (NameDisplay.Enter(NameDisplayLocation.EncyclopediaList))
                {
                    if (NameDisplay.Enabled && item.Object is Hero hero && HeroNameChanger.CanReplaceName(hero))
                    {
                        item = new EncyclopediaListItem(item.Object, hero.Name.ToString(), item.Description,
                            item.Id, item.TypeName, item.PlayerCanSeeValues, item.OnShowTooltip);
                    }
                }
                // Do not let the caller's processing inherit our display scope.
                yield return item;
            }
        }
    }

    // Army.Name is a saved property: SaveSystem must see the same object during
    // reference collection and member serialization. Render copies only inside UI scopes.
    [HarmonyPatch(typeof(Army), nameof(Army.Name), MethodType.Getter)]
    [HarmonyPatchCategory("DisplayLocations")]
    internal static class ArmyDisplayName
    {
        [HarmonyPostfix]
        private static void Format(Army __instance, ref TextObject __result)
        {
            if (NameDisplay.Current != NameDisplayLocation.Map) return;
            var owner = __instance.ArmyOwner ?? __instance.LeaderParty?.Owner;
            if (owner != null && __result != null)
                __result = __result.CopyTextObject().SetTextVariable("LEADER_NAME", owner.Name);
        }
    }

    [HarmonyPatch(typeof(LordPartyComponent), nameof(LordPartyComponent.Name), MethodType.Getter)]
    [HarmonyPatchCategory("DisplayLocations")]
    internal static class LordPartyDisplayName
    {
        [HarmonyPostfix]
        private static void Format(LordPartyComponent __instance, ref TextObject __result)
        {
            if (__instance.Owner != null && __result != null)
            {
                __result = __result.CopyTextObject();
                __result.SetCharacterProperties("TROOP", __instance.Owner.CharacterObject);
            }
        }
    }
}
