using HarmonyLib;
using MCM.Abstractions.Base.PerSave;
using NobleTitlesPlus.MCMSettings;
using System;
using System.IO;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace NobleTitlesPlus
{
    public class SubModule : MBSubModuleBase
    {

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Util.EnableLog = true; // enable various debug logging
            Util.EnableTracer = false; // enable code event tracing (requires enabled logging)
            this.harmony = new(SubModule.HarmonyDomain);
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            // Util.Log.Print($">> OnBeforeInitialModuleScreenSetAsRoot called");
            base.OnBeforeInitialModuleScreenSetAsRoot();

            if (!this.hasLoaded && !this.canceled)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{new TextObject("{=NTP.Sys002}{DisplayName} Loaded").SetTextVariable("DisplayName", new TextObject(DisplayName))} (Assembly v{AssemblyVersion})", ImportantTextColor));
                this.hasLoaded = true;
            }

            if (this.canceled)
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $"003 {new TextObject("{=NTP.Sys003}Error loading {DisplayName} : Disabled!").SetTextVariable("DisplayName", new TextObject(DisplayName))} (Assembly v{AssemblyVersion})"
                        ));
        }
        public override void OnAfterGameInitializationFinished(Game game, object starterObject)
        {
            Util.Log.Print($">> [DEBUG] OnAfterGameInitializationFinished: kingdom={Kingdom.All.Count}");
            if (game.GameType is Campaign c)
            {
                System.Diagnostics.Debug.Assert(settings is null);
                var builder = RuntimeSettings.AddSettings(Options!, c.UniqueGameId);
                settings = builder.BuildAsPerSave();
                settings?.Register();
                this.harmony?.PatchAll();
            }
            base.OnAfterGameInitializationFinished(game, starterObject);
        }
        protected override void OnGameStart(Game game, IGameStarter starterObject)
        {
            if (!this.canceled && game.GameType is Campaign)
            {
                Options ??= new();
                ((CampaignGameStarter)starterObject).AddBehavior(new TitleBehavior(Options));
                Util.Log.Print($">> [DEBUG] OnGameStart: kingdom={Kingdom.All.Count}");
            }
            else
            {
                Util.Log.Print($">> [DEBUG] OnGameStart: not Campaign");
            }
            base.OnGameStart(game, starterObject);
        }
        public override void OnGameEnd(Game game)
        {
            if (game.GameType is Campaign)
            {
                Util.Log.Print($">> [DEBUG] OnGameEnd: kingdom={Kingdom.All.Count}");
                var oldSettings = settings;
                oldSettings?.Unregister();
                settings = null;
                Options = null;
                this.harmony?.UnpatchAll();
            }
            else
            {
                Util.Log.Print($">> [DEBUG] OnGameEnd: not Campaign");
            }
            base.OnGameEnd(game);
        }

        private bool hasLoaded;
        private bool canceled;
        private Harmony? harmony;
        public static string ModVersion
        {
            get
            {
                ModuleInfo info = new();
                string ver;
                try
                {
                    info.LoadWithFullPath(Utilities.GetFullModulePath("NobleTitlesPlus"));
                    ver = $"{info.Version.Major}.{info.Version.Minor}.{info.Version.Revision}";
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
        public Options? Options { get; private set; }
    }
}
