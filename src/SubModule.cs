using HarmonyLib;
using NobleTitlesPlus.MCMSettings;
using System;
using System.IO;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
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
            Util.EnableLog = true;
            Util.EnableTracer = false;
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            InformationManager.DisplayMessage(new InformationMessage(
                    $"{new TextObject("{=NTP.Sys002}{DisplayName} Loaded").SetTextVariable("DisplayName", new TextObject(DisplayName))} (Assembly v{AssemblyVersion})", ImportantTextColor));
        }
        protected override void OnGameStart(Game game, IGameStarter starterObject)
        {
            if (game.GameType is Campaign)
            {
                ((CampaignGameStarter)starterObject).AddBehavior(new TitleBehavior());
                Util.Log.Print($"OnGameStart: kingdoms={Kingdom.All.Count}", LogCategory.Debug);
            }
            else
            {
                Util.Log.Print($"OnGameStart: not Campaign", LogCategory.Debug);
            }
            base.OnGameStart(game, starterObject);
        }
        public override void OnGameLoaded(Game game, object initializerObject)
        {
            Util.Log.Print($"OnGameLoaded: kingdoms={Kingdom.All.Count}", LogCategory.Debug);
            base.OnGameLoaded(game, initializerObject);
        }
        public override void OnGameInitializationFinished(Game game)
        {
            Util.Log.Print($"OnGameInitializationFinished: kingdoms={Kingdom.All.Count}", LogCategory.Debug);
            base.OnGameInitializationFinished(game);
        }
        public override void OnAfterGameInitializationFinished(Game game, object starterObject)
        {
            // minor factions don't seem to be initialized when OnGameInitializationFinished. (why??) So we need initialize here.
            Util.Log.Print($"OnAfterGameInitializationFinished: kingdoms={Kingdom.All.Count}", LogCategory.Debug);
            if (game.GameType is Campaign c)
            {
                try
                {
                    MCMRuntimeSettings.Instance = new(c.UniqueGameId);
                    Util.Log.Print($"unique game id={c.UniqueGameId}");
                }
                catch (Exception e)
                {
                    Util.Log.Print($"At Constructor: {e.Message}\nStackTrace:{e.StackTrace}");
                    throw new(e.Message, e.InnerException);
                }
                try
                {
                    MCMRuntimeSettings.Instance.InitializeMCMSettings();
                }
                catch (Exception e)
                {
                    Util.Log.Print($"OnAfterGameInitializationFinished: {e.Message}\nStackTrace:{e.StackTrace}", LogCategory.Error);
                    throw new(e.Message, e.InnerException);
                }
                MCMRuntimeSettings.Instance.InitializeNomenclaturaOnGameStart();
                harmony.PatchCategory("NameChangerCore");
                harmony.PatchCategory("Conversation");
                harmony.PatchCategory("PartyPopUp");
                harmony.PatchCategory("SettlementPanel");
                harmony.PatchCategory("Encyclopedia");
                Util.Log.Print($"harmony patched", LogCategory.Debug);
                harmony.PatchCategory("Why");
            }
            base.OnAfterGameInitializationFinished(game, starterObject);
        }
        public override void OnGameEnd(Game game)
        {
            Util.Log.Print($"At OnGameEnd: kingdoms={Kingdom.All.Count}", LogCategory.Debug);
            if (game.GameType is Campaign)
            {
                MCMRuntimeSettings.Instance?.Clear();
            }
            else
            {
                Util.Log.Print($"OnGameEnd: not Campaign", LogCategory.Debug);
            }
            // harmony.UnpatchAll(); // why UnpatchAll doesn't unpatch all??
            harmony.UnpatchCategory("NameChangerCore");
            harmony.UnpatchCategory("Conversation");
            harmony.UnpatchCategory("PartyPopUp");
            harmony.UnpatchCategory("SettlementPanel");
            harmony.UnpatchCategory("Encyclopedia");
            Util.Log.Print($"harmony unpatched", LogCategory.Debug);
            base.OnGameEnd(game);
        }

        internal static Harmony harmony = new(HarmonyDomain);
        public static string ModVersion
        {
            get
            {
                ModuleInfo info = new();
                string ver;
                try
                {
                    Util.Log.Print($"modversion={ModuleHelper.GetModuleFullPath("NobleTitlesPlus")}");
                    info.LoadWithFullPath(ModuleHelper.GetModuleFullPath("NobleTitlesPlus"));
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
        public static readonly Version assemblyVersion = typeof(MCMRuntimeSettings).Assembly.GetName().Version;
        public const string Name = "NobleTitlePlus";
        public const string DisplayName = "Noble Titles Plus";
        public static readonly string modFolderName = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Name;
        public const string HarmonyDomain = "com.skatagiri.bannerlord.NobleTitlePlus";
        internal static readonly Color ImportantTextColor = Color.FromUint(0x00F16D26); // orange
    }
}
