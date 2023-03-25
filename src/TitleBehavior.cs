using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
// using TaleWorlds.CampaignSystem.Conversation.Tags;
// using TaleWorlds.Library;
using TaleWorlds.Localization;
// using TaleWorlds.ObjectSystem;

namespace NobleTitlesPlus
{
    internal sealed class TitleBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnGameLoaded));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
        }

        public override void SyncData(IDataStore dataStore)
        {
            string dtKey = $"{SubModule.Name}DeadTitles";
            string svKey = $"{SubModule.Name}SaveVersion";

            // Synchronize current savegame version:
            dataStore.SyncData(svKey, ref saveVersion);

            if (dataStore.IsSaving)
            {
                // Serializing dead heroes' titles:
                savedDeadTitles = new();

                foreach (KeyValuePair<Hero, string> at in assignedTitles.Where(item => item.Key.IsDead))
                    savedDeadTitles[at.Key.StringId] = at.Value;

                string serialized = JsonConvert.SerializeObject(savedDeadTitles);
                dataStore.SyncData(dtKey, ref serialized);
                savedDeadTitles = null;
            }
            else if (saveVersion >= 2)
            {
                // Deserializing dead heroes' titles (will be applied in OnSessionLaunched):
                string? serialized = null;
                dataStore.SyncData(dtKey, ref serialized);

                if (string.IsNullOrEmpty(serialized))
                    return;

                savedDeadTitles = JsonConvert.DeserializeObject<Dictionary<string, string>>(serialized);
            }
            else
                Util.Log.Print($"Savegame version of {saveVersion}: skipping deserialization of dead noble titles...");
        }

        private void OnNewGameCreated(CampaignGameStarter starter) =>
            Util.Log.Print($"Starting new campaign on {SubModule.Name} v{SubModule.Version} with savegame version of {CurrentSaveVersion}...");

        private void OnGameLoaded(CampaignGameStarter starter) =>
            Util.Log.Print($"Loading campaign on {SubModule.Name} v{SubModule.Version} with savegame version of {saveVersion}...");

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            saveVersion = CurrentSaveVersion; // By now (and no later), it's safe to update the save to the latest savegame version

            AddTitlesToLivingHeroes();

            if (savedDeadTitles is null)
                return;

            foreach (KeyValuePair<string, string> item in savedDeadTitles)
            {
                if (Campaign.Current.CampaignObjectManager.Find<Hero>(item.Key) is not Hero hero)
                {
                    Util.Log.Print($">> ERROR: Hero ID lookup failed for hero {item.Key} with title {item.Value}");
                    continue;
                }

                AddTitleToHero(hero, item.Value);
            }

            savedDeadTitles = null;
        }

        private void OnDailyTick()
        {
            // Remove and unregister all titles from living heroes
            RemoveTitlesFromLivingHeroes();

            // Now add currently applicable titles to living heroes
            AddTitlesToLivingHeroes();
        }

        // Leave no trace in the save. Remove all titles from all heroes. Keep their assignment records.
        private void OnBeforeSave()
        {
            Util.Log.Print($"{nameof(OnBeforeSave)}: Temporarily removing title prefixes from all heroes...");

            foreach (KeyValuePair<Hero, string> at in assignedTitles)
                RemoveTitleFromHero(at.Key, unregisterTitle: false);
        }

        internal void OnAfterSave() // Called from a Harmony patch rather than event dispatch
        {
            Util.Log.Print($"{nameof(OnAfterSave)}: Restoring title prefixes to all heroes...");

            // Restore all title prefixes to all heroes using the still-existing assignment records.
            foreach (KeyValuePair<Hero, string> at in assignedTitles)
                AddTitleToHero(at.Key, at.Value, overrideTitle: true, registerTitle: false);
        }

        private void AddTitlesToLivingHeroes()
        {
            // All living, titled heroes are associated with kingdoms for now, so go straight to the source
            Util.Log.Print("Adding kingdom-based noble titles...");

            foreach (Kingdom k in Kingdom.All.Where(x => !x.IsEliminated))
            {
                AddTitlesToKingdomHeroes(k);
            }
        }

        private void AddTitlesToKingdomHeroes(Kingdom kingdom)
        {
            List<string> tr = new() { $"Adding noble titles to {kingdom.Name}..." };
            // Common Nobles, not a Clan Leader
            List<Hero> commonNobles = kingdom.Clans
                .Where(c =>
                    !c.IsClanTypeMercenary &&
                    !c.IsUnderMercenaryService &&
                    c.Leader != null &&
                    c.Leader.IsAlive &&
                    c.Leader.IsLord)
                .SelectMany(c => c.Lords.Where(h => h != c.Leader && (h.IsKnownToPlayer || !titleDb.settings.General.FogOfWar)))
                .ToList();
            foreach (Hero h in commonNobles) AssignNobleTitle(h, titleDb.GetLesserNobleTitle(kingdom.Culture));

            /* The vassals first...
             *
             * We consider all noble, active vassal clans and sort them by their "fief score" and, as a tie-breaker,
             * their renown in ascending order (weakest -> strongest). For the fief score, 3 castles = 1 town.
             * Finally, we select the ordered list of their leaders.
             */

            List<Hero> vassals = kingdom.Clans
                .Where(c =>
                    c != kingdom.RulingClan &&
                    !c.IsClanTypeMercenary &&
                    !c.IsUnderMercenaryService &&
                    c.Leader != null &&
                    c.Leader.IsAlive &&
                    c.Leader.IsLord)
                .OrderBy(c => GetFiefScore(c))
                .ThenBy(c => c.Renown)
                .Select(c => c.Leader)
                .Where(h => h.IsKnownToPlayer || !titleDb.settings.General.FogOfWar)
                .ToList();
            int nBarons = 0;
            // First, pass over all barons.
            foreach (Hero? h in vassals)
            {
                // Are they a baron?
                if (GetFiefScore(h.Clan) < 3)
                {
                    ++nBarons;
                    AssignNobleTitle(h, titleDb.GetBaronTitle(kingdom.Culture));
                    tr.Add(GetHeroTrace(h, "BARON"));
                }
                else // They must be a count or duke. We're done here.
                    break;
            }
            // The allowed number of dukes is a third of the total non-baron noble vassals.
            int nBigVassals = vassals.Count - nBarons;
            int nDukes = nBigVassals / 3; // Round down
            int nCounts = nBigVassals - nDukes;
            int maxDukeIdx = vassals.Count - 1;
            int maxCountIdx = maxDukeIdx - nDukes;
            int maxBaronIdx = maxCountIdx - nCounts;
            // Counts:
            for (int i = maxCountIdx; i > maxBaronIdx; --i)
            {
                AssignNobleTitle(vassals[i], titleDb.GetCountTitle(kingdom.Culture));
                tr.Add(GetHeroTrace(vassals[i], "COUNT"));
            }
            // Dukes:
            for (int i = maxDukeIdx; i > maxCountIdx; --i)
            {
                AssignNobleTitle(vassals[i], titleDb.GetDukeTitle(kingdom.Culture));
                tr.Add(GetHeroTrace(vassals[i], "DUKE"));
            }
            // Finally, the most obvious, the ruler (King) title:
            if (kingdom.Leader != null &&
                !Kingdom.All.Where(k => k != kingdom).SelectMany(k => k.Lords).Where(h => h == kingdom.Leader).Any()) // fix for stale ruler status in defunct kingdoms
            {
                AssignNobleTitle(kingdom.Leader, titleDb.GetKingTitle(kingdom.Culture));
                tr.Add(GetHeroTrace(kingdom.Leader, "KING"));
            }
            Util.Log.Print(tr);
        }
        private void AddTitlesToCommonHeros(Kingdom kingdom)
        {
            List<Hero> vassals = kingdom.Clans
                .Where(c =>
                    c != kingdom.RulingClan &&
                    !c.IsClanTypeMercenary &&
                    !c.IsUnderMercenaryService &&
                    c.Leader != null &&
                    c.Leader.IsAlive &&
                    c.Leader.IsLord)
                .OrderBy(c => GetFiefScore(c))
                .ThenBy(c => c.Renown)
                .Select(c => c.Leader)
                .ToList();
        }
        private string GetHeroTrace(Hero h, string rank) =>
            $" -> {rank}: {h.Name} [Fief Score: {GetFiefScore(h.Clan)} / Renown: {h.Clan.Renown:F0}]";

        private int GetFiefScore(Clan clan) => clan.Fiefs.Sum(t => t.IsTown ? 3 : 1);

        private void AssignNobleTitle(Hero hero, TitleDb.Entry title)
        {
            string titlePrefix = hero.IsFemale ? title.Female : title.Male;
            AddTitleToHero(hero, titlePrefix);

            // Should their spouse also get the same title (after gender adjustment)?
            // If the spouse is the leader of a clan (as we currently assume `hero` is a clan leader too,
            //     it'd also be a different clan) and that clan belongs to any kingdom, no.
            // Else, yes.

            Hero spouse = hero.Spouse;

            if (spouse == null ||
                spouse.IsDead ||
                spouse.Clan?.Leader == spouse && spouse.Clan.Kingdom != null)
                return;

            // Sure. Give the spouse the ruler consort title, which is currently and probably always will
            // be the same as the ruler title, adjusted for gender.

            titlePrefix = spouse.IsFemale ? title.Female : title.Male;
            AddTitleToHero(spouse, titlePrefix);
        }
        private void AddTitleToHero(Hero hero, string titleFormat, bool overrideTitle = false, bool registerTitle = true)
        {
            if (assignedTitles.TryGetValue(hero, out string oldTitlePrefix))
            {
                if (overrideTitle && !titleFormat.Equals(oldTitlePrefix))
                    RemoveTitleFromHero(hero);
                else if (!overrideTitle)
                {
                    Util.Log.Print($">> WARNING: Tried to add title \"{titleFormat}\" to hero \"{hero.Name}\" with pre-assigned title \"{oldTitlePrefix}\"");
                    return;
                }
            }
            if (registerTitle)
                assignedTitles[hero] = titleFormat;
            TextObject name = hero.Name;
            List<string> fiefNames = hero.Clan.Fiefs.Select(f => f.Name.ToString()).ToList();
            string stringFiefs = string.Join<string>(",", fiefNames.Take(Math.Min(fiefNames.Count, 3)));
            hero.SetName(new TextObject(string.Format(titleFormat, new string[] { name.ToString(), stringFiefs })), name);
        }
        private void RemoveTitlesFromLivingHeroes(bool unregisterTitles = true)
        {
            foreach (Hero? h in assignedTitles.Keys.Where(h => h.IsAlive).ToList())
                RemoveTitleFromHero(h, unregisterTitles);
        }
        private void RemoveTitleFromHero(Hero hero, bool unregisterTitle = true)
        {
            string name = hero.Name.ToString();
            string title = string.Format(assignedTitles[hero], hero.FirstName);
            if (name != title)
            {
                Util.Log.Print($">> WARNING: Expected title prefix not found in hero name when removing title! Title prefix: \"{title}\" | Name: \"{name}\"");
                return;
            }

            string fullName = Regex.Replace(name, assignedTitles[hero].Replace("{0}", "(.+?)"), @"$1");
            if (unregisterTitle)
                assignedTitles.Remove(hero);
            hero.SetName(new TextObject(fullName), hero.FirstName);
        }

        private readonly Dictionary<Hero, string> assignedTitles = new Dictionary<Hero, string>();

        private readonly TitleDb titleDb = new TitleDb();

        private Dictionary<string, string>? savedDeadTitles; // Maps a Hero's string ID to a static title prefix for dead heroes, only used for (de)serialization

        private int saveVersion = 0;

        private const int CurrentSaveVersion = 2;
    }
}
