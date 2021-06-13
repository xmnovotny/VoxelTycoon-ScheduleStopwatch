using System;
using System.Collections.Generic;
using VoxelTycoon;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    public partial class VehicleScheduleCapacity
    {
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
            return TotalTransfers?.Transfers;
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

        public TransfersPerStationCont GetRouteTransfersPerStation(bool skipForOnlyOneVehicle = true)
        {
            if (!HasValidData || VehicleSchedule.Vehicle.Route != null)
            {
                TransfersPerStationCont totalTransfers = new TransfersPerStationCont();
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
                        totalTransfers.Add(vehicleData.Capacity.GetTransfersPerStation(), mult);
                    }
                }

                return totalTransfers.AsReadonly();
            }
            return null;
        }

        public IReadOnlyDictionary<Item, int> GetRouteTaskTransfers(RootTask task, bool skipForOnlyOneVehicle = true)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }
            if (!HasValidData || VehicleSchedule.Vehicle.Route != null)
            {
                TaskTransfers routeTransfers = new TaskTransfers();
                VehicleRoute route = VehicleSchedule.Vehicle.Route;
                if (skipForOnlyOneVehicle && route.Vehicles.Count == 1)
                {
                    return null;
                }
                int taskIndex = task.GetIndex();
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
                        routeTransfers.Add(vehicleData.Capacity.GetTransfers(vehicle.Schedule.GetTasks()[taskIndex]), mult);
                    }
                }

                return routeTransfers.Transfers;
            }
            return null;
        }

        public TransfersPerStationCont GetTransfersPerStation()
        {
            return TransfersPerStation.AsReadonly();
        }
        public TransfersPerStationCont GetTransfersPerStation(int unitIndex)
        {
            Invalidate();
            if (_transfPerStationPerUnit == null)
            {
                _transfPerStationPerUnit = new Dictionary<int, TransfersPerStationCont>();
            }
            if (!_transfPerStationPerUnit.TryGetValue(unitIndex, out TransfersPerStationCont result))
            {
                if (!TransfPerUnit.TryGetValue(unitIndex, out Dictionary<RootTask, TaskTransfers> transfers)) {
                    transfers = null;
                }
                result = new TransfersPerStationCont(transfers);
                _transfPerStationPerUnit.Add(unitIndex, result);
            }
            return result.AsReadonly();
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
                _transfPerStation = null;
                _storages = null;
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
        private bool Transfer(TransferTask transferTask, Dictionary<VehicleUnit, StorageState> storages, ref TaskTransfers taskTransfers, ref TaskTransfers[] taskTransfersPerUnit, bool calculateTransfer = false, bool calculateTransferPerUnit = false)
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
                        if (calculateTransferPerUnit)
                        {
                            int unitIndex = targetUnit.Vehicle.Units.IndexOf(targetUnit);
                            if (taskTransfersPerUnit == null)
                            {
                                taskTransfersPerUnit = new TaskTransfers[targetUnit.Vehicle.Units.Count];
                            }
                            if (taskTransfersPerUnit[unitIndex] == null) {
                                taskTransfersPerUnit[unitIndex] = new TaskTransfers();
                            }
                            taskTransfersPerUnit[unitIndex].Add(storage.storage.Item, newCount - storage.count);
                        }
                        storage.count = newCount;
                        storages[targetUnit] = storage;
                    }
                }
            }
            return true;
        }

        private Dictionary<VehicleUnit, StorageState> ActualStorages
        {
            get
            {
                if (_storages == null)
                {
                    Vehicle vehicle = VehicleSchedule.Vehicle;
                    ImmutableList<VehicleUnit> units = vehicle.Units;
                    int unitsCount = units.Count;

                    _storages = new Dictionary<VehicleUnit, StorageState>(unitsCount);
                    for (var i = 0; i < unitsCount; i++)
                    {
                        _storages.Add(units[i], new StorageState(units[i].Storage));
                    }
                }
                return _storages;
            }
        }

        private bool ProcessSchedule(Dictionary<VehicleUnit, StorageState> storages, bool onlyRefit, Dictionary<RootTask, TaskTransfers> transfers = null, Dictionary<int, Dictionary<RootTask, TaskTransfers>> transfersPerUnit = null)
        {
            ImmutableList<RootTask> tasks = VehicleSchedule.GetTasks();
            int tasksCount = tasks.Count;
            for (int i = 0; i < tasksCount; i++)
            {
                RootTask task = tasks[i];
                ImmutableList<SubTask> subTasks = task.GetSubTasks();
                int subTaskCount = subTasks.Count;
                TaskTransfers transfer = null;
                TaskTransfers[] transferPerUnit = null;

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
                        if (!Transfer(transferTask, storages, ref transfer, ref transferPerUnit, transfers != null, transfersPerUnit != null))
                        {
                            return false;
                        }
                    }
                }

                if (transfer != null)
                {
                    transfers.Add(task, transfer);
                }
                if (transferPerUnit != null && transferPerUnit.Length > 0)
                {
                    for(int j = 0; j < transferPerUnit.Length; j++)
                    {
                        TaskTransfers unitTransfer = transferPerUnit[j];
                        if (unitTransfer != null)
                        {
                            if (!transfersPerUnit.TryGetValue(j, out Dictionary<RootTask, TaskTransfers> unitTransfers))
                            {
                                unitTransfers = new Dictionary<RootTask, TaskTransfers>();
                                transfersPerUnit.Add(j, unitTransfers);
                            }

                            if (unitTransfers.TryGetValue(task, out TaskTransfers addedUnitTransfers)) {
                                addedUnitTransfers.Add(unitTransfer);
                            } else
                            {
                                unitTransfers.Add(task, unitTransfer);
                            }
                        }
                    }
                }
            }
            return true;
        }

        private void BuildTaskTransfers()
        {
            _transfers.Clear();
            _hasValidData = false;
            Dictionary<VehicleUnit, StorageState> storages = ActualStorages;
            if (!ProcessSchedule(storages, true))  //find start state of storages
            {
                return; //some refit is set to auto
            }
            if (!ProcessSchedule(storages, false)) //find start state of loaded items
                return;  //some vehicle unit has stroage setted to auto and there is no refit in the schedule for set it to the specific item
            ProcessSchedule(storages, false, _transfers); //finally simulate counts loaded and unloaded for each task
            _hasValidData = true;
        }
        private void BuildTransfersPerUnit()
        {
            if (_hasValidData)
            {
                _transfPerUnit = new Dictionary<int, Dictionary<RootTask, TaskTransfers>>();
                ProcessSchedule(ActualStorages, false, null, _transfPerUnit); //finally simulate counts loaded and unloaded for each task
            }
        }

        private void OnVehicleUnitStorageChange(object _, Storage __, Storage ___)
        {
            if (!((VehicleSchedule.CurrentTask as RootTask)?.CurrentSubTask is RefitTask))
            {
                MarkDirty();
                OnDataChanged();
            }
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

        private TransfersPerStationCont TransfersPerStation
        {
            get
            {
                Invalidate();
                if (_transfPerStation == null && HasValidData)
                {
                    _transfPerStation = new TransfersPerStationCont(_transfers);
                }
                return _transfPerStation;
            }
        }

        private Dictionary<int, Dictionary<RootTask, TaskTransfers>> TransfPerUnit
        {
            get
            {
                Invalidate();
                if (_hasValidData && _transfPerUnit == null)
                {
                    BuildTransfersPerUnit();
                }
                return _transfPerUnit;
            }
        }

        private readonly Dictionary<RootTask, TaskTransfers> _transfers = new Dictionary<RootTask, TaskTransfers>();
        private TaskTransfers _totalTransfers;
        private TransfersPerStationCont _transfPerStation;
        private Dictionary<VehicleUnit, StorageState> _storages;
        private Dictionary<int, Dictionary<RootTask, TaskTransfers>> _transfPerUnit; //key = vehicle unit index
        private Dictionary<int, TransfersPerStationCont> _transfPerStationPerUnit; //key = vehicle unit index

        private bool _dirty = true;
        private bool _hasValidData = false;

    }
}
