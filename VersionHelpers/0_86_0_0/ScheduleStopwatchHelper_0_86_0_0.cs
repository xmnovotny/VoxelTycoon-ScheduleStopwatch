using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI.StorageNetworking;

// ReSharper disable once CheckNamespace
namespace ScheduleStopwatch.Helper
{
    // ReSharper disable once InconsistentNaming
    public static class ScheduleStopwatchHelper_0_86_0_0
    {
        public static Transform GetStorageNetworkTab()
        {
		    StorageNetworkTab tab = Object.Instantiate<StorageNetworkTab>(R.Game.UI.StorageNetworking.StorageNetworkTab);
		    Transform result = tab.transform;
		    Object.DestroyImmediate(tab);
            return result;
        }
    }
}