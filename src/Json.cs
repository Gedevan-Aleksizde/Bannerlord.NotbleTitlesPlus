using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NobleTitlesPlus
{
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
            public bool FogOfWar { get; private set; } = true;
            [JsonProperty("SpouseTitle")]
            public bool SpouseTitle { get; private set; } = true;
            [JsonProperty("Encyclopedia")]
            public bool ApplyToEncyclopedia { get; private set; } = false;
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
}
