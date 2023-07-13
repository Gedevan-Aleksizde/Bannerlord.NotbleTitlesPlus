using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using System.Linq;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements;

namespace NobleTitlesPlus.DB
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class TitleSet
    {
        [JsonProperty("CULTURES")]
        public Dictionary<string, FactionTitleSet> cultures = new() { { "default", DefaultCultureValue } };
        [JsonProperty("MINORS")]
        public Dictionary<string, FactionTitleSet> minorFactions = new() { { "default", DefaultMinorFactionValue } };
        [JsonProperty("FACTIONS")]
        public Dictionary<string, FactionTitleSet> factions = new();
        public static FactionTitleSet DefaultCultureValue => new(
                new(
                    Util.QuoteMultVarBitEasiler(new("{=NTP.DEFRank1M_default}King {NAME}")),
                    Util.QuoteMultVarBitEasiler(new("{=NTP.DEFRanl1F_default}Queen {NAME}"))
                    ),
                new(
                    Util.QuoteMultVarBitEasiler(new("{=NTP.DEFRank2M_default}Duke {NAME}")),
                    Util.QuoteMultVarBitEasiler(new("{=NTP.DEFRank2F_default}Duchess {NAME}"))
                    ),
                new(
                    Util.QuoteMultVarBitEasiler(new("{=NTP.DEFRank3M_default}Count {NAME}")),
                    Util.QuoteMultVarBitEasiler(new("{=NTP.DEFRank3F_default}Countess {NAME}"))
                    ),
                new(Util.QuoteMultVarBitEasiler(new("{=NTP.DEFRank4M_default}Baron {NAME}")),
                    Util.QuoteMultVarBitEasiler(new("{=NTP.DEFRank4F_default}Baroness {NAME}"))
                    ),
                new(Util.QuoteMultVarBitEasiler(new("{=NTP.DEFRank5M_default}{NAME}")),
                    Util.QuoteMultVarBitEasiler(new("{=NTP.DEFRank5F_default}{NAME}"))
                    ),
                new(Util.QuoteMultVarBitEasiler(new("{=NTP.DEFCrownM_default}{NAME}")),
                    Util.QuoteMultVarBitEasiler(new("{=NTP.DEFCrownF_default}{NAME}"))
                    )
            );
        public static FactionTitleSet DefaultMinorFactionValue => new(
            new(
                Util.QuoteMultVarBitEasiler(new("{=NTP.DEFMinorLM_default}{NAME} of {CLAN}")),
                Util.QuoteMultVarBitEasiler(new("{=NTP.DEFMinorLF_default}{NAME} of {CLAN}"))
            ),
            new(
                Util.QuoteMultVarBitEasiler(new("{=NTP.DEFMinorMM_default}{NAME} of {CLAN}")),
                Util.QuoteMultVarBitEasiler(new("{=NTP.DEFMinorMF_default}{NAME} of {CLAN}"))
            ),
            new(
                Util.QuoteMultVarBitEasiler(new("{=NTP.DEFMinorMM_default}{NAME} of {CLAN}")),
                Util.QuoteMultVarBitEasiler(new("{=NTP.DEFMinorMF_default}{NAME} of {CLAN}"))
            ),
            new(
                Util.QuoteMultVarBitEasiler(new("{=NTP.DEFMinorMM_default}{NAME} of {CLAN}")),
                Util.QuoteMultVarBitEasiler(new("{=NTP.DEFMinorMF_default}{NAME} of {CLAN}"))
            ),
            new(
                Util.QuoteMultVarBitEasiler(new("{=NTP.DEFMinorMM_default}{NAME} of {CLAN}")),
                Util.QuoteMultVarBitEasiler(new("{=NTP.DEFMinorMF_default}{NAME} of {CLAN}"))
            )
        );
        public static FactionTitleSet BlankTitleSet => new(new("", ""), new("", ""), new("", ""), new("", ""));
        public TitleSet()
        {
            this.InitCultureTitles(AssignMode.Assign);
            this.InitFactionTitles(AssignMode.Blank);
            this.InitMinorFactionTitles(AssignMode.Assign);
            // this.TmpDebug();
        }
        public void InitCultureTitles(AssignMode mode)
        {
            List<string> cultureIds = Kingdom.All.Select(k => k.Culture.StringId).Distinct().ToList();
            foreach (string cultureId in cultureIds)
            {
                if(!this.cultures.ContainsKey(cultureId)) this.cultures.Add(cultureId, BlankTitleSet);
            }
            if (mode == AssignMode.None) return;
            else if (mode == AssignMode.Assign)
            {
                foreach(string cultureId in cultureIds)
                {
                    foreach (bool isFemale in new bool[] { false, true })
                    {
                        foreach (TitleRank rank in Enum.GetValues(typeof(TitleRank)))
                        {
                            this.SetDefaultCultureTitle(cultureId, isFemale, rank);
                        }
                    }
                }
            }
            else if(mode == AssignMode.Blank)
            {
                foreach (string cultureId in cultureIds)
                {
                    this.cultures.TryGetValue(cultureId, out FactionTitleSet fts);
                    foreach (bool isFemale in new bool[] { false, true })
                    {

                        foreach (TitleRank rank in Enum.GetValues(typeof(TitleRank)))
                        {
                            fts.SetTitle(isFemale, rank, "");
                        }
                    }
                }
            }
        }
        public void InitFactionTitles(AssignMode mode)
        {
            List<string> cultureIds = Kingdom.All.Select(k => k.Culture.StringId).Distinct().ToList();
            List<Kingdom> kingdoms = Kingdom.All.Where(k => !cultureIds.Contains(k.StringId)).ToList();
            foreach (Kingdom k in kingdoms)
            {
                if(!this.factions.ContainsKey(k.StringId)) this.factions.Add(k.StringId, BlankTitleSet);
            }
            if (mode == AssignMode.None) return;
            else if (mode == AssignMode.Assign)
            {
                foreach (Kingdom k in kingdoms)
                {
                    foreach (bool isFemale in new bool[] { false, true })
                    {
                        foreach (TitleRank rank in Enum.GetValues(typeof(TitleRank)))
                        {
                            this.SetFactionTitle(this.GetTitleRaw(isFemale, k.Culture.StringId, null, rank, Category.Default), k.StringId, isFemale, rank);
                        }
                    }
                }
            }
            else if(mode == AssignMode.Blank)
            {
                foreach (Kingdom k in kingdoms)
                {
                    this.factions.TryGetValue(k.StringId, out FactionTitleSet fts);
                    foreach (bool isFemale in new bool[] { false, true })
                    {
                        foreach (TitleRank rank in Enum.GetValues(typeof(TitleRank)))
                        {
                            fts.SetTitle(isFemale, rank, "");
                        }
                    }
                }
            }
        }
        public void InitMinorFactionTitles(AssignMode mode)
        {
            if(mode == AssignMode.None || mode == AssignMode.Assign)
            {
                foreach (Clan c in Clan.All.Where(c => c.IsMinorFaction && !c.Leader.IsHumanPlayerCharacter))
                {
                    this.minorFactions.Add(c.StringId, DefaultMinorFactionValue);
                }
            }
            else if(mode == AssignMode.Blank)
            {
                // perhaps never used
                foreach (Clan c in Clan.All.Where(c => c.IsMinorFaction && !c.Leader.IsHumanPlayerCharacter))
                {
                    this.minorFactions.Add(c.StringId, BlankTitleSet);
                }
            }
        }
        public void AddDefaultCutlureTitles()
        {
            foreach(string cultureId in /*new string[] { "aserai", "battania", "empire", "khuzait", "sturgia", "vlandia" }*/ Kingdom.All.Select(k => k.Culture.StringId).Distinct())
            {
                if (!this.cultures.ContainsKey(cultureId))
                {
                    TextObject ruler = GameTexts.FindText("str_faction_ruler_name_with_title", cultureId);
                    TextObject noble = GameTexts.FindText("str_faction_noble_name_with_title", cultureId);
                    TextObject whyneedM = new("", new Dictionary<string, object>() { { "GENDER", 0}, { "NAME", "______MOCKPLACEHOLDER_____" } });
                    TextObject whyneedF = new("", new Dictionary<string, object>() { { "GENDER", 1 }, { "NAME", "______MOCKPLACEHOLDER_____" } });
                    this.cultures.Add(
                        cultureId,
                        new FactionTitleSet(
                            new(Util.QuoteVarBitEasiler(ruler.SetTextVariable("RULER", whyneedM)), Util.QuoteVarBitEasiler(ruler.SetTextVariable("RULER", whyneedF))),
                            new(Util.QuoteVarBitEasiler(noble.SetTextVariable("RULER", whyneedM)), Util.QuoteVarBitEasiler(noble.SetTextVariable("RULER", whyneedF))),
                            new(Util.QuoteVarBitEasiler(noble.SetTextVariable("RULER", whyneedM)), Util.QuoteVarBitEasiler(noble.SetTextVariable("RULER", whyneedF))),
                            new(Util.QuoteVarBitEasiler(noble.SetTextVariable("RULER", whyneedM)), Util.QuoteVarBitEasiler(noble.SetTextVariable("RULER", whyneedF))),
                            new("{NAME}", "{NAME}"),
                            new("{NAME}", "{NAME}") // ugly code...
                            )
                        );
                }
            }
        }
        public void SetDefaultCultureTitle(string cultureId, bool isFemale, TitleRank rank)
        {
            TextObject title = GameTexts.FindText(rank == TitleRank.King ? "str_faction_ruler_name_with_title": "str_faction_noble_name_with_title", cultureId);
            TextObject dummy = new("", new Dictionary<string, object>() { { "GENDER", isFemale? 1: 0 }, { "NAME", "______MOCKPLACEHOLDER_____" } });
            this.SetCultureTitle(Util.QuoteVarBitEasiler(title.SetTextVariable("RULER", dummy)), cultureId, isFemale, rank);
        }
        public void AddAllMinorFactionTitles()
        {
            foreach(Clan c in Clan.All.Where(c => c.IsMinorFaction && !c.Leader.IsHumanPlayerCharacter))
            {
                this.minorFactions.Add(c.StringId, DefaultMinorFactionValue);
                if (SubModule.Options.VerboseLog)  Util.Log.Print($">> [INFO] Intialized title set  for minor faction {c.StringId}");
            }
        }
        public void TmpDebug()
        {
            foreach(string keys in this.cultures.Keys)
            {
                Util.Log.Print($"Calture key: {keys}");
                for(int i = 1; i <= 5; i++)
                {
                    Util.Log.Print($"King = ({this.cultures[keys].GetTitleRaw(true, (TitleRank)i )}, {this.cultures[keys].GetTitleRaw(false, (TitleRank)i)})");
                }
            }
            foreach (string keys in this.factions.Keys)
            {
                Util.Log.Print($"Faction key: {keys}");
                for (int i = 1; i <= 5; i++)
                {
                    Util.Log.Print($"King = ({this.factions[keys].GetTitleRaw(true, (TitleRank)i)}, {this.factions[keys].GetTitleRaw(false, (TitleRank)i)})");
                }
            }
            foreach (string keys in this.minorFactions.Keys)
            {
                Util.Log.Print($"Minor Factions key: {keys}");
                Util.Log.Print($"Leader = ({this.minorFactions[keys].GetTitleRaw(true, TitleRank.King)}, {this.minorFactions[keys].GetTitleRaw(false, TitleRank.King)})");
                Util.Log.Print($"Member = ({this.minorFactions[keys].GetTitleRaw(true, TitleRank.Noble)}, {this.minorFactions[keys].GetTitleRaw(false, TitleRank.Noble)})");
            }
        }
        
        internal void SetFactionTitle(string newTitle, string id, bool isFemale, TitleRank rank, bool append = false)
        {
            if (this.factions.ContainsKey(id))
            {
                this.factions[id].SetTitle(isFemale, rank, newTitle);
            }
            else if (append)
            {
                Util.Log.Print($"[WARNING] Faction ID {id} not found! Now new culture entry added.");
                this.factions.Add(id, BlankTitleSet);
                this.factions[id].SetTitle(isFemale, rank, newTitle);
            }
            else
            {
                Util.Log.Print($"[WARNING] Renaming the format failed! No faction entries asscociated with {id}!");
            }
        }
        internal string GetMinorTitleRaw(string clanId, bool isFemale, TitleRank rank, string defaultFormat = "")
        {
            if (rank == TitleRank.King || rank == TitleRank.Noble)
            {
                if (this.minorFactions.TryGetValue(clanId, out FactionTitleSet titleSet))
                {
                    return titleSet.GetTitleRaw(isFemale, rank);
                }
                else
                {
                    this.minorFactions.Add(clanId, DefaultMinorFactionValue);
                    this.SetMinorFactionTitle(clanId, isFemale, rank, defaultFormat);
                    return this.minorFactions[clanId].GetTitleRaw(isFemale, rank);
                }
            }
            else
            {
                Util.Log.Print($"WARNING: irregular minor faction rank requested! ({rank})");
                return defaultFormat;
            }
        }
        internal string GetTitleRaw(bool isFemale, string cultureId, string? factionId, TitleRank rank, Category category)
        {
            if (category == Category.Default)
            {
                if (this.factions.TryGetValue(factionId ?? "", out FactionTitleSet factionTitles))
                {
                    return factionTitles.GetTitleRaw(isFemale, rank);
                }
                else if (this.cultures.TryGetValue(cultureId, out FactionTitleSet cultureTitles))
                {
                    return cultureTitles.GetTitleRaw(isFemale, rank);
                }
                else
                {
                    return DefaultCultureValue.GetTitleRaw(isFemale, rank);
                }
            }
            else if (category == Category.MinorFaction)
            {
                if (this.minorFactions.TryGetValue(factionId, out FactionTitleSet minorFactionTitles))
                {
                    return minorFactionTitles.GetTitleRaw(isFemale, rank);
                }
                else
                {
                    return DefaultMinorFactionValue.GetTitleRaw(isFemale, rank);
                }
            }
            else
            {
                Util.Log.Print($">> [WARNING] WRONG CATEGORY: {category}");
                return "";
            }
        }
        internal TextObject GetTitle(Hero hero, TitleRank rank)
        {
            bool isMinorFaction = hero.IsMinorFactionHero && hero.Clan.StringId != "player_faction";
            string cultureId = isMinorFaction ? (hero?.Clan?.StringId ?? "default") : (hero?.Clan?.Kingdom?.Culture?.StringId ?? "");
            string factionId = hero?.Clan?.Kingdom?.StringId ?? "";
            return this.GetTitle(hero.IsFemale, cultureId, factionId, rank, isMinorFaction ? Category.MinorFaction : Category.Default);
        }
        internal TextObject GetTitle(bool isFemale, string cultureId, string? factionId, TitleRank rank, Category category)
        {
            if (category == Category.Default)
            {
                if (this.factions.TryGetValue(factionId ?? "", out FactionTitleSet factionTitles) && factionTitles.GetTitleRaw(isFemale, rank) != "")
                {
                    return factionTitles.GetTitle(isFemale, rank);
                }
                else if (this.cultures.TryGetValue(cultureId, out FactionTitleSet cultureTitles) && cultureTitles.GetTitleRaw(isFemale, rank) != "")
                {
                    return cultureTitles.GetTitle(isFemale, rank);
                }
                else
                {
                    return DefaultCultureValue.GetTitle(isFemale, rank);
                }
            }
            else if (category == Category.MinorFaction)
            {
                if (this.minorFactions.TryGetValue(cultureId, out FactionTitleSet minorFactionTitles) && minorFactionTitles.GetTitleRaw(isFemale, rank) != "")
                {
                    return minorFactionTitles.GetTitle(isFemale, rank);
                }
                else
                {
                    return DefaultMinorFactionValue.GetTitle(isFemale, rank);
                }
            }
            else
            {
                return new TextObject("{NAME}");
            }
        }
        internal string GetTitleId(bool isFemale, string cultureId, string factionId, TitleRank rank, Category category)
        {
            string titleSetId;
            string group = category == Category.Default ? "Kingdom" : "Minor";
            FactionTitleSet v = DefaultCultureValue;
            if (category == Category.Default)
            {
                if (this.factions.TryGetValue(factionId ?? "", out v))
                {
                    titleSetId = factionId;
                }
                else if (this.cultures.TryGetValue(cultureId, out v))
                {
                    titleSetId = cultureId;
                }
                else
                {
                    titleSetId = "default";
                }
            }
            else if (category == Category.MinorFaction)
            {
                if (this.minorFactions.TryGetValue(cultureId, out v))
                {
                    titleSetId = cultureId;
                }
                else
                {
                    titleSetId = "default";
                }
            }
            else
            {
                titleSetId = "default";
            }
            if (v.GetTitleRaw(isFemale, rank) == "") titleSetId = "default";
            return titleSetId;
        }
        internal bool CultureTitleExists(string cultureId, bool isFemale, TitleRank rank)
        {
            if (this.cultures.TryGetValue(cultureId, out FactionTitleSet titleSet))
            {
                if (titleSet.GetTitleRaw(isFemale, rank) == "")
                {
                    return false;
                }
                else return true;
            }
            else return false;
        }
        internal bool FactionTitleExists(string factionId, bool isFemale, TitleRank rank)
        {
            if(this.factions.TryGetValue(factionId, out FactionTitleSet titleSet)){
                if (titleSet.GetTitleRaw(isFemale, rank) == "")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        internal bool MinorFactionTitleExists(string clanId, bool isFemale, TitleRank rank)
        {
            if (this.minorFactions.TryGetValue(clanId, out FactionTitleSet titleSet))
            {
                if(titleSet.GetTitleRaw(isFemale, rank) == "")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        internal void SetCultureTitle(string newTitle, string id, bool isFemale, TitleRank rank, bool append = false)
        {
            if (this.cultures.ContainsKey(id))
            {
                this.cultures[id].SetTitle(isFemale, rank, newTitle);
            }
            else if (append)
            {
                Util.Log.Print($"[WARNING] Culture ID {id} not found! Now new culture entry added.");
                this.cultures.Add(id, DefaultCultureValue);
                this.cultures[id].SetTitle(isFemale, rank, newTitle);
            }
            else
            {
                Util.Log.Print($"[WARNING] Renaming the format failed! No culture entries asscociated with {id}!");
            }
        }
        internal void SetMinorFactionTitle(string clanId, bool isFemale, TitleRank rank, string newTitle, bool append = false)
        {
            if (rank == TitleRank.King || rank == TitleRank.Noble)
            {
                if (this.minorFactions.ContainsKey(clanId))
                {
                    this.minorFactions[clanId].SetTitle(isFemale, rank, newTitle);
                }
                else if (append)
                {
                    this.minorFactions.Add(clanId, DefaultMinorFactionValue);
                    this.minorFactions[clanId].SetTitle(isFemale, rank, newTitle);
                }
                else
                {
                    Util.Log.Print($">> [WARNING] No minor faction entries asscociated with {clanId}!");
                }
            }
            else
            {
                Util.Log.Print($">> [WARNING] Irregular minor faction rank requested! ({rank})");
            }
        }
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public class FactionTitleSet
        {
            [JsonProperty]
            private GenderTitlePair king;
            [JsonProperty]
            private GenderTitlePair duke;
            [JsonProperty]
            private GenderTitlePair count;
            [JsonProperty]
            private GenderTitlePair baron;
            [JsonProperty]
            private GenderTitlePair noble;
            [JsonProperty]
            private GenderTitlePair prince;

            public FactionTitleSet(GenderTitlePair king, GenderTitlePair duke, GenderTitlePair count, GenderTitlePair baron, GenderTitlePair? noble = null, GenderTitlePair? prince = null)
            {
                this.king = king;
                this.duke = duke;
                this.count = count;
                this.baron = baron;
                this.prince = prince?? new("{NAME}", "{NAME}");
                this.noble = noble ?? new("{NAME}", "{NAME}");
            }
            public FactionTitleSet(FactionTitleSet factionTitleSet)
            {
                this.king = new(factionTitleSet.king);
                this.duke = new(factionTitleSet.duke);
                this.count = new(factionTitleSet.count);
                this.baron = new(factionTitleSet.baron);
                this.noble = new(factionTitleSet.noble);
                this.prince = new(factionTitleSet.prince);
            }
            public TextObject GetTitle(bool isFemale, TitleRank rank)
            {
                return rank switch
                {
                    TitleRank.King => this.king.GetTitle(isFemale),
                    TitleRank.Duke => this.duke.GetTitle(isFemale),
                    TitleRank.Count => this.count.GetTitle(isFemale),
                    TitleRank.Baron => this.baron.GetTitle(isFemale),
                    TitleRank.Noble => this.noble.GetTitle(isFemale),
                    TitleRank.Prince => this.prince.GetTitle(isFemale),
                    _ => new("")
                };
            }
            public string GetTitleRaw(bool isFemale, TitleRank rank)
            {
                return rank switch
                {
                    TitleRank.King => this.king.GetTitleRaw(isFemale),
                    TitleRank.Duke => this.duke.GetTitleRaw(isFemale),
                    TitleRank.Count => this.count.GetTitleRaw(isFemale),
                    TitleRank.Baron => this.baron.GetTitleRaw(isFemale),
                    TitleRank.Noble => this.noble.GetTitleRaw(isFemale),
                    TitleRank.Prince => this.prince.GetTitleRaw(isFemale),
                    _ => ""
                };
            }
            public void SetTitle(bool isFemale, TitleRank rank, string titleFormat)
            {
                switch (rank)
                {
                    case TitleRank.King:
                        this.king.SetTitle(isFemale, titleFormat);
                        break;
                    case TitleRank.Duke:
                        this.duke.SetTitle(isFemale, titleFormat);
                        break;
                    case TitleRank.Count:
                        this.count.SetTitle(isFemale, titleFormat);
                        break;
                    case TitleRank.Baron:
                        this.baron.SetTitle(isFemale, titleFormat);
                        break;
                    case TitleRank.Noble:
                        this.noble.SetTitle(isFemale, titleFormat);
                        break;
                    case TitleRank.Prince:
                        this.prince.SetTitle(isFemale, titleFormat);
                        break;
                }
            }
            [JsonObject("Entry", MemberSerialization = MemberSerialization.OptIn)]
            public class GenderTitlePair
            {
                [JsonProperty("Male")]
                private string male = "";
                [JsonProperty("FeMale")]
                private string female = "";
                private TextObject maleFormat;
                private TextObject femaleFormat;
                public TextObject GetTitle(bool isFemale)
                {
                    return isFemale ? this.femaleFormat : this.maleFormat;
                }
                public string GetTitleRaw(bool isFemale)
                {
                    return isFemale ? this.female : this.male;
                }
                public void SetTitle(bool isFemale, string titleFormat)
                {
                    if (isFemale)
                    {
                        this.female = titleFormat;
                        this.femaleFormat = new(this.female);
                    }
                    else
                    {
                        this.male = titleFormat;
                        this.maleFormat = new(this.male);
                    }
                }
                public GenderTitlePair(string? male = null, string? female = null)
                {
                    this.male = male ?? this.male;
                    this.female = female ?? this.female;
                    this.maleFormat = new(this.NormalizeInputTitle(this.male));
                    this.femaleFormat = new(this.NormalizeInputTitle(this.female));
                }
                public GenderTitlePair(GenderTitlePair genderTitlePair)
                {
                    this.male = genderTitlePair.male;
                    this.maleFormat = new(this.male);
                    this.female= genderTitlePair.female;
                    this.femaleFormat = new(genderTitlePair.female);
                }
                private string NormalizeInputTitle(string titleFormat)
                {
                    string normalized;
                    if (string.IsNullOrWhiteSpace(titleFormat))
                    {
                        if (SubModule.Options?.VerboseLog ?? true) Util.Log.Print($">> [INFO] Title format is blank");
                        normalized = "";
                    }
                    else
                    {
                        normalized = Regex.Replace(titleFormat, @"\{[a-zA-Z]+\}", t => t.ToString().ToUpper());
                        try
                        {
                            new TextObject(normalized, new Dictionary<string, object>() { ["NAME"] = "TEST NAME" }).ToString();
                        }
                        catch (Exception)
                        {
                            Util.Log.Print($">> [WARNING] Title format {titleFormat} is invalid. It's a incorrect format! This format is inavailable.");
                            normalized = "{NAME}";
                        }
                        if (!normalized.Contains("{NAME}"))
                        {
                            Util.Log.Print($">> [WARNING] Title format {titleFormat} doesn't contain the name variable! This format is inavailable.");
                            normalized = "{NAME}";
                        }
                    }
                    return normalized;
                }
            }
        }
    }  
    public enum TitleRank
    {
        None,
        King,
        Duke,
        Count,
        Baron,
        Noble,
        Prince
    }
    public enum Category
    {
        Default,
        MinorFaction,
        Citizen
    }
    public enum Gender
    {
        F,
        M
    }
    public enum AssignMode
    {
        None,
        Assign,
        Blank
    }
    public enum Inheritance
    {
        Disabled,
        Issue,
        Adult,
        Elder
    }
}
