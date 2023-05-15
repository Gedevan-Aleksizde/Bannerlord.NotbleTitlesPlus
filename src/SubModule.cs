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

namespace NobleTitlesPlus
{
    public class SubModule : MBSubModuleBase
    {
        /* Semantic Versioning (https://semver.org): */
        // TODO: Why we can't extract it from assembly info or submodule.xml?
        // public const int SemVerMajor = 1;
        // public const int SemVerMinor = 2;
        // public const int SemVerPatch = 0;
        public static readonly string? SemVerSpecial = null;
        private static readonly string SemVerEnd = SemVerSpecial is not null ? "-" + SemVerSpecial : string.Empty;
        // public static readonly string Version = $"{SemVerMajor}.{SemVerMinor}.{SemVerPatch}{SemVerEnd}";
        public static readonly string Name = typeof(SubModule).Namespace; // why we need write again?
        public static readonly string modFolderName = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Name;
        public static readonly string DisplayName = "{=NTP.Sys001}Noble Titles Plus"; // why we need write again?
        public static readonly string HarmonyDomain = "com.skatagiri.bannerlord" + Name.ToLower();
        internal static readonly Color ImportantTextColor = Color.FromUint(0x00F16D26); // orange
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
                    $"{new TextObject("{=NTP.Sys002}{DisplayName} Loaded").SetTextVariable("DisplayName", new TextObject(DisplayName))}", ImportantTextColor));
                hasLoaded = true;
            }

            if (canceled)
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $"{new TextObject("{=NTP.Sys003}Error loading {DisplayName}: Disabled!").SetTextVariable("DisplayName", new TextObject(DisplayName))}"
                        ));
        }
        protected override void OnGameStart(Game game, IGameStarter starterObject)
        {
            base.OnGameStart(game, starterObject);

            if (!canceled && game.GameType is Campaign)
                ((CampaignGameStarter)starterObject).AddBehavior(new TitleBehavior());
        }
        private bool hasLoaded;
        private bool canceled;
    }
}
