using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using VoxelTycoon;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;
using XMNUtils;

namespace ScheduleStopwatch
{
    public class VehicleScheduleCapacity
    {

        public class TaskTransfers
        {
            //transfer per Item ( >0 = loading, <0 = unloading)
            private readonly Dictionary<Item, int> _transfers = new Dictionary<Item, int>();

            public IReadOnlyDictionary<Item, int> Transfers
            {
                get
                {
                    return _transfers;
                }
            }

            public void Add(Item item, int count)
            {
                if (_transfers.ContainsKey(item))
                {
                    _transfers[item] += count;
                }
                else
                {
                    _transfers.Add(item, count);
                }
            }

            public void Add(TaskTransfers transfers, float? multiplier = null)
            {
                Add(transfers._transfers, multiplier);
            }

            public void Add(IReadOnlyDictionary<Item, int> transfers, float? multiplier=null)
            {
                foreach (KeyValuePair<Item, int> transfer in transfers)
                {
                    int value = multiplier != null ? (transfer.Value * multiplier.Value).RoundToInt() : transfer.Value;
                    this.Add(transfer.Key, value);
                }
            }
        }


        public VehicleSchedule VehicleSchedule { get; }
        public event Action<VehicleScheduleCapacity> DataChanged;
        /* capacity and item transfers are valid (=i.e. there is no problem with calculation of that data) */
        public bool HasValidData
        {
            get
            {
                if (!_hasValidData)
                {
                    Invalidate();
                }
                return _hasValidData;
            }
        }

        public VehicleScheduleCapacity(VehicleSchedule vehicleSchedule)
        {
            VehicleSchedule = vehicleSchedule ?? throw new ArgumentNullException(nameof(vehicleSchedule));
            _hasValidData = false;
            SubscribeUnitStorageChange();
        }

        public void MarkDirty()
        {
            _dirty = true;
            _hasValidData = false;
        }

        public IReadOnlyDictionary<Item, int> GetTransfers(RootTask task)
        {
            Invalidate();
            if (_transfers.TryGetValue(task, out TaskTransfers transfer))
            {
                return transfer.Transfers;
            }
            return null;
        }

        /* Returns total of transferred items per schedule (=sum of unloaded items) */
        public IReadOnlyDictionary<Item, int> GetTotalTransfers()
        {
            return TotalTransfers.Transfers;
        }

        public void OnVehicleEdited()
        {
            SubscribeUnitStorageChange();
            MarkDirty();
            OnDataChanged();
        }

        public IReadOnlyDictionary<Item, int> GetRouteTotalTransfers(bool skipForOnlyOneVehicle=true) {
            if (!HasValidData || VehicleSchedule.Vehicle.Route != null)
            {
                TaskTransfers totalTransfers = new TaskTransfers();
                VehicleRoute route = VehicleSchedule.Vehicle.Route;
                if (skipForOnlyOneVehicle && route.Vehicles.Count == 1)
                {
                    return null;
                }
                foreach (Vehicle vehicle in route.Vehicles.ToArray())
                {
                    if (vehicle.IsEnabled)
                    {
                        VehicleScheduleData vehicleData = VehicleScheduleDataManager.Current[vehicle];
                        float? mult;
                        if (vehicleData == null || !vehicleData.Capacity.HasValidData || (mult = vehicleData.ScheduleMonthlyMultiplier) == null)
                        {
                            return null;
                        }
                        totalTransfers.Add(vehicleData.Capacity.TotalTransfers, mult);
                    }
                }

                return totalTransfers.Transfers;
            }
            return null;
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


        private void SubscribeUnitStorageChange()
        {
            foreach (VehicleUnit unit in VehicleSchedule.Vehicle.Units.ToArray())
            {
                unit.StorageChanged -= this.OnVehicleUnitStorageChange;
                unit.StorageChanged += this.OnVehicleUnitStorageChange;
            }
        }

        private void Invalidate()
        {
            if (_dirty)
            {
                _totalTransfers = null;
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
            Item item = refitTask.Item;
            if (item == null)
            {
                //refit to auto = cannot determine begin state
                return false;
            }
            ImmutableUniqueList<VehicleUnit> targetUnits = refitTask.GetTargetUnits();
            int targetUnitsCount = targetUnits.Count;

            StorageManager storageManager = LazyManager<StorageManager>.Current;
            for (var k = 0; k < targetUnitsCount; k++)
            {
                VehicleUnit targetUnit = targetUnits[k];
                storageManager.TryGetStorage(targetUnit.SharedData.AssetId, item, out Storage newStorage);
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
        private bool Transfer(TransferTask transferTask, Dictionary<VehicleUnit, StorageState> storages, ref TaskTransfers taskTransfers, bool calculateTransfer = false)
        {
            ImmutableUniqueList<VehicleUnit> targetUnits = transferTask.GetTargetUnits();
            int targetUnitsCount = targetUnits.Count;

            for (int k = 0; k < targetUnitsCount; k++)
            {
                VehicleUnit targetUnit = targetUnits[k];
                if (storages.TryGetValue(targetUnit, out StorageState storage))
                {
                    if (storage.storage == null)
                    {
                        //autorefitable storage = cannot determine transfer
                        return false;
                    }
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
            return true;
        }

        private Dictionary<VehicleUnit, StorageState> GetActualStorages()
        {
            Vehicle vehicle = VehicleSchedule.Vehicle;
            ImmutableList<VehicleUnit> units = vehicle.Units;
            int unitsCount = units.Count;

            var storages = new Dictionary<VehicleUnit, StorageState>(unitsCount);
            for (var i = 0; i < unitsCount; i++)
            {
                storages.Add(units[i], new StorageState(units[i].Storage));
            }

            return storages;
        }

        private bool ProcessSchedule(Dictionary<VehicleUnit, StorageState> storages, bool onlyRefit, Dictionary<RootTask, TaskTransfers> transfers = null)
        {
            ImmutableList<RootTask> tasks = VehicleSchedule.GetTasks();
            int tasksCount = tasks.Count;
            for (int i = 0; i < tasksCount; i++)
            {
                RootTask task = tasks[i];
                ImmutableList<SubTask> subTasks = task.GetSubTasks();
                int subTaskCount = subTasks.Count;
                TaskTransfers transfer = null;

                for (int j = 0; j < subTaskCount; j++)
                {
                    SubTask subTask = subTasks[j];
                    if (subTask is RefitTask refitTask)
                    {
                        if (!Refit(refitTask, storages))
                        {
                            return false;
                        }
                    } else
                    if (!onlyRefit && subTask is TransferTask transferTask)
                    {
                        if (!Transfer(transferTask, storages, ref transfer, transfers != null))
                        {
                            return false;
                        }
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
            _hasValidData = false;
            Dictionary<VehicleUnit, StorageState> storages = GetActualStorages();
            if (!ProcessSchedule(storages, true))  //find start state of storages
            {
                return; //some refit is set to auto
            }
            if (!ProcessSchedule(storages, false)) //find start state of loaded items
                return;  //some vehicle unit has stroage setted to auto and there is no refit in the schedule for set it to the specific item
            ProcessSchedule(storages, false, _transfers); //finally simulate counts loaded and unloaded for each task
            _hasValidData = true;
        }

        private void OnVehicleUnitStorageChange(object _, Storage __, Storage ___)
        {
            if (!((VehicleSchedule.CurrentTask as RootTask)?.CurrentSubTask is RefitTask))
            {
                MarkDirty();
                OnDataChanged();
                NotificationUtils.ShowVehicleHint(VehicleSchedule.Vehicle, "Storage changed");
            }
        }

        private void OnVehicleConsistItemAdded(VehicleRecipeInstance recipe)
        {
            foreach(VehicleRecipeSectionInstance section in recipe.Sections.ToArray())
            {
                foreach (VehicleUnit unit in section.Units.ToArray())
                {
                    unit.StorageChanged += OnVehicleUnitStorageChange;
                }
            }
            MarkDirty();
            OnDataChanged();
        }

        private void OnVehicleConsistItemRemoved(VehicleRecipeInstance _)
        {
            MarkDirty();
            OnDataChanged();
        }

        private void OnDataChanged()
        {
            DataChanged?.Invoke(this);
        }

        private TaskTransfers TotalTransfers
        {
            get
            {
                Invalidate();
                if (_totalTransfers == null && HasValidData)
                {
                    _totalTransfers = new TaskTransfers();
                    foreach (TaskTransfers taskTransfers in _transfers.Values)
                    {
                        foreach (KeyValuePair<Item, int> transfer in taskTransfers.Transfers)
                        {
                            if (transfer.Value < 0)
                            {
                                _totalTransfers.Add(transfer.Key, 0 - transfer.Value);
                            }
                        }
                    }
                }
                return _totalTransfers;
            }
        }

        private readonly Dictionary<RootTask, TaskTransfers> _transfers = new Dictionary<RootTask, TaskTransfers>();
        private TaskTransfers _totalTransfers;

        private bool _dirty = true;
        private bool _hasValidData = false;

    }
}
