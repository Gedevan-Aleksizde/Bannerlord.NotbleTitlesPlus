using NobleTitlesPlus.MCMSettings;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace NobleTitlesPlus
{
    internal sealed class TitleBehavior : CampaignBehaviorBase
    {
        // public static Options Options { get; set; }
        // public static Nomenclatura nomenclatura = new();
        // private Harmony harmony;
        public override void RegisterEvents()
        {
            // TODO: remove unused event
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            // CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, (a, b) => this.UpdateArmyNames());
            // CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, () => UpdateArmyNames());          
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnGameLoaded));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));
        }
        public TitleBehavior()
        {
            Util.Log.Print($">> [DEBUG] CampaignBehavior constructor called: kingdom={Kingdom.All.Count}");
        }
        public override void SyncData(IDataStore dataStore)
        {
        }
        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            // RuntimeSettings.Instance?.InitializeOnGameStart();
            /*
            Util.Log.Print($">> [DEBUG] OnNewGameCreated: kingdom={Kingdom.All.Count}");
            Options.TitleSet.Initialize();
            nomenclatura.UpdateAll(Options.FixShokuhoClanName);
            if (Options.VerboseLog) Util.Log.Print($">> [INFO] Starting new campaign on {SubModule.Name}");
            */
        }
        private void OnGameLoaded(CampaignGameStarter starter)
        {
            // RuntimeSettings.Instance?.UpdateOnGameLoaded();
            /*
            Util.Log.Print($">> [DEBUG] OnGameLoaded: kingdom={Kingdom.All.Count}");
            if (Options.VerboseLog) Util.Log.Print(">> [INFO] Loading campaign");
            nomenclatura.UpdateAll(Options.FixShokuhoClanName);
            if (Options.VerboseLog) Util.Log.Print($">> [INFO] Loading campaign on {SubModule.Name}");
            */
        }
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            Util.Log.Print($"OnSessionLaunched: kingdom={Kingdom.All.Count}");
        }
        private void OnDailyTick()
        {
            MCMRuntimeSettings.Instance?.UpdateNomenclaturaOnDailyTick();
            /*
            IEnumerable<Kingdom> survivingImperial = Kingdom.All.Where(x => !x.IsEliminated && ImperialFactions.Contains(x.StringId));
            if (survivingImperial.Count() == 1)
            {
                nomenclatura.OverwriteWithImperialFormats(survivingImperial.First());
            }
            nomenclatura.UpdateAll(Options.FixShokuhoClanName);
            */
        }
        public void UpdateArmyNames()
        {
            // TODO
            if (Campaign.Current != null)
            {
                foreach (MobileParty mp in MobileParty.All)
                {
                    mp.Army?.UpdateName();
                }
            }
        }
    }
}
