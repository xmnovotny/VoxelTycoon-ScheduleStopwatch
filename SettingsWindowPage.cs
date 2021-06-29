using ModSettings;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Localization;

namespace ScheduleStopwatch
{
    public class SettingsWindowPage : ModSettingsWindowPage
    {
        protected override void InitializeInternal(SettingsControl settingsControl)
        {
            Settings settings = Settings.Current;
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            settingsControl.AddToggle(locale.GetString("schedule_stopwatch/display_time_for_inidividual_tasks"), null, settings.ShowIndividualTaskTimes, delegate ()
            {
                settings.ShowIndividualTaskTimes = true;
            }, delegate ()
            {
                settings.ShowIndividualTaskTimes = false;
            });

            settingsControl.AddToggle(locale.GetString("schedule_stopwatch/display_totaltime_in_schedule"), null, settings.ShowScheduleTotalTime, delegate ()
            {
                settings.ShowScheduleTotalTime = true;
            }, delegate ()
            {
                settings.ShowScheduleTotalTime = false;
            });

            settingsControl.AddToggle(locale.GetString("schedule_stopwatch/display_total_capacity"), null, settings.ShowTotalTransferCapacity, delegate ()
            {
                settings.ShowTotalTransferCapacity = true;
            }, delegate ()
            {
                settings.ShowTotalTransferCapacity = false;
            });

            settingsControl.AddToggle(locale.GetString("schedule_stopwatch/display_individual_unloading_capacity"), null, settings.ShowIndividualUnloadingCapacity, delegate ()
            {
                settings.ShowIndividualUnloadingCapacity = true;
            }, delegate ()
            {
                settings.ShowIndividualUnloadingCapacity = false;
            });

            settingsControl.AddToggle(locale.GetString("schedule_stopwatch/display_individual_loading_capacity"), null, settings.ShowIndividualLoadingCapacity, delegate ()
            {
                settings.ShowIndividualLoadingCapacity = true;
            }, delegate ()
            {
                settings.ShowIndividualLoadingCapacity = false;
            });

            settingsControl.AddToggle(locale.GetString("schedule_stopwatch/display_station_loading_capacity"), null, settings.ShowStationLoadedItems, delegate ()
            {
                settings.ShowStationLoadedItems = true;
            }, delegate ()
            {
                settings.ShowStationLoadedItems = false;
            });

            settingsControl.AddToggle(locale.GetString("schedule_stopwatch/display_station_unloading_capacity"), null, settings.ShowStationUnloadedItems, delegate ()
            {
                settings.ShowStationUnloadedItems = true;
            }, delegate ()
            {
                settings.ShowStationUnloadedItems = false;
            });

            settingsControl.AddToggle(locale.GetString("schedule_stopwatch/calculate_unserviced_demands"), locale.GetString("schedule_stopwatch/calculate_unserviced_demands_description"), settings.CalculateUnservicedDemands, delegate ()
            {
                settings.CalculateUnservicedDemands = true;
            }, delegate ()
            {
                settings.CalculateUnservicedDemands = false;
            });
        }

    }
}
