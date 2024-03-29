﻿using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Base.Global;
using System.Collections.Generic;
using TaleWorlds.Localization;
using System;
using TaleWorlds.CampaignSystem;
using NobleTitlesPlus.DB;
using MCM.Abstractions.FluentBuilder;
using TaleWorlds.Core;
using MCM.Common;
using MCM.Abstractions.Base;
using System.Linq;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace NobleTitlesPlus.Settings
{
    public record Options()
    {
        public bool VerboseLog { get; set; } = false;
        public bool FogOfWar { get; set; } = true;
        public bool Encyclopedia { get; set; } = false;
        public bool SpouseTitle { get; private set; } = true;
        public bool Tagging { get; set; } = false;
        public string FiefNameSeparator { get; set; } = ",";
        public string FiefNameSeparatorLast { get; set; } = "and";
        public int MaxFiefNames { get; set; } = 1;
        public Dropdown<TextObject> Inheritance { get; set; } = new(Enum.GetValues(typeof(DB.Inheritance)).OfType<DB.Inheritance>().ToList().Select(x => GameTexts.FindText("str_ntp_mcm", $"heir_{x.ToString().ToLower()}")), 1);
    public TitleSet TitleSet { get; set; } = new();
    }
    internal static class RuntimeSettings
    {
        private const string settingsId = SubModule.Name;
        private static string SettingsName => $"{SubModule.DisplayName} v{SubModule.ModVersion} (assembly: v{SubModule.AssemblyVersion})";
        internal static ISettingsBuilder AddSettings(Options options, string saveid)
        {
            Util.Log.Print($">> [INFO] AddSettings Called. kindoms={Kingdom.All.Count}");
            var builder = BaseSettingsBuilder.Create(settingsId, SettingsName)
                .SetFormat("json2")
                .SetFolderName(settingsId)
                .SetSubFolder(saveid)
                .CreateGroup(FindTextShortMCM("general_category"), BuildGeneralGroupProperties)
                .CreateGroup(FindTextShortMCM("fief_category"), BuildFormattingGroupProperties)
                .CreateGroup(FindTextShortMCM("culture_default"), GenerateKingdomGroupPropertiesBuilder("default", 2))
                .CreateGroup(FindTextShortMCM("minor_default"), GenerateMinorFactionGroupProperties("default", 3));
            ;

            int j = 0;
            foreach (string cultureId in options.TitleSet.cultures.Keys.Where(x => x != "default"))
            {
                string name = GameTexts.FindText("str_faction_formal_name_for_culture", cultureId).ToString();
                builder.CreateGroup(name, GenerateKingdomGroupPropertiesBuilder(cultureId, 4 + j)); ;
                Util.Log.Print($">> [INFO] Category {name}({cultureId}, culture) added to MCM options");
                j++;
            }
            if (options.TitleSet.factions.ContainsKey("new_kingdom"))
            {
                // TODO: how to identify player kingdom
                string name = Kingdom.All.Where(k => k.StringId == "new_kingdom")?.First()?.Name?.ToString() ?? FindTextShortMCM("fallback_player");
                builder.CreateGroup(name, GenerateKingdomGroupPropertiesBuilder("new_kingdom", 4 + j, false)); ;
                Util.Log.Print($">> [INFO] Category {name}(new_kingdom, faction) added to MCM options");
                j++;
            }
            foreach (string kingdomId in options.TitleSet.factions.Keys.Where(x => x != "new_kingdom"))
            {
                string name = GameTexts.FindText("str_short_term_for_faction", kingdomId).ToString();
                builder.CreateGroup(name, GenerateKingdomGroupPropertiesBuilder(kingdomId, 4 + j, false)); ;
                Util.Log.Print($">> [INFO] Category {name}({kingdomId}, faction) added to MCM options");
                j++;
            }
            List<Clan> clans = Clan.All.Where(c => c.IsMinorFaction && !c.Leader.IsHumanPlayerCharacter).ToList();
            foreach (string clanId in options.TitleSet.minorFactions.Keys.Where(x => x != "default"))
            {
                string name = clans.Where(c => c.StringId == clanId).First().Name.ToString(); // TODO
                builder.CreateGroup(name, GenerateMinorFactionGroupProperties(clanId, 4 + j));
                Util.Log.Print($">> [INFO] Category {name}({clanId}, minor faction) added to MCM options");
                j++;
            }
            builder.CreatePreset(BaseSettings.DefaultPresetId, BaseSettings.DefaultPresetName, builder => BuildPreset(builder, new(), "DEF"));
            foreach((string id, string name) presetName in new List<(string, string)>(){ ("ORI", "Original"), ("VAR", "Variant")}){
                builder.CreatePreset(presetName.name, FindTextShortMCM($"preset_{presetName.name.ToLower().Replace(" ", "_")}"), builder => BuildPreset(builder, new(), presetName.id));
            }
            return builder;

            void BuildGeneralGroupProperties(ISettingsPropertyGroupBuilder builder) => builder
                .AddBool("fogOfWar", "{=yF9agd1M}Fog of War",
                    new ProxyRef<bool>(
                        () => options.FogOfWar,
                        value => options.FogOfWar = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("fow_hint")).SetOrder(0)
                    )
                .AddBool("encyclopedia", "{=MxmOWsHj}Encyclopedia",
                    new ProxyRef<bool>(
                        () => options.Encyclopedia,
                        value => options.Encyclopedia = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("encyclo_hint")).SetOrder(1)
                    )
                .AddBool("VerboseLog", FindTextShortMCM("verbose"),
                new ProxyRef<bool>(
                    () => options.VerboseLog,
                    value => options.VerboseLog = value),
                propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("verbose_hint")).SetOrder(2)
                )
                .AddDropdown("heir", FindTextShortMCM("heir"), 1,
                    new ProxyRef<Dropdown<TextObject>>(
                        () => options.Inheritance,
                        value => options.Inheritance = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("heir_hint")).SetOrder(4)
                )
                .SetGroupOrder(0);
            void BuildFormattingGroupProperties(ISettingsPropertyGroupBuilder builder) => builder
                .AddBool(
                    "tagging", FindTextShortMCM("tagging"),
                    new ProxyRef<bool>(
                        () => options.Tagging,
                        value => options.Tagging = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("tagging_hint")).SetOrder(0)
                    )
                .AddText("fiefNameSeparator", FindTextShortMCM("separator"),
                    new ProxyRef<string>(
                        () => options.FiefNameSeparator,
                        value => options.FiefNameSeparator = value
                        ),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("separator_hint")).SetOrder(1)
                    )
                .AddText("fiefNameSeparatorLast", FindTextShortMCM("separator_last"),
                    new ProxyRef<string>(
                        () => options.FiefNameSeparatorLast,
                        value => options.FiefNameSeparatorLast = value
                        ),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("separator_last_hint")).SetOrder(2)
                    )
                .AddInteger("maxFiefNames", FindTextShortMCM("size"), 0, 10,
                    new ProxyRef<int>(
                        () => options.MaxFiefNames,
                        value => options.MaxFiefNames = value),
                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM("size_hint")).SetOrder(3)
                    )
                .SetGroupOrder(1);
            Action<ISettingsPropertyGroupBuilder> GenerateKingdomGroupPropertiesBuilder(string id, int order, bool isCulture = true)
            {
                void BuildFactionGroupProperties(ISettingsPropertyGroupBuilder builder)
                {
                    foreach (string s in (string[]) Enum.GetNames(typeof(DB.Gender)))
                    {
                        bool isFemale = s == "F";
                        string genderLong = s == "F" ? "Female" : "Male";
                        // for(int  rank = 1; rank < 6; rank++) // TODO: Why not working??
                        foreach (int rank in new int[] { 1, 2, 3, 4, 5 })
                        {
                            builder
                                .AddText(
                                    $"KingdomRank{rank}{s}_{id}", FindTextShortMCM($"title_{rank}{s}"),
                                    new ProxyRef<string>(
                                        () => options.TitleSet.GetTitleRaw(isFemale, id, isCulture ? null : id, (TitleRank)rank, Category.Default),
                                        value => {
                                            if(isCulture) options.TitleSet.SetCultureTitle(value, id, isFemale, (TitleRank)rank);
                                            else options.TitleSet.SetFactionTitle(value, id, isFemale, (TitleRank)rank);
                                        }
                                    ),
                                    propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM($"{rank}{s}_hint"))
                                    .SetOrder(2 + 2 * rank + (isFemale ? 0 : 1))
                                    );
                        }
                        builder
                        .AddText($"KingdomRoyal{s}_{id}", FindTextShortMCM($"title_Royal{s}"),
                            new ProxyRef<string>(
                                () => options.TitleSet.GetTitleRaw(isFemale, id, isCulture ? null : id, TitleRank.Royal, Category.Default),
                                value =>
                                {
                                    if (isCulture) options.TitleSet.SetCultureTitle(value, id, isFemale, TitleRank.Royal);
                                    else options.TitleSet.SetFactionTitle(value, id, isFemale, TitleRank.Royal);
                                }),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM($"Royal{s}_hint"))
                            .SetOrder(14 + (isFemale ? 0 : 1))
                        );
                        builder
                        .AddText($"KingdomCrown{s}_{id}", FindTextShortMCM($"title_Crown{s}"),
                            new ProxyRef<string>(
                                () => options.TitleSet.GetTitleRaw(isFemale, id, isCulture ? null : id, TitleRank.Prince, Category.Default),
                                value =>
                                {
                                    if (isCulture) options.TitleSet.SetCultureTitle(value, id, isFemale, TitleRank.Prince);
                                    else options.TitleSet.SetFactionTitle(value, id, isFemale, TitleRank.Prince);
                                }),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM($"Crown{s}_hint"))
                            .SetOrder(16 + (isFemale ? 0 : 1))
                        );
                    }
                    builder.SetGroupOrder(order);
                }
                return BuildFactionGroupProperties;
            }
            Action<ISettingsPropertyGroupBuilder> GenerateMinorFactionGroupProperties(string clanStringId, int order)
            {
                void BuildMinorFactionGroupProperties(ISettingsPropertyGroupBuilder builder)
                {
                    if(TitleBehavior.options.VerboseLog) Util.Log.Print($">> [INFO] {clanStringId} group added on MCM");
                    foreach (string g in (string[]) Enum.GetNames(typeof(DB.Gender)))
                    {
                        bool isFemale = g == "F";
                        builder.AddText(
                            $"MinorL{g}_{clanStringId}", FindTextShortMCM($"title_minorL{g}"),
                            new ProxyRef<string>(
                                () => options.TitleSet.GetMinorTitleRaw(clanStringId, isFemale, TitleRank.King),
                                value => options.TitleSet.SetMinorFactionTitle(clanStringId, isFemale, TitleRank.King, value)
                            ),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM($"minorL{g}_hint"))
                            .SetOrder(0 + (g == "F" ? 0 : 1))
                        );
                        builder.AddText($"MinorM{g}_{clanStringId}", FindTextShortMCM($"title_minorM{g}"),
                            new ProxyRef<string>(
                                () => options.TitleSet.GetMinorTitleRaw(clanStringId, isFemale, TitleRank.Noble),
                                value => options.TitleSet.SetMinorFactionTitle(clanStringId, isFemale, TitleRank.Noble, value)
                            ),
                            propBuilder => propBuilder.SetRequireRestart(false).SetHintText(FindTextShortMCM($"minorM{g}_hint"))
                            .SetOrder(2 + (g == "F" ? 0 : 1))
                        );
                    }
                    builder.SetGroupOrder(order);
                }
                return BuildMinorFactionGroupProperties;
            }
            void BuildPreset(ISettingsPresetBuilder builder, Options option, string preset)
            {
                builder
                    .SetPropertyValue("fogOfWar", true)
                    .SetPropertyValue("encyclopedia", false) // TODO
                    .SetPropertyValue("tagging", MBTextManager.ActiveTextLanguage != "日本語")
                    .SetPropertyValue("fiefNameSeparator", FindTextShortMCM("separator_value"))
                    .SetPropertyValue("fiefNameSeparatorLast", FindTextShortMCM("separator_last_value"))
                    .SetPropertyValue("maxFiefName", 1);
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
                        foreach (int rank in new int[] { 1, 2, 3, 4, 5 })
                        // for (int rank = 1; rank < 6; rank++)  // TODO: why not working??
                        {
                            if (preset == "DEF")
                            {
                                if(rank == 5)
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
                            else if(kingdomId != null) // TODO: is really needed?
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
                    foreach(string lm in new string[] { "L", "M" })
                    {
                        foreach (string s in (string[])Enum.GetNames(typeof(DB.Gender)))
                        {
                            builder.SetPropertyValue($"Minor{lm}{s}_{factionId}", FindTitleTextString(preset, $"Minor{lm}", s, factionId));
                        }
                    }
                }
            }
        }
        public const string moduleStrTitles = "str_ntp_title_set";
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FindTextShortMCM(string variantId)
        {
            return GameTexts.FindText("str_ntp_mcm", variantId).ToString();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FindTitleTextString(string preset, string rank, string gender, string group)
        {
            if(GameTexts.TryGetText(moduleStrTitles, out TextObject to, $"{preset}_{rank}{gender}_{group}"))
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
}