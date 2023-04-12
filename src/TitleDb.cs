using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NobleTitlesPlus
{
    class TitleDb
    {
        internal EntryJson GetKingTitle(CultureObject culture) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntryJson? culEntry) ? noCulture.King : culEntry.King;
        internal EntryJson GetDukeTitle(CultureObject culture) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntryJson? culEntry) ? noCulture.Duke : culEntry.Duke;
        internal EntryJson GetCountTitle(CultureObject culture) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntryJson? culEntry) ? noCulture.Count : culEntry.Count;
        internal EntryJson GetBaronTitle(CultureObject culture) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntryJson? culEntry) ? noCulture.Baron : culEntry.Baron;
        internal EntryJson GetLesserNobleTitle(CultureObject culture) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntryJson? culEntry) ? noCulture.Noble : culEntry.Noble;
        internal TextObject GetTitle(bool isFemale, string cultureId, TitleRank rank)
        {
            if (this.cultureMap.TryGetValue(cultureId, out CultureEntryJson cul))
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
            else
            {
                return rank switch
                {
                    TitleRank.King => isFemale ? noCulture.King.FemaleFormat : noCulture.King.MaleFormat,
                    TitleRank.Duke => isFemale ? noCulture.Duke.FemaleFormat : noCulture.Duke.MaleFormat,
                    TitleRank.Count => isFemale ? noCulture.Count.FemaleFormat : noCulture.Count.MaleFormat,
                    TitleRank.Baron => isFemale ? noCulture.Baron.FemaleFormat : noCulture.Baron.MaleFormat,
                    TitleRank.Noble => isFemale ? noCulture.Noble.FemaleFormat : noCulture.Noble.MaleFormat,
                    _ => new TextObject("{NAME}"),
                };
            }
        }
        internal TitleDb()
        {
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
                this.cultureMap["default"] = noCulture;
                // throw new BadTitleDatabaseException("Title database is empty!");
            }
            // Must have a fallback culture entry.
            if (!this.cultureMap.ContainsKey("default"))
            {
                Util.Log.Print($">> WARNING: Title database doesn't contain a fallback culture entry keyed by \"default\". The built-in default setting will be applied.");
                this.cultureMap["default"] = noCulture;
                // throw new BadTitleDatabaseException("Title database must contain a fallback culture entry keyed by \"default\"!");
            }
            foreach (KeyValuePair<string, CultureEntryJson> i in this.cultureMap)
            {
                (string cul, CultureEntryJson entry) = (i.Key, i.Value);
                if (cul == "default")
                {
                    noCulture = entry;
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
        [JsonObject("Settings")]
        public class TitleGlobalSettingsJson
        {
            [JsonProperty("General")]
            public GeneralSettings General { get; private set; } = new GeneralSettings();
            [JsonProperty("Format")]
            public FormatSettings Format { get; private set; } = new FormatSettings();
            public class GeneralSettings
            {
                [JsonProperty("FogOfWar")]
                public bool FogOfWar { get; private set; }
            }
            public class FormatSettings
            {
                [JsonProperty("FiefNameSeparator")]
                public string FiefNameSepratorComma { get; private set; } = ",";
                [JsonProperty("FiefNameSeparatorLast")]
                public string FiefNameSeparatorAnd { get; private set; } = "and";
                [JsonProperty("Tagging")]
                public bool Tagging { get; private set; } = true;
                [JsonProperty("MaxFiefNames")]
                public int MaxFiefNames { get; private set; } = 3;
            }
        }
        private static CultureEntryJson noCulture = new(
                new("{=NobleTitlePlus.defaultKing}King {NAME}", "{=NobleTitlePlus.defaultQueen}Queen {NAME}"),
                new("{=NobleTitlePlus.defaultDuke}Duke {NAME}", "{=NobleTitlePlus.defaultDuchess}Duchess {NAME}"),
                new("{=NobleTitlePlus.defaultCount}Count {NAME}", "{=NobleTitlePlus.defaultCountess}Countess {NAME}"),
                new("{=NobleTitlePlus.defaultBaron}Baron {NAME}", "{=NobleTitlePlus.defaultBaroness}Baroness {NAME}"),
                new("{=NobleTitlePlus.defaultSir}{NAME}", "{=NobleTitlePlus.defaultDame}{NAME}")
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
}
