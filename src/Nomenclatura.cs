using NobleTitlesPlus.DB;
using NobleTitlesPlus.json;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NobleTitlesPlus
{
    /// <summary>
    /// Cache for access the text faster
    /// </summary>
    class Nomenclatura
    {
        public int thretholdBaron;
        public int divisorDuke;
        public Dictionary<Hero, TitleRank> HeroRank { get; private set; } = new();
        public Dictionary<Clan, (TextObject FiefText, TextObject ShokuhoProvName, ClanNamePair ClanNames)> ClanAttrs { get; private set; } = new();
        public Nomenclatura(int thretholdBaron = 3, int divisorDuke = 3, bool update = false)
        {
            this.thretholdBaron = thretholdBaron;
            this.divisorDuke = divisorDuke;
            if (update) this.UpdateAll(false);
        }
        public TitleRank? FindHeroRankById(string id)
        {
            Hero hero = Hero.AllAliveHeroes.Where(x => x.StringId == id).First();
            if (this.HeroRank.TryGetValue(hero, out var rank))
            {
                return rank;
            }
            else
            {
                return null;
            }
        }
        public void UpdateAll(bool changeClanName)
        {
            foreach (Kingdom k in Kingdom.All.Where(x => !x.IsEliminated))
            {
                this.AddTitlesToKingdomHeroes(k);
            }
            foreach (Clan c in Clan.All)
            {
                this.UpdateFiefList(c);
                if (changeClanName) this.UpdateClanName(c);
            }
            this.AddTitlesToMinorFaction();
            this.RemoveTitleFromDead();
        }
        private void RemoveTitleFromDead()
        {
            foreach (Hero h in this.HeroRank.Keys.ToArray())
            {
                if (h.IsDead)
                {
                    this.HeroRank.Remove(h);
                }
            }
        }
        /// <summary>
        /// updates each clan's fief information and values assigned to fief and province variable.
        /// </summary>
        /// <param name="clan"></param>
        public void UpdateFiefList(Clan clan)
        {
            // TODO: keeping TextObject causes replacing wrong name, very weird.
            List<(string SettlementId, string Name)> fiefTupleList = clan.Fiefs.Take(TitleBehavior.Options.MaxFiefNames).Select(x => (SettlementId: x.Settlement.StringId, Name: x.Name.ToString())).ToList();

            if (fiefTupleList.Count() <= 1 || TitleBehavior.Options.MaxFiefNames <= 1)
            {
                (string SettlementId, string Name) = fiefTupleList.FirstOrDefault();
                if (!TitleBehavior.Options.TitleSet.shokuhoCastleProvinceMap.TryGetValue(SettlementId ?? "", out string strProvId))
                {
                    strProvId = "default";
                }
                this.ClanAttrs[clan] = (
                    FiefText: new TextObject(Name),
                    ShokuhoProvName: GameTexts.FindText("ntp_sho_prov", strProvId),
                    ClanNames: this.ClanAttrs.ContainsKey(clan) ? this.ClanAttrs[clan].ClanNames : new(""));
            }
            else if (fiefTupleList.Count > 1)
            {
                string sep = TitleBehavior.Options.FiefNameSeparator + " ";
                string fiefs = string.Join(sep, fiefTupleList.Take(Math.Min(fiefTupleList.Count() - 1, TitleBehavior.Options.MaxFiefNames)).Select(x => x.Name).ToArray<string>());
                string lastElement = string.Join(" ", new string[] { TitleBehavior.Options.FiefNameSeparatorLast, fiefTupleList.Last().Name });

                if (!TitleBehavior.Options.TitleSet.shokuhoCastleProvinceMap.TryGetValue(fiefTupleList.Last().SettlementId ?? "", out string strProvId))
                {
                    strProvId = "default";
                }
                this.ClanAttrs[clan] = (
                    FiefText: new TextObject(string.Join(" ", new string[] { fiefs, lastElement })),
                    ShokuhoProvName: GameTexts.FindText("ntp_sho_prov", strProvId),
                    ClanNames: this.ClanAttrs.ContainsKey(clan) ? this.ClanAttrs[clan].ClanNames : new("")
                    );
            }
            else
            {
                this.ClanAttrs[clan] = (
                    FiefText: GameTexts.FindText("ntp_landless"),
                    ShokuhoProvName: GameTexts.FindText("ntp_sho_prov.default"),
                    ClanNames: this.ClanAttrs.ContainsKey(clan) ? this.ClanAttrs[clan].ClanNames : new("")
                    );
            }
        }
        /// <summary>
        /// Store All clan's long names and short names. This feature is intended to use with Shokuhō which has duplicated clan names. The clan's long name means game-originally name for indentification, and the short name is normally format which can be duplicated with another
        /// </summary>
        /// <param name=""></param>
        public void UpdateClanName(Clan clan)
        {
            TextObject shortName;
            TextObject longName;
            if (TitleBehavior.Options.TitleSet.shokuhoClanNames.TryGetValue(clan.StringId, out ClanNamePair namePair))
            {
                if (namePair?.ClanShort is null || namePair?.ClanLong is null)
                {
                    throw new NullReferenceException("Clan Names failed to be loaded.");
                }
                shortName = namePair.ClanShort;
                longName = namePair.ClanLong;
            }
            else
            {
                shortName = clan.InformalName;
                longName = clan.Name;
            }
            bool contains = this.ClanAttrs.ContainsKey(clan);
            if (contains)
            {
                (TextObject FiefText, TextObject ShokuhoProvName, ClanNamePair ClanNames) t = this.ClanAttrs[clan];
            }
            clan.ChangeClanName(longName, shortName);
            this.ClanAttrs[clan] = (
                FiefText: contains ? this.ClanAttrs[clan].FiefText : new(""),
                ShokuhoProvName: contains ? this.ClanAttrs[clan].ShokuhoProvName : new(""),
                ClanNames: new(shortName, longName)
                );
        }
        /// <summary>
        /// Adds titles to all heroes in a kingdom
        /// </summary>
        /// <param name="kingdom"></param>
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
            switch (TitleBehavior.Options.Inheritance.SelectedValue)
            {
            }
            List<Hero> heirs = (DB.Inheritance)TitleBehavior.Options.Inheritance.SelectedIndex switch
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
                if (this.GetFiefScore(h.Clan) < thretholdBaron)
                {
                    ++nBarons;
                    this.HeroRank[h] = TitleRank.Baron;
                    tr.Add(this.GetHeroTrace(h, TitleRank.Baron));
                    if (TitleBehavior.Options.SpouseTitle && h.Spouse != null && h.Spouse.IsAlive)
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
            int nDukes = nBigVassals / divisorDuke; // Round down
            int nCounts = nBigVassals - nDukes;
            int maxDukeIdx = vassals.Count - 1;
            int maxCountIdx = maxDukeIdx - nDukes;
            int maxBaronIdx = maxCountIdx - nCounts;
            for (int i = maxCountIdx; i > maxBaronIdx; --i)
            {
                this.HeroRank[vassals[i]] = TitleRank.Count;
                tr.Add(this.GetHeroTrace(vassals[i], TitleRank.Count));
                if (TitleBehavior.Options.SpouseTitle && vassals[i].Spouse != null && vassals[i].Spouse.IsAlive)
                {
                    this.HeroRank[vassals[i].Spouse] = TitleRank.Count;
                    tr.Add(this.GetHeroTrace(vassals[i].Spouse, TitleRank.Count));
                }
            }
            for (int i = maxDukeIdx; i > maxCountIdx; --i)
            {
                this.HeroRank[vassals[i]] = TitleRank.Duke;
                tr.Add(this.GetHeroTrace(vassals[i], TitleRank.Duke));
                if (TitleBehavior.Options.SpouseTitle && vassals[i].Spouse != null && vassals[i].Spouse.IsAlive)
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
            if (TitleBehavior.Options.VerboseLog) Util.Log.Print(tr);
        }
        /// <summary>
        /// overwrite the single surviving Imperial faction with the united Imperial titles.
        /// </summary>
        public void OverwriteWithImperialFormats(Kingdom kingdom)
        {
            Util.Log.Print(">> [INFO] The Empire is reunited. The suriving faction inherit legitimate Imperial titles.");
            kingdom.ChangeKingdomName(GameTexts.FindText("str_faction_formal_name_for_culture", "empire"), GameTexts.FindText("str_faction_informal_name_for_culture", "empire"));
            TitleBehavior.Options.TitleSet.cultures.TryGetValue("empire", out TitleSet.FactionTitleSet fts);
            TitleBehavior.Options.TitleSet.factions.Add(kingdom.StringId, fts);
        }

        /// <summary>
        /// Adds all minor faction menbers titles 
        /// </summary>
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
                if (TitleBehavior.Options.VerboseLog) Util.Log.Print(tr);
            }
        }
        private int GetFiefScore(Clan clan) => clan.Fiefs.Sum(t => t.IsTown ? 3 : 1);
        private string GetHeroTrace(Hero h, TitleRank rank) =>
            $" -> {rank}: {h.Name} [Fief Score: {(rank == TitleRank.King || rank == TitleRank.Duke || rank == TitleRank.Count || rank == TitleRank.Baron ? this.GetFiefScore(h.Clan) : 0)} / Renown: {h.Clan.Renown:F0}]";
    }
}
