using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using VoxelTycoon.Tracks;

namespace ScheduleStopwatch
{
    [HarmonyPatch]
    class VehiclePatch
    {
        private static Dictionary<Vehicle, Action<Vehicle>> _unitsCollectionRebuilt = new Dictionary<Vehicle, Action<Vehicle>>();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Vehicle), "RebuildUnitsCollection")]
        static private void VehicleSchedule_MoveTask_pof(Vehicle __instance)
        {

        }

    }
}
