using NobleTitlesPlus.MCMSettings;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace NobleTitlesPlus
{
    internal sealed class TitleBehavior : CampaignBehaviorBase
    {
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
            Util.Log.Print($"CampaignBehavior constructor called: kingdom={Kingdom.All.Count}", LogCategory.Debug);
        }
        public override void SyncData(IDataStore dataStore)
        {
        }
        private void OnNewGameCreated(CampaignGameStarter starter)
        {
        }
        private void OnGameLoaded(CampaignGameStarter starter)
        {
        }
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
        }
        private void OnDailyTick()
        {
            MCMRuntimeSettings.Instance?.UpdateNomenclaturaOnDailyTick();
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
