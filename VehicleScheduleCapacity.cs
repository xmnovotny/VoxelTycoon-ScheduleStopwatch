using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using VoxelTycoon;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    class VehicleScheduleCapacity
    {

        private class TaskTransfers
        {
            //transfer per Item ( >0 = loading, <0 = unloading)
            public Dictionary<Item, int> transfers = new Dictionary<Item, int>();

            internal void Add(Item item, int count)
            {
                if (transfers.ContainsKey(item))
                {
                    transfers[item] += count;
                } else
                {
                    transfers.Add(item, count);
                }
            }

        }

        private struct StorageState
        {
            public Storage storage;
            public int count;

            public StorageState(Storage storage, int count = 0)
            {
                this.storage = storage;
                this.count = count;
            }
        }

        public VehicleSchedule VehicleSchedule { get; }

        public VehicleScheduleCapacity(VehicleSchedule vehicleSchedule)
        {
            VehicleSchedule = vehicleSchedule ?? throw new ArgumentNullException(nameof(vehicleSchedule));
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public IReadOnlyDictionary<Item, int> GetTransfers(RootTask task)
        {
            Invalidate();
            if (_transfers.TryGetValue(task, out var transfer))
            {
                return transfer.transfers;
            }
            return null;
        }

        private void Invalidate()
        {
            if (_dirty)
            {
                BuildTaskTransfers();
                _dirty = false;
            }
        }

        /**
         * Simulate refit task on provided storages.
         * Returns false when the refit task has no item to refit (=Auto refit)
         */
        private bool Refit(RefitTask refitTask, Dictionary<VehicleUnit, StorageState> storages)
        {
            var item = refitTask.Item;
            if (item == null)
            {
                //refit to auto = cannot determine begin state
                return false;
            }
            var targetUnits = refitTask.GetTargetUnits();
            var targetUnitsCount = targetUnits.Count;

            var storageManager = LazyManager<StorageManager>.Current;
            for (var k = 0; k < targetUnitsCount; k++)
            {
                var targetUnit = targetUnits[k];
                storageManager.TryGetStorage(targetUnit.SharedData.AssetId, item, out var newStorage);
                if (newStorage != null && storages.ContainsKey(targetUnit) && storages[targetUnit].storage.Item != newStorage.Item)
                {
                    storages[targetUnit] = new StorageState(newStorage);
                }
            }

            return true;
        }

        /**
         * Simulate transfer task on provided storages.
         */
        private void Transfer(TransferTask transferTask, Dictionary<VehicleUnit, StorageState> storages, ref TaskTransfers taskTransfers, bool calculateTransfer = false)
        {
            var targetUnits = transferTask.GetTargetUnits();
            var targetUnitsCount = targetUnits.Count;

            for (var k = 0; k < targetUnitsCount; k++)
            {
                var targetUnit = targetUnits[k];
                if (storages.TryGetValue(targetUnit, out var storage))
                {
                    int newCount = transferTask is LoadTask ? storage.storage.Capacity : 0;
                    if (storage.count != newCount)
                    {
                        if (calculateTransfer)
                        {
                            if (taskTransfers == null)
                            {
                                taskTransfers = new TaskTransfers();
                            }
                            taskTransfers.Add(storage.storage.Item, newCount - storage.count);
                        }
                        storage.count = newCount;
                        storages[targetUnit] = storage;
                    }
                }
            }
        }

        private Dictionary<VehicleUnit, StorageState> GetActualStorages()
        {
            var vehicle = VehicleSchedule.Vehicle;
            var units = vehicle.Units;
            var unitsCount = units.Count;

            var storages = new Dictionary<VehicleUnit, StorageState>(unitsCount);
            for (var i = 0; i < unitsCount; i++)
            {
                storages.Add(units[i], new StorageState(units[i].Storage));
            }

            return storages;
        }

        private bool ProcessSchedule(Dictionary<VehicleUnit, StorageState> storages, bool onlyRefit, Dictionary<RootTask, TaskTransfers> transfers = null)
        {
            var tasks = VehicleSchedule.GetTasks();
            var tasksCount = tasks.Count;
            for (var i = 0; i < tasksCount; i++)
            {
                var task = tasks[i];
                var subTasks = task.GetSubTasks();
                var subTaskCount = subTasks.Count;
                TaskTransfers transfer = null;

                for (var j = 0; j < subTaskCount; j++)
                {
                    var subTask = subTasks[j];
                    if (subTask is RefitTask refitTask)
                    {
                        if (!Refit(refitTask, storages))
                        {
                            return false;
                        }
                    } else
                    if (!onlyRefit && subTask is TransferTask transferTask)
                    {
                        Transfer(transferTask, storages, ref transfer, transfers != null);
                    }
                }

                if (transfer != null)
                {
                    transfers.Add(task, transfer);
                }
            }
            return true;
        }

        private void BuildTaskTransfers()
        {
            _transfers.Clear();
            var storages = GetActualStorages();
            if (!ProcessSchedule(storages, true))  //find start state of storages
            {
                return;
            }
            ProcessSchedule(storages, false); //find start state of loaded items
            ProcessSchedule(storages, false, _transfers); //finally simulate counts loaded and unloaded for each task
        }

        private Dictionary<RootTask, TaskTransfers> _transfers = new Dictionary<RootTask, TaskTransfers>();

        private bool _dirty = true;
    }
}
