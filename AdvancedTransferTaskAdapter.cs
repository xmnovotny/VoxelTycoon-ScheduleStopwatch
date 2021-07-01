using System;
using System.Collections.Generic;
using VoxelTycoon;
using VoxelTycoon.AssetManagement;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;
using XMNUtils;


namespace ScheduleStopwatch
{
    using AdvancedTransferTask;
    public class AdvancedTransferTaskAdapter: Manager<AdvancedTransferTaskAdapter>
    {
        private readonly Dictionary<Item, int> _tmpCapacity = new();
        private readonly Dictionary<Item, int> _tmpCapacityPerUnit = new();
        private readonly Dictionary<Item, int> _additionalCapacity = new();
        private readonly Dictionary<Item, int> _tmpUnitsCount = new();

        internal Dictionary<VehicleUnit, int> GetCapacityLimits(TransferTask transferTask, Dictionary<VehicleUnit, VehicleScheduleCapacity.StorageState> storages)
        {
            int? percent = TransferTasksManager.Current.GetTaskPercent(transferTask);
            if (percent == null)
            {
                return null;
            }

            _tmpCapacity.Clear();
            _tmpUnitsCount.Clear();
            _tmpCapacityPerUnit.Clear();
            _additionalCapacity.Clear();

            Dictionary<VehicleUnit, int> result = new();
            ImmutableUniqueList<VehicleUnit> units = transferTask.GetTargetUnits();
            for (int i = 0; i < units.Count; i++)
            {
                Storage storage = storages.TryGetValue(units[i], out VehicleScheduleCapacity.StorageState state) ? state.storage : null;
                if (storage != null)
                {
                    _tmpCapacity.AddIntToDict(storage.Item, storage.Capacity);
                    _tmpUnitsCount.AddIntToDict(storage.Item, 1);
                }
            }

            foreach (KeyValuePair<Item, int> itemUnits in _tmpUnitsCount)
            {
                Item item = itemUnits.Key;
                int capacity = _tmpCapacity[item] = TransferTaskInfo.CalculateFinalCapacity(percent.Value, _tmpCapacity[item]);
                _additionalCapacity[item] = capacity % itemUnits.Value;
                _tmpCapacityPerUnit[item] = capacity / itemUnits.Value;
            }

            foreach (var storageInfo in storages)
            {
                Item item = storageInfo.Value.storage?.Item;
                if (item == null) continue;
                
                int limit = _tmpCapacityPerUnit[item];
                if (_additionalCapacity[item] > 0)
                {
                    _additionalCapacity[item]--;
                    limit++;
                }

                result[storageInfo.Key] = limit;
            }

            _tmpCapacity.Clear();
            _tmpUnitsCount.Clear();
            _tmpCapacityPerUnit.Clear();
            _additionalCapacity.Clear();

            return result;
        }

        internal void SubscribePercentChange(Action<TransferTask> action)
        {
            LazyManager<TransferTasksManager>.Current.SettingsChanged -= action;
            LazyManager<TransferTasksManager>.Current.SettingsChanged += action;
        }

        internal void UnsubscribePercentChange(Action<TransferTask> action)
        {
            LazyManager<TransferTasksManager>.Current.SettingsChanged -= action;
        }
    }
}