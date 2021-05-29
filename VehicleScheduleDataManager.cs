using HarmonyLib;
using System;
using System.Collections.Generic;
using VoxelTycoon;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;
using TaskTransfers = ScheduleStopwatch.VehicleScheduleCapacity.TaskTransfers;

namespace ScheduleStopwatch
{
    class VehicleScheduleDataManager: Manager<VehicleScheduleDataManager>
    {
        private Dictionary<Vehicle, VehicleScheduleData> _vehiclesData = new Dictionary<Vehicle, VehicleScheduleData>();

        public VehicleScheduleData this[Vehicle vehicle] {
            get
            {
                return _vehiclesData.TryGetValue(vehicle, out VehicleScheduleData vehicleScheduleData) ? vehicleScheduleData : null;
            }
        }
        public void SubscribeDataChanged(Vehicle vehicle, Action<VehicleScheduleData, RootTask> handler, bool priority = false)
        {
            VehicleScheduleData data = GetOrCreateVehicleScheduleData(vehicle);
            data.SubscribeDataChanged(handler, priority);
        }

        public void UnsubscribeDataChanged(Vehicle vehicle, Action<VehicleScheduleData, RootTask> handler)
        {
            VehicleScheduleData scheduleData = this[vehicle];
            if (scheduleData != null)
            {
                scheduleData.UnsubscribeDataChanged(handler);
            }
        }

        /** start listen events for measuring data */
        public void StartMeasuring()
        {
            VehicleScheduleHelper.Current.DestinationReached -= OnDestinationReached;
            VehicleScheduleHelper.Current.DestinationReached += OnDestinationReached;
            VehicleScheduleHelper.Current.StationLeaved -= OnStationLeaved;
            VehicleScheduleHelper.Current.StationLeaved += OnStationLeaved;
            VehicleScheduleHelper.Current.MeasurementInvalidated -= OnMeasurementInvalidated;
            VehicleScheduleHelper.Current.MeasurementInvalidated += OnMeasurementInvalidated;
            VehicleScheduleHelper.Current.ScheduleChanged -= OnScheduleChanged;
            VehicleScheduleHelper.Current.ScheduleChanged += OnScheduleChanged;
            VehicleScheduleHelper.Current.VehicleRouteChanged -= OnVehicleRouteChanged;
            VehicleScheduleHelper.Current.VehicleRouteChanged += OnVehicleRouteChanged;
        }

        protected override void OnInitialize()
        {
            LazyManager<VehicleManager>.Current.VehicleRemoved += OnRemoveVehicle;
            LazyManager<VehicleManager>.Current.VehicleEdited += OnVehicleEdited;
        }

        public VehicleScheduleData GetOrCreateVehicleScheduleData(Vehicle vehicle)
        {
            if (!_vehiclesData.ContainsKey(vehicle))
            {
                _vehiclesData[vehicle] = new VehicleScheduleData(vehicle);
            }
            return _vehiclesData[vehicle];
        }

        /** copies average values of all vehicles in the route and add it as one-time data (will be overwriten when own data are available) */
        public VehicleScheduleData ReplaceVehicleScheduleDataFromRouteCopy(Vehicle vehicle)
        {
            if (vehicle.Route == null || vehicle.Route.Vehicles.Count <= 1)
            {
                throw new InvalidOperationException("Vehicle route is null or have only one vehicle");
            }

            VehicleScheduleData result = GetOrCreateVehicleScheduleData(vehicle);
            result.NotificationsTurnedOff = true;
            try
            {
                result.ClearAllData();
                result.ChangeDataBufferSize(vehicle.Route.Vehicles.Count);

                foreach (Vehicle currVehicle in vehicle.Route.Vehicles.ToList())
                {
                    if (currVehicle == vehicle)
                        continue;

                    if (!_vehiclesData.TryGetValue(currVehicle, out VehicleScheduleData currData))
                    {
                        continue;
                    }

                    currData.AddAverageValuesToVehicleData(result);
                }
                result.AdjustDataAfterCopy();
            }
            finally
            {
                result.NotificationsTurnedOff = false;
            }

            return result;
        }

        public IReadOnlyDictionary<Item, int> GetStationTaskTransfersSum(ImmutableList<Vehicle> vehicles, VehicleStation station, out bool isIncomplete)
        {
            int count = vehicles.Count;
            TaskTransfers transfersSum = new TaskTransfers();
            isIncomplete = false;
            for (int i = 0; i < vehicles.Count; i++)
            {
                VehicleScheduleData scheduleData = this[vehicles[i]];
                IReadOnlyDictionary<Item, int> transfers = scheduleData?.Capacity.GetTransfersPerStation()[station];
                float? mult;
                if (transfers != null && (mult = scheduleData.ScheduleMonthlyMultiplier) != null)
                {
                    transfersSum.Add(transfers, mult);
                } else
                {
                    isIncomplete = false;
                }
            }
            return transfersSum.Transfers;
        }

        private void OnDestinationReached(Vehicle vehicle, VehicleStation station, RootTask task)
        {
            GetOrCreateVehicleScheduleData(vehicle).OnDestinationReached(station, task);
        }
        private void OnStationLeaved(Vehicle vehicle, VehicleStation station, RootTask task)
        {
            GetOrCreateVehicleScheduleData(vehicle).OnStationLeaved(station, task);
        }
        private void OnScheduleChanged(Vehicle vehicle, RootTask task, bool minorChange)
        {
            GetOrCreateVehicleScheduleData(vehicle).OnScheduleChanged(task, minorChange);
        }

        private void OnMeasurementInvalidated(Vehicle vehicle)
        {
            GetOrCreateVehicleScheduleData(vehicle).OnMeasurementInvalidated();
        }

        private void OnVehicleRouteChanged(Vehicle vehicle, VehicleRoute oldRoute, VehicleRoute newRoute)
        {
            if (newRoute != null && oldRoute != newRoute && newRoute.Vehicles.Count > 1)
            {
                VehicleScheduleData vehicleData = ReplaceVehicleScheduleDataFromRouteCopy(vehicle);
                vehicleData.CallDataChangedEventsForRoute(null);
            }
        }

        internal void OnRemoveVehicle(Vehicle vehicle)
        {
            if (_vehiclesData.TryGetValue(vehicle, out VehicleScheduleData data)) {
                data.OnVehicleRemoved();
            }
            _vehiclesData.Remove(vehicle);
        }

        internal void OnVehicleEdited(Vehicle vehicle)
        {
            if (_vehiclesData.TryGetValue(vehicle, out VehicleScheduleData data))
            {
                data.OnVehicleEdited();
            }
        }

        internal void Write(StateBinaryWriter writer)
        {
            writer.WriteInt(_vehiclesData.Count);
            foreach (KeyValuePair<Vehicle, VehicleScheduleData> pair in _vehiclesData)
            {
                writer.WriteInt(pair.Key.Id);
                pair.Value.Write(writer);
            }
        }

        internal void Read(StateBinaryReader reader, byte version)
        {
            _vehiclesData.Clear();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                int vehicleId = reader.ReadInt();
                Vehicle vehicle = LazyManager<VehicleManager>.Current.FindById(vehicleId);
                _vehiclesData.Add(vehicle, VehicleScheduleData.Read(reader, vehicle, version));
            }
        }

    }
}
