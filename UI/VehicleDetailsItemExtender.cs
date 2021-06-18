using HarmonyLib;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Localization;
using VoxelTycoon.Tracks;
using VoxelTycoon.UI;

namespace ScheduleStopwatch.UI
{
    [HarmonyPatch]
    class VehicleDetailsItemExtender
    {
        private static string GetToolTipText(Vehicle vehicle, int unitIndex)
        {
            VehicleScheduleData scheduleData = Manager<VehicleScheduleDataManager>.Current[vehicle];
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            if (scheduleData != null)
            {
                string result = ScheduleCapacityHelper.GetCapacityTooltipText(scheduleData.ScheduleMonthlyMultiplier, null, scheduleData.Capacity.GetTransfersPerStation(unitIndex), scheduleData.IsInacurate);
                if (result == "")
                {
                    if (scheduleData.ScheduleMonthlyMultiplier != null)
                    {
                        result = locale.GetString("schedule_stopwatch/no_transfers");
                    } else
                    {
                        result = locale.GetString("schedule_stopwatch/no_transfers_data");
                    }
                }
                return result;
            }
            return locale.GetString("schedule_stopwatch/no_transfers_data");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindowDetailsTabBodyItem), "Initialize")]
        private static void VehicleWindowScheduleTabSeparatorView_Initialize_pof(VehicleWindowDetailsTabBodyItem __instance, int index, VehicleUnit vehicleUnit)
        {
            VehicleScheduleData scheduleData = Manager<VehicleScheduleDataManager>.Current[vehicleUnit.Vehicle];
            if (scheduleData != null)
            {
                Tooltip.For(__instance, () => GetToolTipText(vehicleUnit.Vehicle, index), 0);
            }
        }
    }
}
