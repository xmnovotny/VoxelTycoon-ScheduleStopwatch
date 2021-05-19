using ModSettings;
using VoxelTycoon.Game.UI;

namespace ScheduleStopwatch
{
    public class SettingsWindowPage : ModSettingsWindowPage
    {
        protected override void InitializeInternal(SettingsControl settingsControl)
        {
            var settings = Settings.Current;
            settingsControl.AddToggle("Display time for individual tasks in the schedule", null, settings.ShowIndividualTaskTimes, delegate ()
            {
                settings.ShowIndividualTaskTimes = true;
            }, delegate ()
            {
                settings.ShowIndividualTaskTimes = false;
            });

            settingsControl.AddToggle("Display total time in the vehicle details window", null, settings.ShowScheduleTotalTime, delegate ()
            {
                settings.ShowScheduleTotalTime = true;
            }, delegate ()
            {
                settings.ShowScheduleTotalTime = false;
            });
        }

    }
}
