using NobleTitlesPlus.DB;
using NobleTitlesPlus.json;
using NobleTitlesPlus.MCMSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NobleTitlesPlus
{
    /// <summary>
    /// All NPC proflie Cache to access the text faster
    /// </summary>
    public class Nomenclatura
    {
        public Dictionary<Hero, HeroProfile> HeroProfiles { get; private set; } = new();
        public Dictionary<Clan, (TextObject FiefText, TextObject ShokuhoProvName, ClanNamePair ClanNames)> ClanAttrs { get; private set; } = new();
        public Nomenclatura(bool update = false)
        {
            if (update) this.UpdateAll();
        }
        public TitleRank? FindHeroRankById(string id)
        {
            Hero hero = Hero.AllAliveHeroes.Where(x => x.StringId == id).First();
            if (this.HeroProfiles.TryGetValue(hero, out HeroProfile heroProfile))
            {
                return heroProfile.TitleRank;
            }
            else
            {
                return null;
            }
        }
        public void UpdateAll()
        {
            foreach (Kingdom k in Kingdom.All.Where(x => !x.IsEliminated))
            {
                this.AddTitlesToKingdomHeroes(k);
            }
            foreach (Clan c in Clan.All)
            {
                this.UpdateFiefList(c);
            }
            this.AddTitlesToMinorFaction();
            this.RemoveTitleFromDead();
        }
        private void RemoveTitleFromDead()
        {
            foreach (Hero h in this.HeroProfiles.Keys.ToArray())
            {
                if (h.IsDead)
                {
                    this.HeroProfiles.Remove(h);
                }
            }
        }
        /// <summary>
        /// updates each clan's fief information and values assigned to fief and province variable.
        /// </summary>
        /// <param name="clan"></param>
        public void UpdateFiefList(Clan clan)
        {
            if (MCMRuntimeSettings.Instance is null)
            {
                Util.Log.Print("MCM setting instance is null at UpdateFielFList.", LogCategory.Warning);
                return;
            }
            // TODO: keeping TextObject causes replacing wrong name, very weird.
            List<(string SettlementId, string Name)> fiefTupleList = clan.Fiefs.Take(MCMRuntimeSettings.Instance.Options.MaxFiefNames).Select(x => (SettlementId: x.Settlement.StringId, Name: x.Name.ToString())).ToList();

            if (fiefTupleList.Count() <= 1 || MCMRuntimeSettings.Instance.Options.MaxFiefNames <= 1)
            {
                (string SettlementId, string Name) = fiefTupleList.FirstOrDefault();
                if (!MCMRuntimeSettings.Instance.Options.TitleSet.shokuhoCastleProvinceMap.TryGetValue(SettlementId ?? "", out string strProvId))
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
                string sep = MCMRuntimeSettings.Instance.Options.FiefNameSeparator + " ";
                string fiefs = string.Join(sep, fiefTupleList.Take(Math.Min(fiefTupleList.Count() - 1, MCMRuntimeSettings.Instance.Options.MaxFiefNames)).Select(x => x.Name).ToArray<string>());
                string lastElement = string.Join(" ", new string[] { MCMRuntimeSettings.Instance.Options.FiefNameSeparatorLast, fiefTupleList.Last().Name });

                if (!MCMRuntimeSettings.Instance.Options.TitleSet.shokuhoCastleProvinceMap.TryGetValue(fiefTupleList.Last().SettlementId ?? "", out string strProvId))
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
            if (MCMRuntimeSettings.Instance is null)
            {
                Util.Log.Print("MCM setting instance is null at UpdateClanName.", LogCategory.Warning);
                return;
            }
            TextObject shortName;
            TextObject longName;
            if (MCMRuntimeSettings.Instance.Options.TitleSet.shokuhoClanNames.TryGetValue(clan.StringId, out ClanNamePair namePair))
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
            if (MCMRuntimeSettings.Instance is null)
            {
                Util.Log.Print("MCM setting instance is null at AddTitlesToKingdomHeroes.", LogCategory.Warning);
                return;
            }
            List<string> tr = new() { $"Adding noble titles to \"{kingdom.Name}\" (ID={kingdom.StringId}) (culture={kingdom.Culture.StringId})..." };
            // Common Nobles, not a Clan Leader
            List<Hero> commonNobles = kingdom.Clans
                .Where(c =>
                    !c.IsClanTypeMercenary &&
                    !c.IsUnderMercenaryService &&
                    c.Leader != null &&
                    c.Leader.IsAlive &&
                    c.Leader.IsLord)
                .SelectMany(c => c.AliveLords.Where(h => h != c.Leader && !h.IsChild))
                .ToList();
            foreach (Hero h in commonNobles)
            {
                this.UpdateTitlerankInHeroProfiles(h, TitleRank.Noble);
                tr.Add(this.GetHeroTrace(h, TitleRank.Noble));
            }
            // Crown Prince/Princess
            List<Hero> royals = kingdom.RulingClan.Heroes.Where(h => !h.IsFactionLeader && h != h.Clan.Leader.Spouse).ToList();
            switch (MCMRuntimeSettings.Instance.Options.Inheritance)
            {
            }
            List<Hero> heirs = MCMRuntimeSettings.Instance.Options.Inheritance switch
            {
                Inheritance.Primogeniture => royals.Where(h => h.Father == kingdom.Leader || h.Mother == kingdom.Leader).OrderBy(h => -h.Age).ToList(),
                Inheritance.Adult => royals.Where(h => (h.Father == kingdom.Leader || h.Mother == kingdom.Leader) && !h.IsChild).OrderBy(h => -h.Age).ToList(),
                Inheritance.Elder => royals.OrderBy(h => -h.Age).ToList(),
                _ => new(),
            };
            if (heirs.Count > 0)
            {
                this.UpdateTitlerankInHeroProfiles(heirs.First(), TitleRank.Prince);
                tr.Add(this.GetHeroTrace(heirs.First(), TitleRank.Prince));
                royals = royals.Where(h => h != heirs.First()).ToList();
            }
            foreach (Hero h in royals)
            {
                this.UpdateTitlerankInHeroProfiles(h, TitleRank.Royal);
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
                if (this.GetFiefScore(h.Clan) < (MCMRuntimeSettings.Instance?.Options?.ThresholdBaron ?? 3))
                {
                    ++nBarons;
                    this.UpdateTitlerankInHeroProfiles(h, TitleRank.Baron);
                    tr.Add(this.GetHeroTrace(h, TitleRank.Baron));
                    if ((MCMRuntimeSettings.Instance?.Options?.SpouseTitle ?? false) && h.Spouse != null && h.Spouse.IsAlive)
                    {
                        this.UpdateTitlerankInHeroProfiles(h.Spouse, TitleRank.Baron);
                        tr.Add(this.GetHeroTrace(h.Spouse, TitleRank.Baron));
                    }
                }
                else // They must be a count or duke. We're done here.
                    break;
            }
            // The allowed number of dukes is a third of the total non-baron noble vassals.
            int nBigVassals = vassals.Count - nBarons;
            int nDukes = nBigVassals / MCMRuntimeSettings.Instance?.Options?.DivisorCapDuke ?? 3; // Round down
            int nCounts = nBigVassals - nDukes;
            int maxDukeIdx = vassals.Count - 1;
            int maxCountIdx = maxDukeIdx - nDukes;
            int maxBaronIdx = maxCountIdx - nCounts;
            for (int i = maxCountIdx; i > maxBaronIdx; --i)
            {
                this.UpdateTitlerankInHeroProfiles(vassals[i], TitleRank.Count);
                tr.Add(this.GetHeroTrace(vassals[i], TitleRank.Count));
                if ((MCMRuntimeSettings.Instance?.Options.SpouseTitle ?? false) && vassals[i].Spouse != null && vassals[i].Spouse.IsAlive)
                {
                    this.UpdateTitlerankInHeroProfiles(vassals[i].Spouse, TitleRank.Count);
                    tr.Add(this.GetHeroTrace(vassals[i].Spouse, TitleRank.Count));
                }
            }
            for (int i = maxDukeIdx; i > maxCountIdx; --i)
            {
                this.UpdateTitlerankInHeroProfiles(vassals[i], TitleRank.Duke);
                tr.Add(this.GetHeroTrace(vassals[i], TitleRank.Duke));
                if ((MCMRuntimeSettings.Instance?.Options.SpouseTitle ?? false) && vassals[i].Spouse != null && vassals[i].Spouse.IsAlive)
                {
                    this.UpdateTitlerankInHeroProfiles(vassals[i].Spouse, TitleRank.Duke);
                    tr.Add(this.GetHeroTrace(vassals[i].Spouse, TitleRank.Duke));
                }
            }
            if (kingdom.Leader != null &&
                !Kingdom.All.Where(k => k != kingdom).SelectMany(k => k.AliveLords).Where(h => h == kingdom.Leader).Any()) // fix for stale ruler status in defunct kingdoms
            {
                this.UpdateTitlerankInHeroProfiles(kingdom.Leader, TitleRank.King);
                tr.Add(this.GetHeroTrace(kingdom.Leader, TitleRank.King));
                if (kingdom.Leader.Spouse != null && kingdom.Leader.Spouse.IsAlive)
                {
                    this.UpdateTitlerankInHeroProfiles(kingdom.Leader.Spouse, TitleRank.King);
                    tr.Add(this.GetHeroTrace(kingdom.Leader.Spouse, TitleRank.King));
                }
            }
            if (MCMRuntimeSettings.Instance?.Options?.VerboseLog ?? true) Util.Log.Print(tr);
        }
        /// <summary>
        /// overwrite the single surviving Imperial faction with the united Imperial titles.
        /// </summary>
        public void OverwriteWithImperialFormats(Kingdom kingdom)
        {
            if (MCMRuntimeSettings.Instance is null)
            {
                Util.Log.Print("MCM setting instance is null at OverwriteWithImperialFormats.", LogCategory.Warning);
                return;
            }
            else
            {
                Util.Log.Print("The Empire is reunited. The suriving faction inherit legitimate Imperial titles.", LogCategory.Info);
            }
            kingdom.ChangeKingdomName(GameTexts.FindText("str_faction_formal_name_for_culture", "empire"), GameTexts.FindText("str_faction_informal_name_for_culture", "empire"));
            MCMRuntimeSettings.Instance.Options.TitleSet.cultures.TryGetValue("empire", out TitleSet.FactionTitleSet fts);
            if (MCMRuntimeSettings.Instance.Options.TitleSet.factions.ContainsKey(kingdom.StringId))
            {
                MCMRuntimeSettings.Instance.Options.TitleSet.factions[kingdom.StringId] = fts;
            }
            else
            {
                MCMRuntimeSettings.Instance.Options.TitleSet.factions.Add(kingdom.StringId, fts);
            }
        }
        /// <summary>
        /// Adds all minor faction menbers titles 
        /// </summary>
        private void AddTitlesToMinorFaction()
        {
            foreach (Clan c in Clan.All.Where(c => !c.IsEliminated && c.IsMinorFaction))
            {
                this.ShowClanDebugValues(c);
                // player clan is a minor faction, but the player character hero is not a minor faction hero.
                // I can't figure out how to use IsClanTypeMercenary.
                List<string> tr = new() { $"Adding minor faction titles to {c.Name} ({c.StringId})..." };
                foreach (Hero h in c.Heroes)
                {
                    if (h.IsAlive && IfMinorFactionHero(h)) // player clan is always a minor faction even when serving any kingdom.
                    {
                        if (h.IsClanLeader)
                        {
                            this.UpdateTitlerankInHeroProfiles(h, TitleRank.King);
                            tr.Add(this.GetHeroTrace(h, TitleRank.King));
                        }
                        else
                        {
                            this.UpdateTitlerankInHeroProfiles(h, TitleRank.Noble);
                            tr.Add(this.GetHeroTrace(h, TitleRank.Noble));
                        }
                    }
                }
                if (MCMRuntimeSettings.Instance?.Options?.VerboseLog ?? true) Util.Log.Print(tr, LogCategory.Info);
            }
        }
        public static bool IfMinorFactionHero(Hero hero)
        {
            return hero.IsMinorFactionHero || (hero.IsHumanPlayerCharacter && ((hero?.Clan?.IsUnderMercenaryService ?? false) || hero?.Clan?.Kingdom == null));
        }
        /// <summary>
        /// This function can be very heavy, so should be called only at the itinialization
        /// </summary>
        public void UpdateAllHeroSuffixNumber(SuffixNumberFormat suffixNumberFormat)
        {
            if (suffixNumberFormat == SuffixNumberFormat.None) return;
            Util.Log.Print("UpdateAllHeroSuffix is called", LogCategory.Info);
            foreach (Clan clan in Clan.All)
            {
                Dictionary<string, int> nameCounter = new();
                IEnumerable<Hero> heroes = clan.Heroes.OrderBy(x => x.BirthDay);
                foreach (Hero hero in heroes)
                {
                    if (nameCounter.TryGetValue(hero.FirstName.ToString(), out int value))
                    {
                        int genNum = value + 1;
                        if (this.HeroProfiles.TryGetValue(hero, out HeroProfile hp))
                        {
                            hp.GenSuffixNum = genNum;
                            nameCounter[hero.FirstName.ToString()]++;
                            hp.UpdateGunSuffixText();
                            this.HeroProfiles[hero] = hp;
                        }
                        else
                        {
                            // TODO when this happens?
                            Util.Log.Print($"Hero profile not found when calculating the suffix number. clan={clan.StringId}, id={hero.StringId}", LogCategory.Warning);
                        }
                    }
                    else
                    {
                        nameCounter.Add(hero.FirstName.ToString(), 1);
                        if (this.HeroProfiles.TryGetValue(hero, out HeroProfile hp))
                        {
                            hp.GenSuffixNum = 1;
                            if (suffixNumberFormat == SuffixNumberFormat.All) hp.UpdateGunSuffixText();
                            this.HeroProfiles[hero] = hp;
                        }
                        else
                        {
                            // TODO when this happens?
                            Util.Log.Print($"Hero profile not found when calculating the suffix number. clan={clan.StringId}, id={hero.StringId}", LogCategory.Warning);
                        }
                    }
                }
            }
        }
        public void UpdateGenerationInfo(Hero hero)
        {
            if (hero.FirstName == null) return;
            int maxNum = this.HeroProfiles.Keys.Where(x => x?.Clan == hero.Clan && x.FirstName.ToString() == hero.FirstName.ToString()).Where(x => this.HeroProfiles.ContainsKey(hero)).Select(x => this.HeroProfiles[x].GenSuffixNum).ToList().Max();
            SuffixNumberFormat suffixFormat = MCMRuntimeSettings.Instance?.Options?.SuffixNumFormat ?? SuffixNumberFormat.None;
            if (this.HeroProfiles.TryGetValue(hero, out HeroProfile hp))
            {
                hp.GenSuffixNum = maxNum + 1;
                if (maxNum <= HeroProfile.MaxGenNum && suffixFormat == SuffixNumberFormat.All || maxNum >= 2)
                {
                    hp.UpdateGunSuffixText();
                    this.HeroProfiles[hero] = hp;
                }
            }
        }
        public void UpdateTitlerankInHeroProfiles(Hero hero, TitleRank titleRank)
        {
            if (this.HeroProfiles.TryGetValue(hero, out HeroProfile hp))
            {
                hp.TitleRank = titleRank;
                this.HeroProfiles[hero] = hp;
            }
            else
            {
                HeroProfile hpNew = new(titleRank);
                this.HeroProfiles.Add(hero, hpNew);
            }
        }
        public void UpdateSuffNumInHeroProfiles(Hero hero, int suffNum)
        {
            if (this.HeroProfiles.TryGetValue(hero, out HeroProfile hp))
            {
                hp.GenSuffixNum = suffNum;
                hp.UpdateGunSuffixText();
                this.HeroProfiles[hero] = hp;
            }
            else
            {
                HeroProfile hpNew = new(TitleRank.None);
                hpNew.UpdateGunSuffixText();
                this.HeroProfiles.Add(hero, hpNew);
            }
        }
        /// <summary>
        /// to inspect Bannerlord clan attribute in case disruptive updates
        /// </summary>
        /// <param name="clan"></param>
        private void ShowClanDebugValues(Clan clan)
        {
            Util.Log.Print($"clan={clan.Name}, kingdom={clan?.Kingdom?.StringId}, rulingClan={clan?.Kingdom?.RulingClan.StringId}, isMinorFaction={clan.IsMinorFaction}, typeMerc={clan.IsClanTypeMercenary}, MercService={clan.IsUnderMercenaryService}, leader={clan?.Leader?.StringId}, isleaderalive={clan?.Leader?.IsAlive}, leaderIsLord={clan?.Leader?.IsLord}");
        }
        private int GetFiefScore(Clan clan) => clan.Fiefs.Sum(t => t.IsTown ? 3 : 1);
        private string GetHeroTrace(Hero h, TitleRank rank) =>
            $" -> {rank}: {h.Name} [Fief Score: {(rank == TitleRank.King || rank == TitleRank.Duke || rank == TitleRank.Count || rank == TitleRank.Baron ? this.GetFiefScore(h.Clan) : 0)} / Renown: {h.Clan.Renown:F0}]";
    }

    public class HeroProfile
    {
        public TitleRank TitleRank { get; set; }
        public int GenSuffixNum { get; set; } = 1;
        public TextObject GenSuffixText { get; private set; } = new("");
        public HeroProfile(TitleRank titleRank, int? genSuffixNum = null)
        {
            this.TitleRank = titleRank;
            this.GenSuffixNum = genSuffixNum ?? 1;
        }
        public void UpdateGunSuffixText()
        {
            if (MCMRuntimeSettings.Instance.Options.SuffixNumFormat == SuffixNumberFormat.None) return;
            if (this.GenSuffixNum <= 0 || 20 <= this.GenSuffixNum) return;
            if (2 <= this.GenSuffixNum || MCMRuntimeSettings.Instance.Options.SuffixNumFormat == SuffixNumberFormat.All)
            {
                this.GenSuffixText = GameTexts.FindText("ntp_suffix_num", this.GenSuffixNum.ToString());
            }
        }
        public const int MaxGenNum = 20;
    }
}
