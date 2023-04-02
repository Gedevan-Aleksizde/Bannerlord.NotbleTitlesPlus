using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
// using TaleWorlds.CampaignSystem.Party;
// using TaleWorlds.CampaignSystem.Conversation.Tags;
// using TaleWorlds.Library;
using TaleWorlds.Localization;
// using TaleWorlds.ObjectSystem;

namespace NobleTitlesPlus
{
    internal sealed class TitleBehavior : CampaignBehaviorBase
    {
        public static Nomenclatura nomenclatura = new();
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnGameLoaded));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));
            // CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
        }
        public override void SyncData(IDataStore dataStore)
        {
            // No need to save data.
        }
        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            Util.Log.Print("Starting new campaign");
            nomenclatura.UpdateAll();
            // Util.Log.Print($"Starting new campaign on {SubModule.Name} v{SubModule.Version} with savegame version of {CurrentSaveVersion}...");
        }
        private void OnGameLoaded(CampaignGameStarter starter)
        {
            Util.Log.Print("Loading campaign");
            nomenclatura.UpdateAll();
            // Util.Log.Print($"Loading campaign on {SubModule.Name} v{SubModule.Version} with savegame version of {this.saveVersion}...");
        }
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            Harmony.DEBUG = true;
            Harmony harmony = new(SubModule.HarmonyDomain);
            harmony.PatchAll();
        }
        private void OnDailyTick()
        {
            nomenclatura.UpdateAll();
        }
        // private int GetFiefScore(Clan clan) => clan.Fiefs.Sum(t => t.IsTown ? 3 : 1);
        private void OverrideLocalization()
        {
            foreach(Kingdom k in Kingdom.All)
            {
                // GameTexts.SetText("str_faction_noble_name_with_title", out TextObject text, k.Culture.StringId);
            }
        }
    }
    class Nomenclatura
    {
        private readonly TitleDb titleDb = new();
        // public Dictionary<Hero, TextObject> NameTitle { get; private set; } = new();
        public Dictionary<Hero, TitleRank> HeroRank { get; private set; } = new();
        public Nomenclatura(bool update = false)
        {
            if(update) this.UpdateAll();
        }
        public TextObject GetTitle(bool isFemale, string cultureId, TitleRank rank)
        {
            return this.titleDb.GetTitle(isFemale, cultureId, rank);
        } 
        public void UpdateAll()
        {
            foreach (Kingdom k in Kingdom.All.Where(x => !x.IsEliminated))
            {
                this.AddTitlesToKingdomHeroes(k);
            }
            this.RemoveTitleFromDead();
        }
        private void RemoveTitleFromDead()
        {
            /*foreach(Hero h in this.NameTitle.Keys.ToArray())
            {
                if (h.IsDead)
                {
                    this.NameTitle.Remove(h);
                }
            }*/
            foreach(Hero h in this.HeroRank.Keys.ToArray())
            {
                if (h.IsDead)
                {
                    this.HeroRank.Remove(h);                }
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
                .SelectMany(c => c.Lords.Where(h => h != c.Leader && (h.IsKnownToPlayer || !this.titleDb.settings.General.FogOfWar)))
                .ToList();
            foreach (Hero h in commonNobles)
            {
                this.HeroRank[h] = TitleRank.Noble;
                tr.Add(this.GetHeroTrace(h, TitleRank.Noble));
                // this.NameTitle[h] = h.IsFemale ? this.titleDb.GetLesserNobleTitle(kingdom.Culture).FemaleFormat : this.titleDb.GetLesserNobleTitle(kingdom.Culture).MaleFormat;
            }
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
                .OrderBy(c => this.GetFiefScore(c))
                .ThenBy(c => c.Renown)
                .Select(c => c.Leader)
                .Where(h => h.IsKnownToPlayer || !this.titleDb.settings.General.FogOfWar)
                .ToList();
            int nBarons = 0;
            // First, pass over all barons.
            foreach (Hero? h in vassals)
            {
                // Are they a baron?
                if (this.GetFiefScore(h.Clan) < 3)
                {
                    ++nBarons;
                    this.HeroRank[h] = TitleRank.Baron;
                    tr.Add(this.GetHeroTrace(h, TitleRank.Baron));
                    // this.NameTitle[h] = h.IsFemale ? this.titleDb.GetBaronTitle(kingdom.Culture).FemaleFormat: this.titleDb.GetBaronTitle(kingdom.Culture).MaleFormat;
                    // tr.Add(GetHeroTrace(h, "BARON"));
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
                this.HeroRank[vassals[i]] = TitleRank.Count;
                tr.Add(this.GetHeroTrace(vassals[i], TitleRank.Count));
                // this.NameTitle[vassals[i]] = vassals[i].IsFemale ? this.titleDb.GetCountTitle(kingdom.Culture).Female : this.titleDb.GetCountTitle(kingdom.Culture).Male;
                // tr.Add(this.GetHeroTrace(vassals[i], "COUNT"));
            }
            // Dukes:
            for (int i = maxDukeIdx; i > maxCountIdx; --i)
            {
                this.HeroRank[vassals[i]] = TitleRank.Duke;
                tr.Add(this.GetHeroTrace(vassals[i], TitleRank.Duke));
                // this.NameTitle[vassals[i]] = vassals[i].IsFemale ? this.titleDb.GetDukeTitle(kingdom.Culture).Female : this.titleDb.GetDukeTitle(kingdom.Culture).Male;
                // tr.Add(this.GetHeroTrace(vassals[i], "DUKE"));
            }
            // Finally, the most obvious, the ruler (King) title:
            if (kingdom.Leader != null &&
                !Kingdom.All.Where(k => k != kingdom).SelectMany(k => k.Lords).Where(h => h == kingdom.Leader).Any()) // fix for stale ruler status in defunct kingdoms
            {
                this.HeroRank[kingdom.Leader] = TitleRank.King;
                tr.Add(this.GetHeroTrace(kingdom.Leader, TitleRank.King));
                // this.NameTitle[kingdom.Leader] = kingdom.Leader.IsFemale? this.titleDb.GetKingTitle(kingdom.Culture).Female: this.titleDb.GetKingTitle(kingdom.Culture).Male;
                // tr.Add(this.GetHeroTrace(kingdom.Leader, "KING"));
            }
            Util.Log.Print(tr);
        }
        private int GetFiefScore(Clan clan) => clan.Fiefs.Sum(t => t.IsTown ? 3 : 1);
        private string GetHeroTrace(Hero h, TitleRank rank) =>
            $" -> {rank}: {h.Name} [Fief Score: {this.GetFiefScore(h.Clan)} / Renown: {h.Clan.Renown:F0}]";
    }
}
