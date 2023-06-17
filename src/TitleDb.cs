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
using TaleWorlds.SaveSystem;

namespace NobleTitlesPlus
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
                // ?? throw new BadTitleDatabaseException("Failed to deserialize title database!");
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
            foreach (KeyValuePair<string, FactionTitleSet> i in this.titleSet.cultures)
            {
                (string cul, FactionTitleSet entry) = (i.Key, i.Value);
                if (cul == "default")
                {
                    defaultCulture = entry;
                }
            }
            foreach (KeyValuePair<string, FactionTitleSet> i in this.titleSet.minorFactions)
            {
                (string cul, FactionTitleSet entry) = (i.Key, i.Value);
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

        private static FactionTitleSet defaultCulture = new(
                new("{=NobleTitlePlus.defaultKing}King {NAME}", "{=NobleTitlePlus.defaultQueen}Queen {NAME}"),
                new("{=NobleTitlePlus.defaultDuke}Duke {NAME}", "{=NobleTitlePlus.defaultDuchess}Duchess {NAME}"),
                new("{=NobleTitlePlus.defaultCount}Count {NAME}", "{=NobleTitlePlus.defaultCountess}Countess {NAME}"),
                new("{=NobleTitlePlus.defaultBaron}Baron {NAME}", "{=NobleTitlePlus.defaultBaroness}Baroness {NAME}"),
                new("{=NobleTitlePlus.defaultSir}{NAME}", "{=NobleTitlePlus.defaultDame}{NAME}")
            );
        private static FactionTitleSet defaultMinorFaction = new(
                new("{=NobleTitlePlus.defaultMinorLeader}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorLeader.Female}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}")
            );
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public class TitleSet
        {
            [JsonProperty("CULTURES")]
            public readonly Dictionary<string, FactionTitleSet> cultures = new() { { "default", defaultCulture } };
            [JsonProperty("MINORS")]
            public readonly Dictionary<string, FactionTitleSet> minorFactions = new() { { "default", defaultMinorFaction } };
            [JsonProperty("KINGDOMS")]
            public readonly Dictionary<string, FactionTitleSet> kingdoms = new();
        }
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public class FactionTitleSet
        {
            [JsonProperty]
            public readonly GenderTitlePair king;
            [JsonProperty]
            public readonly GenderTitlePair duke;
            [JsonProperty]
            public readonly GenderTitlePair count;
            [JsonProperty]
            public readonly GenderTitlePair baron;
            [JsonProperty]
            public readonly GenderTitlePair noble;

            public FactionTitleSet(GenderTitlePair king, GenderTitlePair duke, GenderTitlePair count, GenderTitlePair baron, GenderTitlePair noble)
            {
                this.king = king;
                this.duke = duke;
                this.count = count;
                this.baron = baron;
                this.noble = noble;
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
                    _ => new TextObject("{NAME}"),
                };
            }
        }
        [JsonObject("Entry", MemberSerialization = MemberSerialization.OptIn)]
        public class GenderTitlePair
        {
            [JsonProperty("Male")]
            private readonly string male = "";
            [JsonProperty("FeMale")]
            private readonly string female = "";
            public TextObject maleFormat;
            public TextObject femaleFormat;
            public TextObject GetTitle(bool isFemale)
            {
                return isFemale ? femaleFormat : maleFormat;
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
            private string RmEndChar(string s) => s.Substring(0, s.Length - 1);
        }
        internal TextObject GetTitle(Hero hero, TitleRank rank)
        {
            bool isMinorFaction = hero?.IsMinorFactionHero ?? true;
            string factionId = isMinorFaction ? (hero?.Clan?.StringId ?? "default_minor") : (hero?.Clan?.Kingdom?.Culture?.StringId ?? "");
            string kingdomId = hero.Clan?.Kingdom?.Name.ToString() ?? "";
            return this.GetTitle(hero.IsFemale, factionId, kingdomId, rank, isMinorFaction ? Category.MinorFaction : Category.Default);
        }
        internal TextObject GetTitle(bool isFemale, string factionId, string kingdomId, TitleRank rank, Category category)
        {
            if (category == Category.Default)
            {
                if(this.titleSet.cultures.TryGetValue(kingdomId, out FactionTitleSet kingdomTitles))
                {
                    return kingdomTitles.GetTitle(isFemale, rank);
                }
                else if (this.titleSet.cultures.TryGetValue(factionId, out FactionTitleSet cultureTitles))
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
                if(this.titleSet.minorFactions.TryGetValue(factionId, out FactionTitleSet minorFactionTitles))
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
