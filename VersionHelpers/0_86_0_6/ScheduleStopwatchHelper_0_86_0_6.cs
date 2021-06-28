using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;

// ReSharper disable once CheckNamespace
namespace ScheduleStopwatch.Helper
{
    // ReSharper disable once InconsistentNaming
    public static class ScheduleStopwatchHelper_0_86_0_6
    {
        public static Transform GetStorageNetworkTab()
        {
            LayoutElement tab = Object.Instantiate(R.Game.UI.StorageNetworking.StorageNetworkTab);
            return tab.transform;
        }
    }
}