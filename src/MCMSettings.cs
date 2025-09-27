using MCM.Abstractions.Base;
using MCM.Abstractions.Base.PerCampaign;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;
using NobleTitlesPlus.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NobleTitlesPlus.MCMSettings
{
    public record Options()
    {
        public bool VerboseLog { get; set; } = false;
        public bool FogOfWar { get; set; } = true;
        public bool Encyclopedia { get; set; } = false;
        public bool ShowOnSettlementUI { get; set; } = true;
        public bool ShowOnConversation { get; set; } = true;
        public bool ShowOnMission { get; set; } = true;
        public bool ShowOnPartyTooltip { get; set; } = true;
        public bool SpouseTitle { get; private set; } = true;
        public bool FixShokuhoClanName { get; set; } = false;
        public bool Tagging { get; set; } = false;
        public string FiefNameSeparator { get; set; } = ",";
        public string FiefNameSeparatorLast { get; set; } = "and";
        public int MaxFiefNames { get; set; } = 1;
        public int DivisorCapDuke { get; set; } = 3;
        public int ThresholdBaron { get; set; } = 3;
        public Dropdown<TextObject> KingdomTitleFormat { get; set; } = new(Enum.GetValues(typeof(KingdomTitleFormat)).OfType<KingdomTitleFormat>().ToList().Select(x => GameTexts.FindText("ntp_mcm", $"kingdom_title_format_{x.ToString().ToLower()}")), 1);
        public Dropdown<TextObject> SuffixNumFormat = new(Enum.GetValues(typeof(SuffixNumberFormat)).OfType<SuffixNumberFormat>().ToList().Select(x => GameTexts.FindText("ntp_mcm", $"suffix_number_format_{x.ToString().ToLower()}")), 1);
        public bool UseUnitedTitle { get; set; } = false;
        public Dropdown<TextObject> Inheritance { get; set; } = new(Enum.GetValues(typeof(Inheritance)).OfType<Inheritance>().ToList().Select(x => GameTexts.FindText("ntp_mcm", $"heir_{x.ToString().ToLower()}")), 1);
        public TitleSet TitleSet { get; set; } = new();
    }
    public class MCMRuntimeSettings
    {
        private const string settingsId = SubModule.Name;
        private static string SettingsName => $"{SubModule.DisplayName} v{SubModule.ModVersion}/{SubModule.AssemblyVersion}";
        public static readonly string[] ImperialFactions = { "empire_w", "empire_s", "empire" };

        private string saveId = "";
        private FluentPerCampaignSettings? settings;
        public Options Options { get; private set; }
        public Nomenclatura Nomenclatura { get; private set; } = new();
        public static MCMRuntimeSettings? Instance;
        public MCMRuntimeSettings(string saveId)
        {
            this.Options ??= new Options();
            this.saveId = saveId;
        }
        /// <summary>
        /// 
        /// </summary>
        public void InitializeMCMSettings()
        {
            // Options should be constructed before creating settings, but should be initialized after the settings registered.
            ISettingsBuilder builder = CreateSettingBuilder(this.saveId);
            this.Options.TitleSet.Initialize();
            this.settings = builder.BuildAsPerCampaign();
            this.settings.Register();
            if (this.Options.VerboseLog) Util.Log.Print("MCM Settings are initlialized", LogCategory.Debug);
        }
        /// <summary>
        /// this should be called as soon after the game started.
        /// nomenclatura should be updated after the Options.titleset is set
        /// </summary>
        public void InitializeNomenclaturaOnGameStart()
        {
            if (this.Options.VerboseLog) Util.Log.Print($"InitializeNomenclaturaOnGameStart: kingdoms={Kingdom.All.Count}", LogCategory.Debug);
            Nomenclatura.UpdateAll(this.Options.FixShokuhoClanName);
            if (this.Options.VerboseLog) Util.Log.Print($">> [INFO] Starting new campaign on {SubModule.Name}");
        }
        /// <summary>
        /// this should be called when quitting the game mode.
        /// </summary>
        public void Clear()
        {
            this.settings?.Unregister();
        }
        public void UpdateNomenclaturaOnDailyTick()
        {
            IEnumerable<Kingdom> survivingImperial = Kingdom.All.Where(x => !x.IsEliminated && ImperialFactions.Contains(x.StringId));
            if (survivingImperial.Count() == 1)
            {
                Nomenclatura.OverwriteWithImperialFormats(survivingImperial.First());
            }
            Nomenclatura.UpdateAll(this.Options.FixShokuhoClanName);
        }
        /// <summary>
        /// crea a settings builder. This deeply depends on this.Options.TitleSet
        /// </summary>
        /// <param name="saveid"></param>
        /// <returns></returns>
        internal ISettingsBuilder CreateSettingBuilder(string saveid)
        {
            Util.Log.Print($"CreateSettings Called. kindoms={Kingdom.All.Count}", LogCategory.Info);
            ISettingsBuilder builder = BaseSettingsBuilder.Create(settingsId, SettingsName)
                .SetFormat("json2")
                .SetFolderName(settingsId)
                .SetSubFolder(saveid)
                .CreateGroup(FindTextShortMCM("general_category"), BuildGeneralGroupProperties)
                .CreateGroup(FindTextShortMCM("fief_category"), BuildFormattingGroupProperties)
                .CreateGroup(FindTextShortMCM("shokuho_category"), BuildShokuhoGroupProperties)
                .CreateGroup(FindTextShortMCM("advanced_category"), BuildAdvancedGroupProperties)
                .CreateGroup(FindTextShortMCM("culture_default"), GenerateKingdomGroupPropertiesBuilder("default", 4))
                .CreateGroup(FindTextShortMCM("minor_default"), GenerateMinorFactionGroupProperties("default", 5));
            ;
            const int orderoffset = 6;
            int j = 0;
            foreach (string cultureId in this.Options.TitleSet.cultures.Keys.Where(x => x != "default"))
            {
                string name = GameTexts.FindText("str_faction_formal_name_for_culture", cultureId).ToString();
                builder.CreateGroup(name, GenerateKingdomGroupPropertiesBuilder(cultureId, orderoffset + j)); ;
                Util.Log.Print($"Category {name}({cultureId}, culture) added to MCM options", LogCategory.Info);
                j++;
            }
            if (this.Options.TitleSet.factions.ContainsKey("new_kingdom"))
            {
                // TODO: how to identify player kingdom
                string name = Kingdom.All.Where(k => k.StringId == "new_kingdom")?.First()?.Name?.ToString() ?? FindTextShortMCM("error_kingdom");
                builder.CreateGroup(name, GenerateKingdomGroupPropertiesBuilder("new_kingdom", orderoffset + j, false)); ;
                Util.Log.Print($"Category {name}(new_kingdom, faction) added to MCM options", LogCategory.Info);
                j++;
            }
            foreach (string kingdomId in this.Options.TitleSet.factions.Keys.Where(x => x != "new_kingdom"))
            {
                string name = Kingdom.All.Where(k => k.StringId == kingdomId)?.First()?.InformalName.ToString() ?? FindTextShortMCM("error_kingdom");
                builder.CreateGroup(name, GenerateKingdomGroupPropertiesBuilder(kingdomId, orderoffset + j, false)); ;
                Util.Log.Print($"Category {name}({kingdomId}, faction) added to MCM options", LogCategory.Info);
                j++;
            }
            List<Clan> clans = Clan.All.Where(c => c.IsMinorFaction && !c.Leader.IsHumanPlayerCharacter).ToList();
            foreach (string clanId in this.Options.TitleSet.minorFactions.Keys.Where(x => x != "default"))
            {
                string name = clans.Where(c => c.StringId == clanId).First().Name.ToString(); // TODO
                builder.CreateGroup(name, GenerateMinorFactionGroupProperties(clanId, orderoffset + j));
                Util.Log.Print($"Category {name}({clanId}, minor faction) added to MCM options", LogCategory.Info);
                j++;
            }
            builder.CreatePreset(BaseSettings.DefaultPresetId, BaseSettings.DefaultPresetName, builder => BuildPreset(builder, "DEF"));
            foreach ((string id, string name) presetName in new List<(string, string)>() { ("ORI", "Original"), ("VAR", "Variant"), ("VARM", "VariantModified"), ("SHO", "Shokuho"), ("SHOM", "ShokuhoModified") })
            {
                builder.CreatePreset(presetName.name, FindTextShortMCM($"preset_{presetName.name.ToLower().Replace(" ", "_")}"), builder => BuildPreset(builder, presetName.id));
            }
            Util.Log.Print($"Builder created", LogCategory.Info);
            return builder;

            /*
             * general setting items
             */
            void BuildGeneralGroupProperties(ISettingsPropertyGroupBuilder builder) => builder
                .AddBool("fogOfWar", "{=yF9agd1M}Fog of War",
                    new ProxyRef<bool>(
                        () => this.Options.FogOfWar,
                        value => this.Options.FogOfWar = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("fow_hint")).SetOrder(0)
                    )
                // TODO
                .AddDropdown("heir", FindTextShortMCM("heir"), 1,
                    new ProxyRef<Dropdown<TextObject>>(
                        () => this.Options.Inheritance,
                        value => this.Options.Inheritance = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("heir_hint")).SetOrder(6)
                )
                .AddDropdown("suffix_num", FindTextShortMCM("suffix_num"), 0,
                    new ProxyRef<Dropdown<TextObject>>(
                        () => this.Options.SuffixNumFormat,
                        value => this.Options.SuffixNumFormat = value),
                    probBuilder => probBuilder.SetRequireRestart(true).SetHintText(FindTextShortMCM("suffix_num_hint")).SetOrder(7)
                )
                .SetGroupOrder(0);
            void BuildFormattingGroupProperties(ISettingsPropertyGroupBuilder builder) => builder
                .AddBool(
                    "tagging", FindTextShortMCM("tagging"),
                    new ProxyRef<bool>(
                        () => this.Options.Tagging,
                        value => this.Options.Tagging = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("tagging_hint")).SetOrder(0)
                    )
                .AddText("fiefNameSeparator", FindTextShortMCM("separator"),
                    new ProxyRef<string>(
                        () => this.Options.FiefNameSeparator,
                        value => this.Options.FiefNameSeparator = value
                        ),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("separator_hint")).SetOrder(1)
                    )
                .AddText("fiefNameSeparatorLast", FindTextShortMCM("separator_last"),
                    new ProxyRef<string>(
                        () => this.Options.FiefNameSeparatorLast,
                        value => this.Options.FiefNameSeparatorLast = value
                        ),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("separator_last_hint")).SetOrder(2)
                    )
                .AddInteger("maxFiefNames", FindTextShortMCM("size"), 0, 10,
                    new ProxyRef<int>(
                        () => this.Options.MaxFiefNames,
                        value => this.Options.MaxFiefNames = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("size_hint")).SetOrder(3)
                    )
                .SetGroupOrder(1);

            void BuildShokuhoGroupProperties(ISettingsPropertyGroupBuilder builder) => builder
                .AddBool("fix_shokuho_clan_name", FindTextShortMCM("fix_shokuho_clan_name"), new ProxyRef<bool>(
                    () => this.Options.FixShokuhoClanName,
                    value =>
                    {
                        if (value)
                        {
                            this.Nomenclatura.UpdateAll(true);
                        }
                        this.Options.FixShokuhoClanName = value;
                    }
                    ),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("fix_shokuho_clan_name_hint")).SetOrder(0)
                ).SetGroupOrder(2); ;

            void BuildAdvancedGroupProperties(ISettingsPropertyGroupBuilder builder) => builder
                .AddInteger(
                "duke_cap_divisor", FindTextShortMCM("duke_cap_divisor"), 1, 10, new ProxyRef<int>(
                    () => this.Options.DivisorCapDuke,
                    value =>
                    {
                        this.Options.DivisorCapDuke = value;
                    }
                    ),
                propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("duke_cap_divisor_hint")).SetOrder(0)
                )
                .AddInteger(
                "baron_threshold", FindTextShortMCM("baron_threshold"), 1, 10, new ProxyRef<int>(
                    () => this.Options.ThresholdBaron,
                    value =>
                    {
                        this.Options.ThresholdBaron = value;
                    }
                    ),
                propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("baron_threshold_hint")).SetOrder(1)
                )
                /*Canceled. the native implementation is too messy */
                /*
                .AddBool("encyclopedia", FindTextShortMCM("encyclo"),
                    new ProxyRef<bool>(
                        () => options.Encyclopedia,
                        value => options.Encyclopedia = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("encyclo_hint")).SetOrder(1)
                    )
                */
                .AddBool("use_united_empire_title", FindTextShortMCM("united_empire"),
                new ProxyRef<bool>(
                    () => this.Options.UseUnitedTitle,
                    value => this.Options.UseUnitedTitle = value),
                propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("united_empire_hint")).SetOrder(8)
                )
                /*Canceled. the native implementation is too messy */
                /*
                .AddDropdown("kingdom_title_format", FindTextShortMCM("kingdom_title_format"), 0,
                new ProxyRef<Dropdown<TextObject>>(
                    () => options.KingdomTitleFormat,
                    value =>
                    {
                        options.KingdomTitleFormat = value;

                        switch ((KingdomTitleFormat)value.SelectedIndex)
                        {
                            case KingdomTitleFormat.Abbreviated:
                                Util.Log.Print("[DEBUG] Kingdom format is abbreviated");
                                SubModule.harmony?.PatchCategory("KingdomAbbreviated");
                                SubModule.harmony?.UnpatchCategory("KingdomFull");
                                break;
                            case KingdomTitleFormat.Full:
                                Util.Log.Print("[DEBUG] Kingdom format is full");
                                SubModule.harmony?.PatchCategory("KingdomFull");
                                SubModule.harmony?.UnpatchCategory("KingdomAbbreviated");
                                break;
                            case KingdomTitleFormat.Default:
                                Util.Log.Print("[DEBUG] Kingdom format is default");
                                SubModule.harmony?.UnpatchCategory("KingdomFull");
                                SubModule.harmony?.UnpatchCategory("KingdomAbbreviated");
                                break;
                        }
                    }),
                probBuilder => probBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("kingdom_title_format_hint")).SetOrder(7)
                ) */
                .AddBool("VerboseLog", FindTextShortMCM("verbose"),
                new ProxyRef<bool>(
                    () => this.Options.VerboseLog,
                    value => this.Options.VerboseLog = value),
                propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("verbose_hint")).SetOrder(9)
                )
                .SetGroupOrder(3);

            /*
             * each kingdom's title format entries
             */
            Action<ISettingsPropertyGroupBuilder> GenerateKingdomGroupPropertiesBuilder(string id, int order, bool isCulture = true)
            {
                void BuildFactionGroupProperties(ISettingsPropertyGroupBuilder builder)
                {
                    foreach (string s in (string[])Enum.GetNames(typeof(DB.Gender)))
                    {
                        bool isFemale = s == "F";
                        string genderLong = s == "F" ? "Female" : "Male";
                        // for(int  rank = 0; rank < 6; rank++) // TODO: Why not working??
                        foreach (int rank in new int[] { 0, 1, 2, 3, 4, 5 })
                        {
                            builder
                                .AddText(
                                    $"KingdomRank{rank}{s}_{id}", FindTextShortMCM($"title_{rank}{s}"),
                                    new ProxyRef<string>(
                                        () => this.Options.TitleSet.GetTitleRaw(isFemale, id, isCulture ? null : id, (TitleRank)rank, Category.Default),
                                        value =>
                                        {
                                            if (isCulture) this.Options.TitleSet.SetCultureTitle(value, id, isFemale, (TitleRank)rank);
                                            else this.Options.TitleSet.SetFactionTitle(value, id, isFemale, (TitleRank)rank);
                                        }
                                    ),
                                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM($"{rank}{s}_hint"))
                                    .SetOrder(rank == 0 ? 99 : 2 + 2 * rank + (isFemale ? 0 : 1))
                                    );
                        }
                        builder
                        .AddText($"KingdomRoyal{s}_{id}", FindTextShortMCM($"title_Royal{s}"),
                            new ProxyRef<string>(
                                () => this.Options.TitleSet.GetTitleRaw(isFemale, id, isCulture ? null : id, TitleRank.Royal, Category.Default),
                                value =>
                                {
                                    if (isCulture) this.Options.TitleSet.SetCultureTitle(value, id, isFemale, TitleRank.Royal);
                                    else this.Options.TitleSet.SetFactionTitle(value, id, isFemale, TitleRank.Royal);
                                }),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM($"Royal{s}_hint"))
                            .SetOrder(14 + (isFemale ? 0 : 1))
                        );
                        builder
                        .AddText($"KingdomCrown{s}_{id}", FindTextShortMCM($"title_Crown{s}"),
                            new ProxyRef<string>(
                                () => this.Options.TitleSet.GetTitleRaw(isFemale, id, isCulture ? null : id, TitleRank.Prince, Category.Default),
                                value =>
                                {
                                    if (isCulture) this.Options.TitleSet.SetCultureTitle(value, id, isFemale, TitleRank.Prince);
                                    else this.Options.TitleSet.SetFactionTitle(value, id, isFemale, TitleRank.Prince);
                                }),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM($"Crown{s}_hint"))
                            .SetOrder(16 + (isFemale ? 0 : 1))
                        );
                    }
                    builder.SetGroupOrder(order);
                }
                return BuildFactionGroupProperties;
            }
            /*
             * each minor faction's title format entries
             */
            Action<ISettingsPropertyGroupBuilder> GenerateMinorFactionGroupProperties(string clanStringId, int order)
            {
                void BuildMinorFactionGroupProperties(ISettingsPropertyGroupBuilder builder)
                {
                    if (this.Options.VerboseLog) Util.Log.Print($">> [INFO] {clanStringId} group added on MCM");
                    foreach (string g in (string[])Enum.GetNames(typeof(DB.Gender)))
                    {
                        bool isFemale = g == "F";
                        builder.AddText(
                            $"MinorL{g}_{clanStringId}", FindTextShortMCM($"title_minorL{g}"),
                            new ProxyRef<string>(
                                () => this.Options.TitleSet.GetMinorTitleRaw(clanStringId, isFemale, TitleRank.King),
                                value => this.Options.TitleSet.SetMinorFactionTitle(clanStringId, isFemale, TitleRank.King, value)
                            ),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM($"minorL{g}_hint"))
                            .SetOrder(0 + (g == "F" ? 0 : 1))
                        );
                        builder.AddText($"MinorM{g}_{clanStringId}", FindTextShortMCM($"title_minorM{g}"),
                            new ProxyRef<string>(
                                () => this.Options.TitleSet.GetMinorTitleRaw(clanStringId, isFemale, TitleRank.Noble),
                                value => this.Options.TitleSet.SetMinorFactionTitle(clanStringId, isFemale, TitleRank.Noble, value)
                            ),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM($"minorM{g}_hint"))
                            .SetOrder(2 + (g == "F" ? 0 : 1))
                        );
                    }
                    builder.SetGroupOrder(order);
                }
                return BuildMinorFactionGroupProperties;
            }

            ///
            /// <summary>
            /// set preset values
            /// </summary>
            ///
            void BuildPreset(ISettingsPresetBuilder builder, string preset)
            {
                Dropdown<TextObject> dropDownPreset = new(Enum.GetValues(typeof(KingdomTitleFormat)).OfType<KingdomTitleFormat>().ToList().Select(x => GameTexts.FindText("ntp_mcm", $"kingdom_title_format_{x.ToString().ToLower()}")), (int)KingdomTitleFormat.Default);
                builder
                    .SetPropertyValue("fogOfWar", true)
                    .SetPropertyValue("encyclopedia", false) // TODO
                    .SetPropertyValue("tagging", MBTextManager.ActiveTextLanguage != "日本語")
                    .SetPropertyValue("fiefNameSeparator", FindTextShortMCM("separator_value"))
                    .SetPropertyValue("fiefNameSeparatorLast", FindTextShortMCM("separator_last_value"))
                    .SetPropertyValue("maxFiefName", 1)
                    .SetPropertyValue("kingdom_title_format", dropDownPreset)
                    .SetPropertyValue("fix_shokuho_clan_name", false);
                FillFactionPropertyValues(preset, "default", "default");
                FillMinorFactionPropertyValues(preset, "default");
                foreach (String cultureId in Kingdom.All.Select(k => k.Culture.StringId).Distinct())
                {
                    FillFactionPropertyValues(preset, cultureId, null);
                }
                foreach (String factionId in Clan.All.Where(c => c.IsMinorFaction && !c.Leader.IsHumanPlayerCharacter).Select(c => c.StringId))
                {
                    FillMinorFactionPropertyValues(preset, factionId);
                }
                // FIXME
                List<string> defaultKingdomIds = new() { "aserai", "battania", "khuzait", "empire", "empire_s", "empire_w", "sturgia", "vlandia" };
                foreach (Kingdom k in Kingdom.All.Where(x => !defaultKingdomIds.Contains(x.StringId)).OrderBy(x => x.Name.ToString()))
                {
                    FillFactionPropertyValues(preset, k.Culture.StringId, k.StringId);
                }
                void FillFactionPropertyValues(string preset, string cultureId, string? kingdomId)
                {
                    // tried to get value from kingdoms, then get it from cultures when failed that
                    foreach (string s in (string[])Enum.GetNames(typeof(DB.Gender)))
                    {
                        foreach (int rank in new int[] { 0, 1, 2, 3, 4, 5 })
                        {
                            if (preset == "DEF")
                            {
                                if (rank == 5)
                                {
                                    builder.SetPropertyValue($"KingdomRank{rank}{s}_{cultureId}", "{NAME}");
                                }
                                else if (GameTexts.TryGetText($"str_faction_{(rank == 1 ? "ruler" : "noble")}_name_with_title", out TextObject TOCulture, cultureId))
                                {
                                    TextObject a = new("", new Dictionary<string, object>() { { "GENDER", s == "F" ? 1 : 0 }, { "NAME", "______MOCKPLACEHOLDER_____" } });
                                    builder.SetPropertyValue($"KingdomRank{rank}{s}_{cultureId}", Util.QuoteVarBitEasiler(TOCulture.SetTextVariable("RULER", a)));
                                }
                                else
                                {
                                    TextObject TO = GameTexts.FindText(moduleStrTitles, $"{preset}_{rank}{s}_default");
                                    builder.SetPropertyValue($"KingdomRank{rank}{s}_{kingdomId}", Util.QuoteMultVarBitEasiler(TO));
                                }
                            }
                            else if (kingdomId != null) // TODO: is really needed?
                            {
                                if (GameTexts.TryGetText(moduleStrTitles, out TextObject TOKingdom, $"{preset}_{rank}{s}_{kingdomId ?? ""}"))
                                {
                                    builder.SetPropertyValue($"KingdomRank{rank}{s}_{kingdomId}", Util.QuoteMultVarBitEasiler(TOKingdom));
                                }
                                else
                                {
                                    builder.SetPropertyValue($"KingdomRank{rank}{s}_{cultureId}", "");
                                }
                            }
                            else
                            {
                                builder.SetPropertyValue($"KingdomRank{rank}{s}_{cultureId}", FindTitleTextString(preset, rank.ToString(), s, cultureId));
                            }
                        }
                        builder.SetPropertyValue($"KingdomCrown{s}_{cultureId}", FindTitleTextString(preset, "crown", s, cultureId));
                        builder.SetPropertyValue($"KingdomRoyal{s}_{cultureId}", FindTitleTextString(preset, "royal", s, cultureId));
                    }
                }
                void FillMinorFactionPropertyValues(string preset, string factionId)
                {
                    foreach (string lm in new string[] { "L", "M" })
                    {
                        foreach (string s in (string[])Enum.GetNames(typeof(DB.Gender)))
                        {
                            builder.SetPropertyValue($"Minor{lm}{s}_{factionId}", FindTitleTextString(preset, $"Minor{lm}", s, factionId));
                        }
                    }
                }
            }
        }
        public const string moduleStrTitles = "ntp_title_set";
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FindTextShortMCM(string variantId)
        {
            return GameTexts.FindText("ntp_mcm", variantId).ToString();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FindTitleTextString(string preset, string rank, string gender, string group)
        {
            if (GameTexts.TryGetText(moduleStrTitles, out TextObject to, $"{preset}_{rank}{gender}_{group}"))
            {
                return Util.QuoteMultVarBitEasiler(to);
            }
            else
            {
                return Util.QuoteMultVarBitEasiler(GameTexts.FindText(moduleStrTitles, $"{preset}_{rank}{gender}_default"));
            }
        }
    }
    public enum FallbackType
    {
        Default,
        Vanilla,
        Localization
    }
    public enum SuffixNumberFormat
    {
        None,
        All,
        UntilSecond
    }
}