using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
// using NobleTitlesPlus.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.ClassLoadout;
using TaleWorlds.SaveSystem;

namespace NobleTitlesPlus.DB
{
    class TitleDb
    {
        internal TitleDb()
        {
            // Util.Log.Print($"MCM test = {NTPSettings.Instance.DefaultNoblewoman}");
            // TODO: avoid abuse of execption. every title need to have default value.
            this.settings = JsonConvert.DeserializeObject<TitleGlobalSettingsJson>(
                File.ReadAllText($"{BasePath.Name}/Modules/{SubModule.modFolderName}/settings.json"),
                new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace }) ?? new TitleGlobalSettingsJson();
            this.PathTitles = $"{BasePath.Name}/Modules/{SubModule.modFolderName}/titles.json";
            this.titleSet = JsonConvert.DeserializeObject<TitleSet>(
                File.ReadAllText(this.PathTitles),
                new JsonSerializerSettings
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                }) ?? new TitleSet();
            Util.Log.Print($"{this.titleSet.cultures.Count} set of titles for culutures");
            Util.Log.Print($"{this.titleSet.kingdoms.Count} set of titles for kingdoms");
            Util.Log.Print($"{this.titleSet.kingdoms.Count} set of titles for minor factions");

            if (this.titleSet.cultures.Count == 0)
            {
                Util.Log.Print($">> WARNING: Title database is empty. The built-in default setting will be applied.");
                // this.titleSet.cultures = defaultCulture;
                // throw new BadTitleDatabaseException("Title database is empty!");
            }
            // Must have a fallback culture entry.
            if (!this.titleSet.cultures.ContainsKey("default"))
            {
                Util.Log.Print($">> WARNING: Title database doesn't contain a fallback culture entry keyed by \"default\". The built-in default setting will be applied.");
                // this.cultureMap["default"] = defaultCulture;
                // throw new BadTitleDatabaseException("Title database must contain a fallback culture entry keyed by \"default\"!");
            }
            foreach (KeyValuePair<string, TitleSet.FactionTitleSet> i in this.titleSet.cultures)
            {
                (string cul, TitleSet.FactionTitleSet entry) = (i.Key, i.Value);
                if (cul == "default")
                {
                    defaultCulture = entry;
                }
            }
            foreach (KeyValuePair<string, TitleSet.FactionTitleSet> i in this.titleSet.minorFactions)
            {
                (string cul, TitleSet.FactionTitleSet entry) = (i.Key, i.Value);
                if (cul == "default_minor")
                {
                    defaultMinorFaction = entry;
                }
            }
        }
        public class BadTitleDatabaseException : Exception
        {
            public BadTitleDatabaseException(string message) : base(message) { }
            public BadTitleDatabaseException() { }
            public BadTitleDatabaseException(string message, Exception innerException) : base(message, innerException) { }
        }
        protected string PathTitles { get; set; }
        public TitleGlobalSettingsJson settings;
        // culture StringId => CultureEntry (contains bulk of title information, only further split by gender)
        protected TitleSet titleSet;

        public static TitleSet.FactionTitleSet defaultCulture = TitleSet.defaultCultureValue;
        public static TitleSet.FactionTitleSet defaultMinorFaction = new(
                new("{=NobleTitlePlus.defaultMinorLeader}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorLeader.Female}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}")
            );
        internal TextObject GetTitle(Hero hero, TitleRank rank)
        {
            bool isMinorFaction = hero.IsMinorFactionHero;
            string factionId = isMinorFaction ? (hero?.Clan?.StringId ?? "default_minor") : (hero?.Clan?.Kingdom?.Culture?.StringId ?? "");
            string kingdomId = hero?.Clan?.Kingdom?.Name.ToString() ?? "";
            return this.GetTitle(hero.IsFemale, factionId, kingdomId, rank, isMinorFaction ? Category.MinorFaction : Category.Default);
        }
        internal TextObject GetTitle(bool isFemale, string factionId, string kingdomId, TitleRank rank, Category category)
        {
            if (category == Category.Default)
            {
                if(this.titleSet.kingdoms.TryGetValue(kingdomId, out TitleSet.FactionTitleSet kingdomTitles))
                {
                    return kingdomTitles.GetTitle(isFemale, rank);
                }
                else if (this.titleSet.cultures.TryGetValue(factionId, out TitleSet.FactionTitleSet cultureTitles))
                {
                    return cultureTitles.GetTitle(isFemale, rank);
                }
                else
                {
                    return defaultCulture.GetTitle(isFemale, rank);
                }
            }
            else if (category == Category.MinorFaction)
            {
                if(this.titleSet.minorFactions.TryGetValue(factionId, out TitleSet.FactionTitleSet minorFactionTitles))
                {
                    return minorFactionTitles.GetTitle(isFemale, rank);
                }
                else
                {
                    return defaultMinorFaction.GetTitle(isFemale, rank);
                }
            }
            else
            {
                return new TextObject("{NAME}");
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class TitleSet
    {
        [JsonProperty("CULTURES")]
        public Dictionary<string, FactionTitleSet> cultures = new() { { "default", defaultCultureValue } };
        [JsonProperty("MINORS")]
        public Dictionary<string, FactionTitleSet> minorFactions = new() { { "default", defaultMinorFactionValue } };
        [JsonProperty("KINGDOMS")]
        public Dictionary<string, FactionTitleSet> kingdoms = new() { { "default", defaultCultureValue } };
        public static readonly FactionTitleSet defaultCultureValue = new(
                new("{=NobleTitlePlus.defaultKing}King {NAME}", "{=NobleTitlePlus.defaultQueen}Queen {NAME}"),
                new("{=NobleTitlePlus.defaultDuke}Duke {NAME}", "{=NobleTitlePlus.defaultDuchess}Duchess {NAME}"),
                new("{=NobleTitlePlus.defaultCount}Count {NAME}", "{=NobleTitlePlus.defaultCountess}Countess {NAME}"),
                new("{=NobleTitlePlus.defaultBaron}Baron {NAME}", "{=NobleTitlePlus.defaultBaroness}Baroness {NAME}"),
                new("{=NobleTitlePlus.defaultSir}{NAME}", "{=NobleTitlePlus.defaultDame}{NAME}")
            );
        public static readonly FactionTitleSet defaultMinorFactionValue = new(
                new("{=NobleTitlePlus.defaultMinorLeader}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorLeader.Female}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}")
            );
        internal TextObject GetTitle(Hero hero, TitleRank rank)
        {
            bool isMinorFaction = hero.IsMinorFactionHero;
            string factionId = isMinorFaction ? (hero?.Clan?.StringId ?? "default_minor") : (hero?.Clan?.Kingdom?.Culture?.StringId ?? "");
            string kingdomId = hero?.Clan?.Kingdom?.Name.ToString() ?? "";
            return this.GetTitle(hero.IsFemale, factionId, kingdomId, rank, isMinorFaction ? Category.MinorFaction : Category.Default);
        }
        internal TextObject GetTitle(bool isFemale, string factionId, string kingdomId, TitleRank rank, Category category)
        {
            if (category == Category.Default)
            {
                if (this.kingdoms.TryGetValue(kingdomId, out FactionTitleSet kingdomTitles))
                {
                    return kingdomTitles.GetTitle(isFemale, rank);
                }
                else if (this.cultures.TryGetValue(factionId, out FactionTitleSet cultureTitles))
                {
                    return cultureTitles.GetTitle(isFemale, rank);
                }
                else
                {
                    return defaultCultureValue.GetTitle(isFemale, rank);
                }
            }
            else if (category == Category.MinorFaction)
            {
                if (this.minorFactions.TryGetValue(factionId, out FactionTitleSet minorFactionTitles))
                {
                    return minorFactionTitles.GetTitle(isFemale, rank);
                }
                else
                {
                    return defaultMinorFactionValue.GetTitle(isFemale, rank);
                }
            }
            else
            {
                return new TextObject("{NAME}");
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

            public FactionTitleSet(GenderTitlePair king, GenderTitlePair duke, GenderTitlePair count, GenderTitlePair baron, GenderTitlePair noble)
            {
                this.king = king;
                this.duke = duke;
                this.count = count;
                this.baron = baron;
                this.noble = noble;
            }
            public TextObject GetTitle(bool isFemale, TitleRank rank, bool returnBlankfMisssing = false)
            {
                return rank switch
                {
                    TitleRank.King => this.king.GetTitle(isFemale),
                    TitleRank.Duke => this.duke.GetTitle(isFemale),
                    TitleRank.Count => this.count.GetTitle(isFemale),
                    TitleRank.Baron => this.baron.GetTitle(isFemale),
                    TitleRank.Noble => this.noble.GetTitle(isFemale),
                    _ => returnBlankfMisssing ? new TextObject("") : new TextObject("{NAME}"),
                };
            }
            public void SetTitle(bool isFemale, TitleRank rank, TextObject titleFormat)
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
                    return isFemale ? femaleFormat : maleFormat;
                }
                public void SetTitle(bool isFemale, TextObject titleFormat)
                {
                    if (isFemale) this.maleFormat = titleFormat;
                    else this.femaleFormat = titleFormat;
                }
                public GenderTitlePair(string? male = null, string? female = null)
                {
                    this.male = male ?? this.male;
                    this.female = female ?? this.female;
                    this.maleFormat = new TextObject(this.NormalizeInputTitle(this.male));
                    this.femaleFormat = new TextObject(this.NormalizeInputTitle(this.female));
                }
                private string NormalizeInputTitle(string titleFormat)
                {
                    if (string.IsNullOrWhiteSpace(titleFormat))
                    {
                        Util.Log.Print($">> WARNING: Title format is missing!");
                        titleFormat = "{name}";
                    }
                    string normalized = Regex.Replace(titleFormat, @"\{[a-zA-Z]+\}", t => t.ToString().ToUpper());
                    try
                    {
                        new TextObject(normalized, new Dictionary<string, object>() { ["NAME"] = "TEST NAME" }).ToString();
                    }
                    catch (Exception)
                    {
                        Util.Log.Print($">> WARNING: Title format {titleFormat} is invalid. It's a incorrect format! This format is inavailable.");
                        normalized = "{NAME}";
                    }
                    if (!normalized.Contains("{NAME}"))
                    {
                        Util.Log.Print($">> WARNING: Title format {titleFormat} doesn't contain the name variable! This format is inavailable.");
                        normalized = "{NAME}";
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
        Noble
    }
    public enum Category
    {
        Default,
        MinorFaction,
        Citizen
    }
}
