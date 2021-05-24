using HarmonyLib;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using UnityEngine.UI;
using VoxelTycoon.UI.Controls;
using System.Collections.Generic;
using VoxelTycoon.Tracks.Tasks;
using System;
using VoxelTycoon.UI;
using VoxelTycoon.Localization;
using VoxelTycoon.Game.UI.VehicleUnitPickerWindowViews;
using ScheduleStopwatch.UI;

namespace ScheduleStopwatch
{
    [HarmonyPatch]
    class VehicleWindowHelper: LazyManager<VehicleWindowHelper>
    {
        
        #region HARMONY
        #region VehicleWindow
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VehicleWindowScheduleTab), "Initialize")]
        private static void VehicleWindowScheduleTab_Initialize_prf(VehicleWindowScheduleTab __instance, VehicleWindow window)
        {
            VehicleScheduleData scheduleData = VehicleScheduleDataManager.Current.GetOrCreateVehicleScheduleData(window.Vehicle);
            VehicleScheduleTabExtender tabExt = __instance.gameObject.AddComponent<VehicleScheduleTabExtender>();
            tabExt.Initialize(scheduleData);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindow), "OnClose")]
        private static void VehicleWindow_OnClose_pof(VehicleWindow __instance)
        {
            VehicleScheduleData scheduleData = VehicleScheduleDataManager.Current.GetOrCreateVehicleScheduleData(__instance.Vehicle);
            VehicleScheduleTabExtender tabExt = __instance.gameObject.GetComponentInChildren<VehicleScheduleTabExtender>();
        }
        #endregion
        #endregion
    }
}
