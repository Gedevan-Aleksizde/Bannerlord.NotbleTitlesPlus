﻿using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using System.Reflection;
using System.IO;
using TaleWorlds.Localization;
using MCM.Abstractions.Base.PerSave;
using NobleTitlesPlus.Settings;
using System;
using TaleWorlds.ModuleManager;
using TaleWorlds.Engine;

namespace NobleTitlesPlus
{
    public class SubModule : MBSubModuleBase
    {
        public static string ModVersion
        {
            get
            {
                ModuleInfo info = new();
                string ver;
                try
                {
                    info.LoadWithFullPath(Utilities.GetFullModulePath("NobleTitlesPlus"));
                    ver = $"{info.Version.Major}.{info.Version.Minor}.{info.Version.Revision}" ;
                }
                catch
                {
                    ver = "(ERROR)";
                }
                return ver;
            }
        }
        public static string AssemblyVersion
        {
            get
            {
                return $"{SubModule.assemblyVersion.Major}.{SubModule.assemblyVersion.Minor}.{SubModule.assemblyVersion.Build}";
            }
        }
        public static readonly Version assemblyVersion = typeof(RuntimeSettings).Assembly.GetName().Version;
        public const string Name = "NobleTitlePlus";
        public const string DisplayName = "Noble Titles Plus";
        public static readonly string modFolderName = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Name;
        public static readonly string HarmonyDomain = "com.skatagiri.bannerlord" + Name.ToLower();
        internal static readonly Color ImportantTextColor = Color.FromUint(0x00F16D26); // orange
        private FluentPerSaveSettings? settings;
        public static Options? Options { get; private set; }
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
                    $"{new TextObject("{=NTP.Sys002}{DisplayName} Loaded").SetTextVariable("DisplayName", new TextObject(DisplayName))} (Assembly v{AssemblyVersion})", ImportantTextColor));
                hasLoaded = true;
            }

            if (canceled)
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $"003 {new TextObject("{=NTP.Sys003}Error loading {DisplayName} : Disabled!").SetTextVariable("DisplayName", new TextObject(DisplayName))} (Assembly v{AssemblyVersion})"
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
