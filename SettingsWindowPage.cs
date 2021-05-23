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
        }

    }
}
