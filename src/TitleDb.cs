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
using static TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultPerks;

namespace NobleTitles
{
    class TitleDb
    {
        internal Entry GetKingTitle(CultureObject culture) =>
            culture is null || !cultureMap.TryGetValue(culture.StringId, out CultureEntry? culEntry) ? noCulture.King : culEntry.King;
        internal Entry GetDukeTitle(CultureObject culture) =>
            culture is null || !cultureMap.TryGetValue(culture.StringId, out CultureEntry? culEntry) ? noCulture.Duke : culEntry.Duke;
        internal Entry GetCountTitle(CultureObject culture) =>
            culture is null || !cultureMap.TryGetValue(culture.StringId, out CultureEntry? culEntry) ? noCulture.Count : culEntry.Count;
        internal Entry GetBaronTitle(CultureObject culture) =>
            culture is null || !cultureMap.TryGetValue(culture.StringId, out CultureEntry? culEntry) ? noCulture.Baron : culEntry.Baron;
        internal Entry GetLesserNobleTitle(CultureObject culture) =>
            culture is null || !cultureMap.TryGetValue(culture.StringId, out CultureEntry? culEntry) ? noCulture.Noble : culEntry.Noble;
        internal TitleDb()
        {
            string pathSettings = BasePath.Name + $"Modules/{SubModule.Name}/settings.json";
            Dictionary<string, Dictionary<string, bool>> settings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, bool>>>(
                File.ReadAllText(pathSettings),
                new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace }) ?? new Dictionary<string, Dictionary<string, bool>>();
            if (settings.ContainsKey("general"))
            {
                if (settings["general"].ContainsKey("FogOfWar"))
                {
                    this.FogOfWar = settings["general"]["FogOfWar"];
                }
            }
            PathTitles = BasePath.Name + $"Modules/{SubModule.Name}/titles.json";
            cultureMap = JsonConvert.DeserializeObject<Dictionary<string, CultureEntry>>(
                File.ReadAllText(PathTitles),
                new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace })
                ?? throw new BadTitleDatabaseException("Failed to deserialize title database!");

            if (cultureMap.Count == 0)
                throw new BadTitleDatabaseException("Title database is empty!");

            // Must have a fallback culture entry.
            if (!cultureMap.ContainsKey("default"))
                throw new BadTitleDatabaseException("Title database must contain a fallback culture entry keyed by \"default\"!");

            foreach (KeyValuePair<string, CultureEntry> i in cultureMap)
            {
                (string cul, CultureEntry entry) = (i.Key, i.Value);

                if (entry.King is null || entry.Duke is null || entry.Count is null || entry.Baron is null)
                    throw new BadTitleDatabaseException($"All title types must be defined for culture '{cul}'!");

                if (string.IsNullOrWhiteSpace(entry.King.Male) || string.IsNullOrWhiteSpace(entry.Duke.Male) ||
                    string.IsNullOrWhiteSpace(entry.Count.Male) || string.IsNullOrWhiteSpace(entry.Baron.Male))
                    throw new BadTitleDatabaseException($"Missing at least one male variant of a title type for culture '{cul}'");
                entry.King.Male = this.NormalizeInputTitle(entry.King.Male);
                entry.King.Female = this.NormalizeInputTitle(entry.King.Female);
                entry.Duke.Male = this.NormalizeInputTitle(entry.Duke.Male);
                entry.Duke.Female = this.NormalizeInputTitle(entry.Duke.Female);
                entry.Count.Male = this.NormalizeInputTitle(entry.Count.Male);
                entry.Count.Female = this.NormalizeInputTitle(entry.Count.Female);
                entry.Baron.Male = this.NormalizeInputTitle(entry.Baron.Male);
                entry.Baron.Female = this.NormalizeInputTitle(entry.Baron.Female);
                entry.Noble.Male = this.NormalizeInputTitle(entry.Noble.Male);
                entry.Noble.Female = this.NormalizeInputTitle(entry.Noble.Female);
                // TODO: exception
                // Missing feminine titles default to equivalent masculine/neutral titles:
                // if (string.IsNullOrWhiteSpace(entry.King.Female)) entry.King.Female = entry.King.Male;
                // if (string.IsNullOrWhiteSpace(entry.Duke.Female)) entry.Duke.Female = entry.Duke.Male;
                // if (string.IsNullOrWhiteSpace(entry.Count.Female)) entry.Count.Female = entry.Count.Male;
                // if (string.IsNullOrWhiteSpace(entry.Baron.Female)) entry.Baron.Female = entry.Baron.Male;

                if (cul == "default")
                    noCulture = entry;
            }
        }
        internal string NormalizeInputTitle(string str)
        {
            string normalized = str.Replace("{", "{{").Replace("}", "}}").Replace("{{NAME}}", "{0}").Replace("{{name}}", "{0}");
            try
            {
                string.Format(normalized, "TEST NAME");
            }
            catch(Exception)
            {
                Util.Log.Print($">> WARNING: Title format {str} is invalid. It's a incorrect format!");
                normalized = "{0}";
            }
            if (!normalized.Contains("{0}"))
            {
                Util.Log.Print($">> WARNING: Title format {str} doesn't contain the name variable!");
            }
            return normalized;
        }
        internal void Serialize()
        {
            // Undo our baked-in trailing space
            foreach (CultureEntry e in cultureMap.Values)
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

            File.WriteAllText(PathTitles, JsonConvert.SerializeObject(cultureMap, Formatting.Indented));
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
                King = king;
                Duke = duke;
                Count = count;
                Baron = baron;
                Noble = noble;
            }
        }
        public class Entry
        {
            public string Male;
            public string Female;

            public Entry(string male, string female)
            {
                Male = male;
                Female = female;
            }
        }
        public class BadTitleDatabaseException : Exception
        {
            public BadTitleDatabaseException(string message) : base(message) { }
            public BadTitleDatabaseException() { }
            public BadTitleDatabaseException(string message, Exception innerException) : base(message, innerException) { }
        }
        protected string PathTitles { get; set; }
        public bool FogOfWar { get; private set; } = true;
        // culture StringId => CultureEntry (contains bulk of title information, only further split by gender)
        protected Dictionary<string, CultureEntry> cultureMap;
        protected CultureEntry noCulture = new(
            new("King", "Queen"),
            new("Duke", "Duchess"),
            new("Count", "Countess"),
            new("Baron", "Baroness"),
            new("Noble", "Lady")
            );
    }
}
