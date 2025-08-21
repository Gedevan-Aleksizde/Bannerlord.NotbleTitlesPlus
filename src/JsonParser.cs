using Newtonsoft.Json;
using TaleWorlds.Localization;

namespace NobleTitlesPlus.json
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ClanNamePair
    {
        [JsonConstructor]
        public ClanNamePair(string name_short = "", string name_long = "")
        {
            this.ClanShort = new(name_short);
            this.ClanLong = new(name_long);
        }
        public ClanNamePair(TextObject? name_short, TextObject? name_long)
        {
            this.ClanShort = name_short ?? new("");
            this.ClanLong = name_long ?? new("");
        }
        [JsonProperty("name_short")]
        public TextObject ClanShort { get; set; } = new();
        [JsonProperty("name_long")]
        public TextObject ClanLong { get; set; } = new();
    }
}
