using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using Newtonsoft.Json;
using StoryMode.GauntletUI.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultPerks;

namespace NobleTitlesPlus
{
    class TitleDb
    {
        [JsonObject("Settings")]
        public class TitleGlobalSettings
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
        internal Entry GetKingTitle(CultureObject culture) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntry? culEntry) ? this.noCulture.King : culEntry.King;
        internal Entry GetDukeTitle(CultureObject culture) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntry? culEntry) ? this.noCulture.Duke : culEntry.Duke;
        internal Entry GetCountTitle(CultureObject culture) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntry? culEntry) ? this.noCulture.Count : culEntry.Count;
        internal Entry GetBaronTitle(CultureObject culture) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntry? culEntry) ? this.noCulture.Baron : culEntry.Baron;
        internal Entry GetLesserNobleTitle(CultureObject culture) =>
            culture is null || !this.cultureMap.TryGetValue(culture.StringId, out CultureEntry? culEntry) ? this.noCulture.Noble : culEntry.Noble;
        internal TitleDb()
        {
            // TODO: avoid abuse of execption. every title need to have default value.
            this.settings = JsonConvert.DeserializeObject<TitleGlobalSettings>(
                File.ReadAllText($"{BasePath.Name}/Modules/{SubModule.modFolderName}/settings.json"),
                new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace }) ?? new TitleGlobalSettings();
            this.PathTitles = $"{BasePath.Name}/Modules/{SubModule.modFolderName}/titles.json";
            this.cultureMap = JsonConvert.DeserializeObject<Dictionary<string, CultureEntry>>(
                File.ReadAllText(this.PathTitles),
                new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace })
                ?? throw new BadTitleDatabaseException("Failed to deserialize title database!");
            if (this.cultureMap.Count == 0)
                throw new BadTitleDatabaseException("Title database is empty!");
            // Must have a fallback culture entry.
            if (!this.cultureMap.ContainsKey("default"))
                throw new BadTitleDatabaseException("Title database must contain a fallback culture entry keyed by \"default\"!");
            foreach (KeyValuePair<string, CultureEntry> i in this.cultureMap)
            {
                (string cul, CultureEntry entry) = (i.Key, i.Value);

                if (entry.King is null || entry.Duke is null || entry.Count is null || entry.Baron is null || entry.Noble is null)
                    throw new BadTitleDatabaseException($"All title types must be defined for culture '{cul}'!");
                if (string.IsNullOrWhiteSpace(entry.King.Male) || string.IsNullOrWhiteSpace(entry.Duke.Male) ||
                    string.IsNullOrWhiteSpace(entry.Count.Male) || string.IsNullOrWhiteSpace(entry.Baron.Male))
                    throw new BadTitleDatabaseException($"Missing at least one male variant of a title type for culture '{cul}'");
                entry.King.Male = NormalizeInputTitle(entry.King.Male);
                entry.King.Female = NormalizeInputTitle(entry.King.Female);
                entry.Duke.Male = NormalizeInputTitle(entry.Duke.Male);
                entry.Duke.Female = NormalizeInputTitle(entry.Duke.Female);
                entry.Count.Male = NormalizeInputTitle(entry.Count.Male);
                entry.Count.Female = NormalizeInputTitle(entry.Count.Female);
                entry.Baron.Male = NormalizeInputTitle(entry.Baron.Male);
                entry.Baron.Female = NormalizeInputTitle(entry.Baron.Female);
                entry.Noble.Male = NormalizeInputTitle(entry.Noble.Male);
                entry.Noble.Female = NormalizeInputTitle(entry.Noble.Female);
                if (cul == "default")
                    this.noCulture = entry;
            }
        }
        internal string NormalizeInputTitle(string str)
        {
            string normalized = str.Replace("{name}", "{NAME}");
            try
            {
                new TextObject(normalized, new Dictionary<string, object>() { ["NAME"] = "TEST NAME"}).ToString();
            }
            catch (Exception)
            {
                Util.Log.Print($">> WARNING: Title format {str} is invalid. It's a incorrect format! This format is inavailable.");
                normalized = "{NAME}";
            }
            if (!normalized.Contains("{NAME}"))
            {
                Util.Log.Print($">> WARNING: Title format {str} doesn't contain the name variable! This format is inavailable.");
                normalized = "{NAME}";
            }
            return normalized;
        }
        internal void Serialize()
        {
            // Undo our baked-in trailing space
            foreach (CultureEntry e in this.cultureMap.Values)
            {
                e.King.Male = RmEndChar(e.King.Male);
                e.King.Female = RmEndChar(e.King.Female);
                e.Duke.Male = RmEndChar(e.Duke.Male);
                e.Duke.Female = RmEndChar(e.Duke.Female);
                e.Count.Male = RmEndChar(e.Count.Male);
                e.Count.Female = RmEndChar(e.Count.Female);
                e.Baron.Male = RmEndChar(e.Baron.Male);
                e.Baron.Female = RmEndChar(e.Baron.Female);
            }

            File.WriteAllText(this.PathTitles, JsonConvert.SerializeObject(this.cultureMap, Formatting.Indented));
        }

        private string RmEndChar(string s) => s.Substring(0, s.Length - 1);

        // private string StripTitlePrefix(string s, string prefix) => s.StartsWith(prefix) ? s.Remove(0, prefix.Length) : s;

        public class CultureEntry
        {
            public readonly Entry King;
            public readonly Entry Duke;
            public readonly Entry Count;
            public readonly Entry Baron;
            public readonly Entry Noble;

            public CultureEntry(Entry king, Entry duke, Entry count, Entry baron, Entry noble)
            {
                this.King = king;
                this.Duke = duke;
                this.Count = count;
                this.Baron = baron;
                this.Noble = noble;
            }
        }
        public class Entry
        {
            public string Male;
            public string Female;

            public Entry(string male, string female)
            {
                this.Male = male;
                this.Female = female;
            }
        }
        public class BadTitleDatabaseException : Exception
        {
            public BadTitleDatabaseException(string message) : base(message) { }
            public BadTitleDatabaseException() { }
            public BadTitleDatabaseException(string message, Exception innerException) : base(message, innerException) { }
        }
        protected string PathTitles { get; set; }
        public TitleGlobalSettings settings;
        // culture StringId => CultureEntry (contains bulk of title information, only further split by gender)
        protected Dictionary<string, CultureEntry> cultureMap;
        protected CultureEntry noCulture = new(
            new("{=NobleTitlePlus.King}King {NAME}", "{=NobleTitlePlus.Queen}Queen {NAME}"),
            new("{=NobleTitlePlus.Duke}Duke {NAME}", "{=NobleTitlePlus.Duchess}Duchess {NAME}"),
            new("{=NobleTitlePlus.Count}Count {NAME}", "{=NobleTitlePlus.Countess}Countess {NAME}"),
            new("{=NobleTitlePlus.Baron}Baron {NAME}", "{=NobleTitlePlus.Baroness}Baroness {NAME}"),
            new("{=NobleTitlePlus.Sir}{NAME}", "{=NobleTitlePlus.Dame}{NAME}")
            );
    }
}
