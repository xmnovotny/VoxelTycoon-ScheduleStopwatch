using HarmonyLib;
using VoxelTycoon;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    [HarmonyPatch]
    class RefitTaskTimeCorrection
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RefitTaskDelayedAction), "GetStorageToSet")]
        private static void RefitTaskDelayedAction_GetStorageToSet_pof(RefitTaskDelayedAction __instance, ref Storage __result, VehicleUnit unit)
        {
            if (__result != null && unit.Storage != null && __result.Item == unit.Storage.Item)
            {
                //same strorage = no need to refit
                __result = null;
            }
        }
    }
}
