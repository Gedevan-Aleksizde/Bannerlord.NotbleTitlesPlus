using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Base.Global;
using System.Collections.Generic;
using TaleWorlds.Localization;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using NobleTitlesPlus.DB;
using TaleWorlds.Core.ViewModelCollection;
using MCM.Abstractions.FluentBuilder;
using TaleWorlds.Core;
using MCM.Common;
using MCM.Abstractions.Base;
using System.Linq;

namespace NobleTitlesPlus.Settings
{
    public record Options()
    {
        public bool FogOfWar { get; set; } = true;
        public bool Encyclopedia { get; set; } = false;
        public bool Tagging { get; set;} = false;
        public string FiefNameSeparator { get; set; } = "{=NTP.sep},";
        public string FiefNameSeparatorLast { get; set; } = "{=NTP.sepLast}and";
        public int MaxFiefNames { get; set; } = 3;
        public TitleSet TitleSet { get; set; } = new();
    }
    internal static class RuntimeSettings
    {
        private const string settingsId = SubModule.Name;
        private static string SettingsName => $"{SubModule.DisplayName} v{SubModule.ModVersion} (assembly: v{SubModule.assemblyVersion})";

        internal static ISettingsBuilder AddSettings(Options options, string saveid)
        {
            var builder = BaseSettingsBuilder.Create(settingsId, SettingsName)
                .SetFormat("json2")
                .SetFolderName(settingsId)
                .SetSubFolder(saveid)
                .CreateGroup("{=NTP.MCMGGEN}General", BuildGeneralGroupProperties)
                .CreateGroup("{=NTP.MCMFOR}Fief Display Format", BuildFormattingGroupProperties)
                .CreateGroup("{=NTP.MCMGDEf}Default Titles", BuildFactionGroupProperties);
            foreach (CultureObject cult in Kingdom.All.Select(k => k.Culture).OrderBy(c => c.Name.ToString()))
            {
                builder.CreateGroup(cult.Name.ToString(), BuildFactionGroupProperties); ;
            }
            builder.CreateGroup("{=NTP.MCMGMinor}Minor Faction Default", BuildMinorFactionGroupProperties);
            foreach (Clan c in Clan.All.Where(c => c.IsMinorFaction))
            {
                builder.CreateGroup(c.Name.ToString(), BuildMinorFactionGroupProperties);
            }
            List<string> defaultKingdoms = new() { "aserai", "battania", "khuzait", "empire", "sturgia", "vlandia" };
            foreach (Kingdom k in Kingdom.All.Where(x => !defaultKingdoms.Contains(x.StringId)).OrderBy(x => x.Name.ToString()))
            {
                builder.CreateGroup(k.Name.ToString(), BuildFactionGroupProperties);
            }
            builder.CreatePreset(BaseSettings.DefaultPresetId, BaseSettings.DefaultPresetName, builder => BuildDefaultPreset(builder, new()));
            return builder;

            void BuildGeneralGroupProperties(ISettingsPropertyGroupBuilder builder) => builder
                .AddBool("fogOfWar", "{=yF9agd1M}Fog of War",
                    new ProxyRef<bool>(() => options.FogOfWar, value => options.FogOfWar = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NPT.MCMFOWHint}Enable Fog of War to titles; titles are not shown unless you have met the hero.")
                    )
                .AddBool("encyclopedia", "{=MxmOWsHj}Encyclopedia",
                    new ProxyRef<bool>(() => options.Encyclopedia, value => options.Encyclopedia = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMencyclopediaHint}Current Inavailable")
                    );
            void BuildFormattingGroupProperties(ISettingsPropertyGroupBuilder builder) => builder
                .AddBool(
                    "tagging", "{=NTP.MCMTag}Tagging", new ProxyRef<bool>(() => options.Tagging, value => options.Tagging = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMTagHint}Tagging fief names. Basically enabling recommended if you are European langauge user.")
                    )
                .AddText("fiefNameSeparator", "{=NTP.MCMSep}Fief Name Separator",
                    new ProxyRef<string>(() => new TextObject(options.FiefNameSeparator).ToString(), value => options.FiefNameSeparator = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMSepHint}Separator for Fief Names.")
                    )
                .AddText("fiefNameSeparatorLast", "{=NTP.MCM003}Fief Name Last Separator",
                    new ProxyRef<string>(() => new TextObject(options.FiefNameSeparatorLast).ToString(), value => options.FiefNameSeparatorLast = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMSepLastHint}Separator for the Last Fief Name.")
                    )
                .AddInteger("maxFiefNames", "{=NTP.MCMSize}Max Fief Names", 0, 10,
                    new ProxyRef<int>(() => options.MaxFiefNames, value => options.MaxFiefNames = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMSizeHint}")
                    );
            void BuildFactionGroupProperties(ISettingsPropertyGroupBuilder builder)
            {
                builder
                    .AddText("factionCommonFemale", "{=NTP.MCMKing}King",
                        new ProxyRef<string>(() => options.FiefNameSeparatorLast, value => options.FiefNameSeparatorLast = value),
                        propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMKingHint}Title of a male monorch"))
                    .AddText("factionCommonMale", "{=NTP.MCMQueen}Queen",
                        new ProxyRef<string>(() => options.FiefNameSeparatorLast, value => options.FiefNameSeparatorLast = value),
                        propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMQueenHint}Title of a female monorch"));
                // TODO: add crown prince/princess
                for (int i = 1; i <= 4; i++)
                {
                    builder
                        .AddText(
                            $"factionVassal{i}Female", $"{{=NTP.MCMVassal{i}M}}Male Vassal Tier 1",
                            new ProxyRef<string>(() => options.FiefNameSeparatorLast, value => options.FiefNameSeparatorLast = value),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText($"{{=NTP.MCMVassal{i}MHint}}")
                            )
                        .AddText(
                            $"factionVassal{i}Male", $"{{=NTP.MCMVassal1F{i}F}}Female Vassal Tier 1",
                            new ProxyRef<string>(() => options.FiefNameSeparatorLast, value => options.FiefNameSeparatorLast = value),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText($"{{=NTP.MCMVassal{i}FHint}}")
                            );
                }
                builder
                    .AddText("factionCommonFemale", "{=NTP.MCMnoblewoman}Noblewoman",
                        new ProxyRef<string>(() => options.FiefNameSeparatorLast, value => options.FiefNameSeparatorLast = value),
                        propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.noblewomanHint}"))
                    .AddText("factionCommonMale", "{=NTP.MCMnobleman}Nobleman",
                        new ProxyRef<string>(() => options.FiefNameSeparatorLast, value => options.FiefNameSeparatorLast = value),
                        propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.noblemanHint}"));
            }
            void BuildMinorFactionGroupProperties(ISettingsPropertyGroupBuilder builder)
            {
                builder.AddText("minorFactionCommonF", "{=NTP.MinorMF}Female Member",
                    new ProxyRef<string>(() => options.FiefNameSeparatorLast, value => options.FiefNameSeparatorLast = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMMinorMFHint}")
                    )
                    .AddText("minorFactionCommonM", "{=NTP.MinorMM}Male Member",
                        new ProxyRef<string>(() => options.FiefNameSeparatorLast, value => options.FiefNameSeparatorLast = value),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMMinorMMHint}")
                        )
                    .AddText("minorFactionLeaderF", "{=NTP.MinorLF}Female Leader",
                        new ProxyRef<string>(() => options.FiefNameSeparatorLast, value => options.FiefNameSeparatorLast = value),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMMinorLFHint}")
                        )
                    .AddText("minorFactionLeaderM", "{=NTP.MinorLM}Male Leader",
                        new ProxyRef<string>(() => options.FiefNameSeparatorLast, value => options.FiefNameSeparatorLast = value),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText("{=NTP.MCMMinorLMHint}")
                        );
            }
            static void BuildDefaultPreset(ISettingsPresetBuilder builder, Options option)
            {
                // No scape from boilerplating...
                builder
                    .SetPropertyValue("fogOfWar", option.FogOfWar)
                    .SetPropertyValue("encyclopedia", option.Encyclopedia)
                    .SetPropertyValue("tagging", option.Tagging)
                    .SetPropertyValue("fiefNameSeparator", option.FiefNameSeparator)
                    .SetPropertyValue("fiefNameSeparatorLast", option.FiefNameSeparatorLast)
                    .SetPropertyValue("maxFiefName", option.MaxFiefNames);
            }
        }
    }
    /*
    public interface ICustomSettingsProvider
    {
        public bool FogOfWar { get; set; }
        public string FifeNameSeparator { get; set; }
        public string FiefNameSeparatorLast { get; set; }
        public int MaxFiefName { get; set; }
        public string DefaultKing { get; set; }
        public string DefaultQueen { get; set; }
        public string DefaultDuke { get; set; }
        public string DefaultDuchess { get; set; }
        public string DefaultCount { get; set; }
        public string DefaultCountess { get; set; }
        public string DefaultBaron { get; set; }
        public string DefaultBaroness { get; set; }
        public string DefaultNobleman { get; set; }
        public string DefaultNoblewoman { get; set; }
        public string DefaultMinorLeaderM { get; set; }
        public string DefaultMinorLeaderF { get; set; }
        public string DefaultMinorMemberM { get; set; }
        public string DefaultMinorMemberF { get; set; }
    } */
    /*
    public class HardcodedCustomSettings : ICustomSettingsProvider
    {
        public bool FogOfWar { get; set; } = true;
        public string FifeNameSeparator { get; set; } = ",";
        public string FiefNameSeparatorLast { get; set; } = "and";
        public int MaxFiefName { get; set; } = 3;
        public string DefaultKing { get; set; } = "King {NAME}";
        public string DefaultQueen { get; set; } = "Queen {NAME}";
        public string DefaultDuke { get; set; } = "Duke {NAME}";
        public string DefaultDuchess { get; set; } = "Duchess {NAME}";
        public string DefaultCount { get; set; } = "Count {NAME}";
        public string DefaultCountess { get; set; } = "Countess {NAME}";
        public string DefaultBaron { get; set; } = "Baron {NAME}";
        public string DefaultBaroness { get; set; } = "Baroness {NAME}";
        public string DefaultNobleman { get; set; } = "{NAME}";
        public string DefaultNoblewoman { get; set; } = "{NAME}";
        public string DefaultMinorLeaderM { get; set; } = "{NAME}";
        public string DefaultMinorLeaderF { get; set; } = "{NAME}";
        public string DefaultMinorMemberM { get; set; } = "{NAME}";
        public string DefaultMinorMemberF { get; set; } = "{NAME}";
    }
    */
    /*
    internal class NTPSetttings : AttributeGlobalSettings<NTPSetttings>, ICustomSettingsProvider
    {
        public override string Id => "NobleTitlesPlus";
        public override string DisplayName => $"Noble Titles Plus v{typeof(NTPSetttings).Assembly.GetName().Version.ToString(3)}";
        public override string FolderName { get; } = "NobleTitlesPlus"; // TODO: ??????
        public override string FormatType { get; } = "json2";
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
        [SettingPropertyBool(
            "{=NTP.Opt000}Fog of War", Order = 0, RequireRestart = false,
            HintText = "{=NTP.Opt000Desc}Enable Fog of War to titles; titles are not shown unless you have met the hero.")]
        [SettingPropertyGroup(HeadingGeneral, GroupOrder = 0)]
        public bool FogOfWar {
            get => _fogOfWar;
            set
            {
                if (_fogOfWar != value)
                {
                    _fogOfWar = value;
                    OnPropertyChanged(nameof(FogOfWar));
                }
            }
        }
        public bool _fogOfWar = true;
        [SettingPropertyBool("{NTP.Opt001}Encyclopedia", Order = 1, RequireRestart = false, HintText = "{=NPT.Opt001Desc}Currently Pending")]
        [SettingPropertyGroup(HeadingGeneral, GroupOrder = 0)]
        public bool Encyclopedia { get; set; }
        // fief
        [SettingPropertyBool("{=NTP.Opt001}Tagging", Order = 0, RequireRestart = false,
            HintText = "{=NTP.Opt001Desc}Tagging fief names. Basically True is recommended if you are European langauge user.")]
        [SettingPropertyGroup(HeadingFief, GroupOrder = 1)]
        public bool Tagging { get; set; }
        [SettingPropertyText("{=NTP.Opt002}Fief Name Separator", Order = 1, RequireRestart = false,
            HintText = "{=NTP.Opt002Desc}Separator for Fief Names.")]
        [SettingPropertyGroup(HeadingFief)]
        public string FifeNameSeparator { get; set; }
        private string _fiefNameSeparator = ",";
        [SettingPropertyText("{=NTP.Opt003}Fief Name Separator Last", Order = 1, RequireRestart = false,
            HintText = "{=NTP.Opt003Desc}Separator for the Last Fief Name.")]
        [SettingPropertyGroup(HeadingFief)]
        public string FiefNameSeparatorLast { get; set; }
        [SettingPropertyInteger("{=NTP.Opt004}Max Number of Fief Names", 1, 10, Order = 2, RequireRestart = false,
            HintText = "{=NTP.Opt004Desc}Separator for the Last Fief Name.")]
        [SettingPropertyGroup(HeadingFief)]
        public int MaxFiefName { get; set; }
        // titles
        [SettingPropertyText("{=NTP.OptTitleKing}King Title", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault, GroupOrder = 2)]
        public string DefaultKing { get; set; }
        [SettingPropertyText("{=NTP.OptTitleQueen}Queen Title", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultQueen { get; set; }
        [SettingPropertyText("{=NTP.OptTitleDuke}Duke Title", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultDuke { get; set; }
        [SettingPropertyText("{=NTP.OptTitleDuchess}Duchess Title", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultDuchess { get; set; }
        [SettingPropertyText("{=NTP.OptTitleCount}Count Title", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultCount { get; set; }
        [SettingPropertyText("{=NTP.OptTitleCountess}Countess Title", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultCountess { get; set; }
        [SettingPropertyText("{=NTP.OptTitleCount}Baron Title", Order = 6, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultBaron { get; set; }
        [SettingPropertyText("{=NTP.OptTitleBaroness}Baroness Title", Order = 7, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultBaroness { get; set; }
        [SettingPropertyText("{=NTP.OptTitleNobleman}Nobleman Title", Order = 8, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultNobleman { get; set; }
        [SettingPropertyText("{=NTP.OptTitleNoblewoman}Noblewoman Title", Order = 9, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultNoblewoman { get; set; }
        [SettingPropertyText("{=NTP.OptTitleMinorLeaderM}Minor Faction Male Leader Title", Order = 10, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultMinorLeaderM { get; set; }
        [SettingPropertyText("{=NTP.OptTitleMinorLeaderF}Minor Faction Female Leader Title", Order = 10, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultMinorLeaderF { get; set; }
        [SettingPropertyText("{=NTP.OptTitleMinorMemberF}Minor Faction Male Member Title", Order = 11, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultMinorMemberM { get; set; }
        [SettingPropertyText("{=NTP.OptTitleMinorMemberF}Minor Faction Female Member Title", Order = 11, RequireRestart = false)]
        [SettingPropertyGroup(HeadingTitlesDefault)]
        public string DefaultMinorMemberF { get; set; }
        private TitleSet.FactionTitleSet CheckAndFillMissingTitles(TitleSet.FactionTitleSet set, TitleSet.FactionTitleSet defaultValue)
        {
            foreach (bool isFemale in new bool[] { false, true })
            {
                foreach (TitleRank rank in (TitleRank[])Enum.GetValues(typeof(TitleRank)))
                {
                    if (set.GetTitle(isFemale, rank, true).ToStringWithoutClear() == "")
                    {
                        set.SetTitle(isFemale, rank, defaultValue.GetTitle(isFemale, rank));
                    }
                }
            }
            return set;
        }
        // private ICustomSettingsProvider _provider;
        public NTPSetttings()
        {
            /*
            if (NTPSetttings.Instance is not null)
            {
                //_provider = NTPSetttings.Instance;
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
                    }) ?? new();
                Util.Log.Print($"{this.titleSet.cultures.Count} set of titles for culutures");
                Util.Log.Print($"{this.titleSet.kingdoms.Count} set of titles for kingdoms");
                Util.Log.Print($"{this.titleSet.kingdoms.Count} set of titles for minor factions");
                foreach (KeyValuePair<string, TitleSet.FactionTitleSet> i in this.titleSet.cultures)
                {
                    (string cul, TitleSet.FactionTitleSet entry) = (i.Key, i.Value);

                }
                foreach (KeyValuePair<string, TitleSet.FactionTitleSet> i in this.titleSet.minorFactions)
                {
                    (string cul, TitleSet.FactionTitleSet entry) = (i.Key, i.Value);

                }
                if (!this.titleSet.cultures.ContainsKey("default"))
                {
                    this.titleSet.cultures.Add("default", TitleSet.defaultCultureValue);
                }
                if (!this.titleSet.minorFactions.ContainsKey("default"))
                {
                    this.titleSet.minorFactions.Add("default", TitleSet.defaultMinorFactionValue);

                }
            }
            else
            {
                //_provider = new HardcodedCustomSettings();
            }
        }
        private TitleSet titleSet;
        private TitleGlobalSettingsJson settings;
        protected string PathTitles { get; set; }
    }
    */
}