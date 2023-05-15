// using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;
// using MCM.Abstractions.Attributes.v2;
// using MCM.Abstractions.Attributes;

namespace NobleTitlesPlus.Settings
{
    /*public class NTPSettings : AttributeGlobalSettings<NTPSettings>
    {
        public override string Id => "NobleTitlesPlus";
        public override string DisplayName => $"{new TextObject(SubModule.DisplayName)} {typeof(NTPSettings).Assembly.GetName().Version.ToString(3)}";
        public override string FolderName => "NobleTitlesPlus"; // TODO: ??????
        public override string FormatType => "json2";
        //Headings
        private const string HeadingGeneral = "{=NTP.OptH000}General";
        private const string HeadingFief = "{=NTP.OptH001}Fief Name Format";
        private const string HeadingTitlesDefault = "{=NTP.OptH00D}Default Titles";
        private const string HeadingTitlesAserai = "{=NTP.OptH00A}Aserai Titles";
        private const string HeadingTitlesBattania = "{=NTP.OptH00B}Battanian Titles";
        private const string HeadingTitlesEmpire = "{=NTP.OptH00E}Imperial Titles";
        private const string HeadingTitlesKhuzait = "{=NTP.OptH00S}Khuzait Titles";
        private const string HeadingTitlesSturgia = "{=NTP.OptH00S}Sturgian Titles";
        private const string HeadingTitlesVlandia = "{=NTP.OptH00V}Vlandian Titles";
        // General
        [SettingPropertyBool("{=NTP.Opt000}Fog of War", Order = 0, RequireRestart = false, HintText = "{=NTP.Opt000Desc}Enable Fog of War to titles; titles are not shown unless you have met the hero.")]
        [SettingPropertyGroup(HeadingGeneral, GroupOrder = 0)]
        public bool FogOfWar { get; set; } = true;
        // fief
        [SettingPropertyBool("{=NTP.Opt001}Tagging", Order =0, RequireRestart = false, HintText = "{=NTP.Opt001Desc}Tagging fief names. Basically True is recommended if you are European langauge user.")]
        [SettingPropertyGroup(HeadingFief, GroupOrder = 1)]
        public bool Tagging { get; set; } = true;
        [SettingPropertyText("{=NTP.Opt002}Fief Name Separator", Order = 1, RequireRestart = false, HintText ="{=NTP.Opt002Desc}Separator for Fief Names.")]
        [SettingPropertyGroup(HeadingFief)]
        public string FifeNameSeparator { get; set; } = ",";
        [SettingPropertyText("{=NTP.Opt003}Fief Name Separator Last", Order = 1, RequireRestart = false, HintText = "{=NTP.Opt003Desc}Separator for the Last Fief Name.")]
        [SettingPropertyGroup(HeadingFief)]
        public string FiefNameSeparatorLast { get; set; } = "and";
        [SettingPropertyInteger("{=NTP.Opt004}Max Number of Fief Names", 1, 10, Order = 2, RequireRestart = false, HintText = "{=NTP.Opt004Desc}Separator for the Last Fief Name.")]
        [SettingPropertyGroup(HeadingFief)]
        public int MaxFiefName { get; set; } = 3;
        // titles
        [SettingPropertyText("{=NTP.OptTitleKing}King Title", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault, GroupOrder = 2)]
        public string DefaultKing { get; set; } = "King {NAME}";
        [SettingPropertyText("{=NTP.OptTitleQueen}Queen Title", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultQueen { get; set; } = "Queen {NAME}";
        [SettingPropertyText("{=NTP.OptTitleDuke}Duke Title", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultDuke { get; set; } = "Duke {NAME}";
        [SettingPropertyText("{=NTP.OptTitleDuchess}Duchess Title", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultDuchess { get; set; } = "Duchess {NAME}";
        [SettingPropertyText("{=NTP.OptTitleCount}Count Title", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultCount { get; set; } = "Count {NAME}";
        [SettingPropertyText("{=NTP.OptTitleCountess}Countess Title", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultCountess { get; set; } = "Countess {NAME}";
        [SettingPropertyText("{=NTP.OptTitleCount}Baron Title", Order = 6, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultBaron { get; set; } = "Baron {NAME}";
        [SettingPropertyText("{=NTP.OptTitleBaroness}Baroness Title", Order = 7, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultBaroness { get; set; } = "Baroness {NAME}";
        [SettingPropertyText("{=NTP.OptTitleNobleman}Nobleman Title", Order = 8, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultNobleman { get; set; } = "{NAME}";
        [SettingPropertyText("{=NTP.OptTitleNoblewoman}Noblewoman Title", Order = 9, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultNoblewoman { get; set; } = "{NAME}";
        [SettingPropertyText("{=NTP.OptTitleMinorLeaderM}Minor Faction Male Leader Title", Order = 10, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultMinorLeaderM { get; set; } = "{NAME}";
        [SettingPropertyText("{=NTP.OptTitleMinorLeaderF}Minor Faction Female Leader Title", Order = 10, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultMinorLeaderF { get; set; } = "{NAME}";
        [SettingPropertyText("{=NTP.OptTitleMinorMemberF}Minor Faction Male Member Title", Order = 11, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultMinorMemberM { get; set; } = "{NAME}";
        [SettingPropertyText("{=NTP.OptTitleMinorMemberF}Minor Faction Female Member Title", Order = 11, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultMinorMemberF { get; set; } = "{NAME}";
    }*/
}
