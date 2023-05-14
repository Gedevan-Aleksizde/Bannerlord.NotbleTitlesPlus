using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
// using NobleTitlesPlus.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NobleTitlesPlus
{
    class TitleDb
    {
        internal EntryJson GetKingTitle(CultureObject culture, Category category) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntryJson? culEntry) ? (category == Category.Default? defaultCulture: defaultMinorFaction).King : culEntry.King;
        internal EntryJson GetDukeTitle(CultureObject culture, Category category) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntryJson? culEntry) ? (category == Category.Default ? defaultCulture : defaultMinorFaction).Duke : culEntry.Duke;
        internal EntryJson GetCountTitle(CultureObject culture, Category category) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntryJson? culEntry) ? (category == Category.Default ? defaultCulture : defaultMinorFaction).Count : culEntry.Count;
        internal EntryJson GetBaronTitle(CultureObject culture, Category category) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntryJson? culEntry) ? (category == Category.Default ? defaultCulture : defaultMinorFaction).Baron : culEntry.Baron;
        internal EntryJson GetLesserNobleTitle(CultureObject culture, Category category) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntryJson? culEntry) ? (category == Category.Default ? defaultCulture : defaultMinorFaction).Noble : culEntry.Noble;
        internal TextObject GetTitle(bool isFemale, string titleSetId, TitleRank rank, Category category)
        {
            if (this.cultureMap.TryGetValue(titleSetId, out CultureEntryJson cul))
            {
                return rank switch
                {
                    TitleRank.King => isFemale ? cul.King.FemaleFormat : cul.King.MaleFormat,
                    TitleRank.Duke => isFemale ? cul.Duke.FemaleFormat : cul.Duke.MaleFormat,
                    TitleRank.Count => isFemale ? cul.Count.FemaleFormat : cul.Count.MaleFormat,
                    TitleRank.Baron => isFemale ? cul.Baron.FemaleFormat : cul.Baron.MaleFormat,
                    TitleRank.Noble => isFemale ? cul.Noble.FemaleFormat : cul.Noble.MaleFormat,
                    _ => new TextObject("{NAME}"),
                };
            }
            else if(category == Category.MinorFaction)
            {
                return rank switch
                {
                    TitleRank.King => isFemale ? defaultMinorFaction.King.FemaleFormat : defaultMinorFaction.King.MaleFormat,
                    TitleRank.Duke => isFemale ? defaultMinorFaction.Duke.FemaleFormat : defaultMinorFaction.Duke.MaleFormat,
                    TitleRank.Count => isFemale ? defaultMinorFaction.Count.FemaleFormat : defaultMinorFaction.Count.MaleFormat,
                    TitleRank.Baron => isFemale ? defaultMinorFaction.Baron.FemaleFormat : defaultMinorFaction.Baron.MaleFormat,
                    TitleRank.Noble => isFemale ? defaultMinorFaction.Noble.FemaleFormat : defaultMinorFaction.Noble.MaleFormat,
                    _ => new TextObject("{NAME}"),
                };
            }
            else
            {
                return rank switch
                {
                    TitleRank.King => isFemale ? defaultCulture.King.FemaleFormat : defaultCulture.King.MaleFormat,
                    TitleRank.Duke => isFemale ? defaultCulture.Duke.FemaleFormat : defaultCulture.Duke.MaleFormat,
                    TitleRank.Count => isFemale ? defaultCulture.Count.FemaleFormat : defaultCulture.Count.MaleFormat,
                    TitleRank.Baron => isFemale ? defaultCulture.Baron.FemaleFormat : defaultCulture.Baron.MaleFormat,
                    TitleRank.Noble => isFemale ? defaultCulture.Noble.FemaleFormat : defaultCulture.Noble.MaleFormat,
                    _ => new TextObject("{NAME}"),
                };
            }
        }
        internal TitleDb()
        {
            // Util.Log.Print($"MCM test = {NTPSettings.Instance.DefaultNoblewoman}");
            // TODO: avoid abuse of execption. every title need to have default value.
            this.settings = JsonConvert.DeserializeObject<TitleGlobalSettingsJson>(
                File.ReadAllText($"{BasePath.Name}/Modules/{SubModule.modFolderName}/settings.json"),
                new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace }) ?? new TitleGlobalSettingsJson();
            this.PathTitles = $"{BasePath.Name}/Modules/{SubModule.modFolderName}/titles.json";
            this.cultureMap = JsonConvert.DeserializeObject<Dictionary<string, CultureEntryJson>>(
                File.ReadAllText(this.PathTitles),
                new JsonSerializerSettings
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                }) ?? new Dictionary<string, CultureEntryJson>();
                // ?? throw new BadTitleDatabaseException("Failed to deserialize title database!");
            if (cultureMap.Count == 0)
            {
                Util.Log.Print($">> WARNING: Title database is empty. The built-in default setting will be applied.");
                this.cultureMap["default"] = defaultCulture;
                // throw new BadTitleDatabaseException("Title database is empty!");
            }
            // Must have a fallback culture entry.
            if (!this.cultureMap.ContainsKey("default"))
            {
                Util.Log.Print($">> WARNING: Title database doesn't contain a fallback culture entry keyed by \"default\". The built-in default setting will be applied.");
                this.cultureMap["default"] = defaultCulture;
                // throw new BadTitleDatabaseException("Title database must contain a fallback culture entry keyed by \"default\"!");
            }
            foreach (KeyValuePair<string, CultureEntryJson> i in this.cultureMap)
            {
                (string cul, CultureEntryJson entry) = (i.Key, i.Value);
                if (cul == "default")
                {
                    defaultCulture = entry;
                }
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
        protected Dictionary<string, CultureEntryJson> cultureMap;
        
        private static CultureEntryJson defaultCulture = new(
                new("{=NobleTitlePlus.defaultKing}King {NAME}", "{=NobleTitlePlus.defaultQueen}Queen {NAME}"),
                new("{=NobleTitlePlus.defaultDuke}Duke {NAME}", "{=NobleTitlePlus.defaultDuchess}Duchess {NAME}"),
                new("{=NobleTitlePlus.defaultCount}Count {NAME}", "{=NobleTitlePlus.defaultCountess}Countess {NAME}"),
                new("{=NobleTitlePlus.defaultBaron}Baron {NAME}", "{=NobleTitlePlus.defaultBaroness}Baroness {NAME}"),
                new("{=NobleTitlePlus.defaultSir}{NAME}", "{=NobleTitlePlus.defaultDame}{NAME}")
            );
        private static CultureEntryJson defaultMinorFaction= new(
                new("{=NobleTitlePlus.defaultMinorLeader}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorLeader.Female}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}"),
                new("{=NobleTitlePlus.defaultMinorMember}{NAME} of {CLAN}", "{=NobleTitlePlus.defaultMinorMember.Famale}{NAME} of {CLAN}")
            );
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public class CultureEntryJson
        {
            [JsonProperty]
            public readonly EntryJson King;
            [JsonProperty]
            public readonly EntryJson Duke;
            [JsonProperty]
            public readonly EntryJson Count;
            [JsonProperty]
            public readonly EntryJson Baron;
            [JsonProperty]
            public readonly EntryJson Noble;

            public CultureEntryJson(EntryJson king, EntryJson duke, EntryJson count, EntryJson baron, EntryJson noble)
            {
                this.King = king;
                this.Duke = duke;
                this.Count = count;
                this.Baron = baron;
                this.Noble = noble;
            }
        }
        [JsonObject("Entry", MemberSerialization = MemberSerialization.OptIn)]
        public class EntryJson
        {
            [JsonProperty("Male")]
            private readonly string male = "";
            [JsonProperty("FeMale")]
            private readonly string female = "";
            public TextObject MaleFormat;
            public TextObject FemaleFormat;
            public EntryJson(string? male = null, string? female = null)
            {
                this.male = male ?? this.male;
                this.female = female ?? this.female;
                this.MaleFormat = new TextObject(this.NormalizeInputTitle(this.male));
                this.FemaleFormat = new TextObject(this.NormalizeInputTitle(this.female));
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
