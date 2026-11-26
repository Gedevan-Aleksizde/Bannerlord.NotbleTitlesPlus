using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace NobleTitlesPlus
{
    internal static class SaveNameRecovery
    {
        internal static void RestoreMissingArmyNames()
            => RestoreMissingArmyNames(MobileParty.All.Select(party => party.Army));

        // Earlier display patches returned a new Army.Name object on every access.
        // Saves written with those patches may have lost the reference to the name.
        // Rebuild only missing names, before the map creates its tracked-army markers.
        internal static int RestoreMissingArmyNames(IEnumerable<Army> armies)
        {
            int restored = 0;
            var seen = new HashSet<Army>();
            using (NameDisplay.Enter(NameDisplayLocation.Original))
            {
                foreach (var army in armies)
                {
                    if (army is null || !seen.Add(army) || army.Name != null) continue;
                    if (army.ArmyOwner is null && army.LeaderParty is null) continue;
                    army.UpdateName();
                    restored++;
                }
            }
            if (restored > 0)
                Util.Log.Print($"Restored {restored} missing army name(s) after loading a save.", LogCategory.Warning);
            return restored;
        }
    }
}
