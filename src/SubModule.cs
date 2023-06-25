using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using System.Reflection;
using System.IO;
using TaleWorlds.Localization;
using MCM.Internal.Extensions;
using MCM.Implementation;
using MCM.Abstractions;
using System.Collections.Generic;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;
using MCM.Abstractions.Base.PerSave;
using NobleTitlesPlus.Settings;
using System;
using System.Runtime.CompilerServices;

namespace NobleTitlesPlus
{
    public class SubModule : MBSubModuleBase
    {
        /* Semantic Versioning (https://semver.org): */
        // TODO: Why we can't extract it from assembly info or submodule.xml?
        // public const int SemVerMajor = 1;
        // public const int SemVerMinor = 2;
        // public const int SemVerPatch = 0;
        // public static readonly string? SemVerSpecial = null;
        // private static readonly string SemVerEnd = SemVerSpecial is not null ? "-" + SemVerSpecial : string.Empty;
        // public static readonly string Version = $"{SemVerMajor}.{SemVerMinor}.{SemVerPatch}{SemVerEnd}";
        public static readonly Version assemblyVersion = typeof(RuntimeSettings).Assembly.GetName().Version;
        public static string ModVersion = "";
        public const string Name = "NobleTitlePlus";
        public const string DisplayName = "Noble Titles Plus";
        public static readonly string modFolderName = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Name;
        public static readonly string HarmonyDomain = "com.skatagiri.bannerlord" + Name.ToLower();
        internal static readonly Color ImportantTextColor = Color.FromUint(0x00F16D26); // orange
        private FluentPerSaveSettings? settings;
        private Options Options { get; set; }
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Util.EnableLog = true; // enable various debug logging
            Util.EnableTracer = false; // enable code event tracing (requires enabled logging)
            /*if (!SaveManagerPatch.Apply(new(HarmonyDomain)))
            {
                Util.Log.Print($"Patch was required! Canceling {DisplayName}...");
                canceled = true;
            }*/
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            if (!hasLoaded && !canceled)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{new TextObject("{=NTP.Sys002}{DisplayName} Loaded").SetTextVariable("DisplayName", new TextObject(DisplayName))} (Assembly v{assemblyVersion})", ImportantTextColor));
                hasLoaded = true;
            }

            if (canceled)
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $"003 {new TextObject("{=NTP.Sys003}Error loading {DisplayName} : Disabled!").SetTextVariable("DisplayName", new TextObject(DisplayName))} (Assembly v{assemblyVersion})"
                        ));
        }
        public override void OnAfterGameInitializationFinished(Game game, object starterObject)
        {
            if (game.GameType is not Campaign campaign)
            {
                return;
            }
            System.Diagnostics.Debug.Assert(settings is null);
            var builder = RuntimeSettings.AddSettings(Options!, campaign.UniqueGameId);
            settings = builder.BuildAsPerSave();
            settings?.Register();
            base.OnAfterGameInitializationFinished(game, starterObject);
        }
        protected override void OnGameStart(Game game, IGameStarter starterObject)
        {
            base.OnGameStart(game, starterObject);

            if (!canceled && game.GameType is Campaign)
            {
                Options ??= new();
                ((CampaignGameStarter)starterObject).AddBehavior(new TitleBehavior());
            }
            if (starterObject is not CampaignGameStarter campaignGameStarter)
            {
                return;
            }
            
            //  campaignGameStarter.AddBehavior(eqUpBehavior = new AutoEquipBehavior(Options));
        }
        public override void OnGameEnd(Game game)
        {
            var oldSettings = settings;
            oldSettings?.Unregister();
            settings = null;

            Options = null;
            base.OnGameEnd(game);
        }

        private bool hasLoaded;
        private bool canceled;
    }
}
