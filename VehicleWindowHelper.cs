using HarmonyLib;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using ScheduleStopwatch.UI;

namespace ScheduleStopwatch
{
    [HarmonyPatch]
    class VehicleWindowHelper: LazyManager<VehicleWindowHelper>
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VehicleWindowScheduleTab), "Initialize")]
        private static void VehicleWindowScheduleTab_Initialize_prf(VehicleWindowScheduleTab __instance, VehicleWindow window)
        {
            VehicleScheduleData scheduleData = VehicleScheduleDataManager.Current.GetOrCreateVehicleScheduleData(window.Vehicle);
            VehicleScheduleTabExtender tabExt = __instance.gameObject.AddComponent<VehicleScheduleTabExtender>();
            tabExt.Initialize(scheduleData);
        }
    }
}
