using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using NobleTitlesPlus.DB;
using NobleTitlesPlus.Settings;

namespace NobleTitlesPlus
{
    internal sealed class TitleBehavior : CampaignBehaviorBase
    {
        public static Options options { get; set; }
        public static Nomenclatura nomenclatura = new();
        // private Harmony harmony;
        public override void RegisterEvents()
        {
            // TODO: remove unused event
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            // CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, (a, b) => this.UpdateArmyNames());
            // CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, () => UpdateArmyNames());          
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnGameLoaded));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));
        }
        public TitleBehavior(Options opt)
        {
            Util.Log.Print($">> [DEBUG] CampaignBehavior constructor called: kingdom={Kingdom.All.Count}");
            options = opt;
        }
        public override void SyncData(IDataStore dataStore)
        {
        }
        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            Util.Log.Print($">> [DEBUG] OnNewGameCreated: kingdom={Kingdom.All.Count}");
            options.TitleSet.Initialize();
            nomenclatura.UpdateAll();
            if (options.VerboseLog) Util.Log.Print($">> [INFO] Starting new campaign on {SubModule.Name}");
        }
        private void OnGameLoaded(CampaignGameStarter starter)
        {
            Util.Log.Print($">> [DEBUG] OnGameLoaded: kingdom={Kingdom.All.Count}");
            if (options.VerboseLog) Util.Log.Print(">> [INFO] Loading campaign");
            nomenclatura.UpdateAll();
            if (options.VerboseLog) Util.Log.Print($">> [INFO] Loading campaign on {SubModule.Name}");
        }
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            Util.Log.Print($"OnSessionLaunched: kingdom={Kingdom.All.Count}");
        }
        private void OnDailyTick()
        {
            nomenclatura.UpdateAll();
        }
        public void UpdateArmyNames()
        {
            // TODO
            if(Campaign.Current != null)
            {
                foreach (MobileParty mp in MobileParty.All)
                {
                    mp.Army?.UpdateName();
                }
            }
        }
    }
    class Nomenclatura
    {
        public Dictionary<Hero, TitleRank> HeroRank { get; private set; } = new();
        public Dictionary<Clan, TextObject> FiefLists { get; private set; } = new();
        public Nomenclatura(bool update = false)
        {
            if(update) this.UpdateAll();
        }
        public void UpdateAll()
        {
            foreach (Kingdom k in Kingdom.All.Where(x => !x.IsEliminated))
            {
                this.AddTitlesToKingdomHeroes(k);
            }
            foreach(Clan c in Clan.All)
            {
                this.UpdateFiefList(c);
            }
            this.AddTitlesToMinorFaction();
            this.RemoveTitleFromDead();
        }
        private void RemoveTitleFromDead()
        {
            foreach(Hero h in this.HeroRank.Keys.ToArray())
            {
                if (h.IsDead)
                {
                    this.HeroRank.Remove(h);
                }
            }
        }
        public void UpdateFiefList(Clan clan)
        {
            List<string> fiefList = clan.Fiefs.Take(TitleBehavior.options.MaxFiefNames).Select(x => x.Name.ToString()).ToList();
            
            if( fiefList.Count() <= 1 || TitleBehavior.options.MaxFiefNames <= 1)
            {
                this.FiefLists[clan] = new TextObject(fiefList.FirstOrDefault());
            }
            else if (fiefList.Count > 1)
            {
                string sep = TitleBehavior.options.FiefNameSeparator + " ";
                string fiefs = string.Join(sep, fiefList.Take(Math.Min(fiefList.Count() - 1, TitleBehavior.options.MaxFiefNames)).ToArray<string>());
                string lastElement = string.Join(" ", new string[] { TitleBehavior.options.FiefNameSeparatorLast, fiefList.Last() });
                this.FiefLists[clan] = new TextObject(string.Join(" ", new string[] { fiefs, lastElement }));
            }
            else
            {
                this.FiefLists[clan] = GameTexts.FindText("str_ntp_landless");
            }
        }
        private void AddTitlesToKingdomHeroes(Kingdom kingdom)
        {
            List<string> tr = new() { $">> [INFO] Adding noble titles to \"{kingdom.Name}\" (ID={kingdom.StringId}) (culture={kingdom.Culture.StringId})..." };
            // Common Nobles, not a Clan Leader
            List<Hero> commonNobles = kingdom.Clans
                .Where(c =>
                    !c.IsClanTypeMercenary &&
                    !c.IsUnderMercenaryService &&
                    c.Leader != null &&
                    c.Leader.IsAlive &&
                    c.Leader.IsLord)
                .SelectMany(c => c.Lords.Where(h => h != c.Leader && h.IsAlive && !h.IsChild))
                .ToList();
            foreach (Hero h in commonNobles)
            {
                this.HeroRank[h] = TitleRank.Noble;
                tr.Add(this.GetHeroTrace(h, TitleRank.Noble));
            }
            // Crown Prince/Princess
            List<Hero> royals = kingdom.RulingClan.Heroes.Where(h => !h.IsFactionLeader && h != h.Clan.Leader.Spouse).ToList();
            switch (TitleBehavior.options.Inheritance.SelectedValue)
            {
            }
            List<Hero> heirs = (DB.Inheritance)TitleBehavior.options.Inheritance.SelectedIndex switch
            {
                Inheritance.Primogeniture => royals.Where(h => h.Father == kingdom.Leader || h.Mother == kingdom.Leader).OrderBy(h => -h.Age).ToList(),
                Inheritance.Adult => royals.Where(h => (h.Father == kingdom.Leader || h.Mother == kingdom.Leader) && !h.IsChild).OrderBy(h => -h.Age).ToList(),
                Inheritance.Elder => royals.OrderBy(h => -h.Age).ToList(),
                _ => new(),
            };
            if (heirs.Count > 0)
            {
                this.HeroRank[heirs.First()] = TitleRank.Prince;
                tr.Add(this.GetHeroTrace(heirs.First(), TitleRank.Prince));
                royals = royals.Where(h => h != heirs.First()).ToList();
            }
            foreach (Hero h in royals)
            {
                this.HeroRank[h] = TitleRank.Royal;
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
                    if (TitleBehavior.options.SpouseTitle && h.Spouse != null && h.Spouse.IsAlive)
                    {
                        this.HeroRank[h.Spouse] = TitleRank.Baron;
                        tr.Add(this.GetHeroTrace(h.Spouse, TitleRank.Baron));
                    }
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
            for (int i = maxCountIdx; i > maxBaronIdx; --i)
            {
                this.HeroRank[vassals[i]] = TitleRank.Count;
                tr.Add(this.GetHeroTrace(vassals[i], TitleRank.Count));
                if (TitleBehavior.options.SpouseTitle && vassals[i].Spouse != null && vassals[i].Spouse.IsAlive)
                {
                    this.HeroRank[vassals[i].Spouse] = TitleRank.Count;
                    tr.Add(this.GetHeroTrace(vassals[i].Spouse, TitleRank.Count));
                }
            }
            for (int i = maxDukeIdx; i > maxCountIdx; --i)
            {
                this.HeroRank[vassals[i]] = TitleRank.Duke;
                tr.Add(this.GetHeroTrace(vassals[i], TitleRank.Duke));
                if (TitleBehavior.options.SpouseTitle && vassals[i].Spouse != null && vassals[i].Spouse.IsAlive)
                {
                    this.HeroRank[vassals[i].Spouse] = TitleRank.Duke;
                    tr.Add(this.GetHeroTrace(vassals[i].Spouse, TitleRank.Duke));
                }
            }
            if (kingdom.Leader != null &&
                !Kingdom.All.Where(k => k != kingdom).SelectMany(k => k.Lords).Where(h => h == kingdom.Leader).Any()) // fix for stale ruler status in defunct kingdoms
            {
                this.HeroRank[kingdom.Leader] = TitleRank.King;
                tr.Add(this.GetHeroTrace(kingdom.Leader, TitleRank.King));
                if (kingdom.Leader.Spouse != null && kingdom.Leader.Spouse.IsAlive)
                {
                    this.HeroRank[kingdom.Leader.Spouse] = TitleRank.King;
                    tr.Add(this.GetHeroTrace(kingdom.Leader.Spouse, TitleRank.King));
                }
            }
            if (TitleBehavior.options.VerboseLog) Util.Log.Print(tr);
        }
        private void AddTitlesToMinorFaction()
        {
            foreach (Clan c in Clan.All.Where(c => !c.IsEliminated && c.IsMinorFaction && (!c.Leader?.IsHumanPlayerCharacter ?? true)))
            {
                List<string> tr = new() { $">> [INFO] Adding minor faction titles to {c.Name} ({c.StringId})..." };
                foreach (Hero h in c.Heroes)
                {
                    if (h.IsAlive)
                    {
                        if (h == h.Clan.Leader)
                        {
                            this.HeroRank[h] = TitleRank.King;
                            tr.Add(this.GetHeroTrace(h, TitleRank.King));
                        }
                        else
                        {
                            this.HeroRank[h] = TitleRank.Noble;
                            tr.Add(this.GetHeroTrace(h, TitleRank.Noble));
                        }
                    }
                }
                if (TitleBehavior.options.VerboseLog) Util.Log.Print(tr);
            }
        }
        private int GetFiefScore(Clan clan) => clan.Fiefs.Sum(t => t.IsTown ? 3 : 1);
        private string GetHeroTrace(Hero h, TitleRank rank) =>
            $" -> {rank}: {h.Name} [Fief Score: {(rank == TitleRank.King || rank == TitleRank.Duke || rank == TitleRank.Count || rank == TitleRank.Baron? this.GetFiefScore(h.Clan): 0)} / Renown: {h.Clan.Renown:F0}]";
    }
}
