using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using VoxelTycoon;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

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
        public void SubscribeDataChanged(Vehicle vehicle, Action<VehicleScheduleData, RootTask> handler)
        {
            VehicleScheduleData data = GetOrCreateVehicleScheduleData(vehicle);
            data.SubscribeDataChanged(handler);
        }

        public void UnsubscribeDataChanged(Vehicle vehicle, Action<VehicleScheduleData, RootTask> handler)
        {
            VehicleScheduleData scheduleData = this[vehicle];
            if (scheduleData != null)
            {
                scheduleData.UnsubscribeDataChanged(handler);
            }
        }

        protected override void OnInitialize()
        {
            VehicleScheduleHelper.Current.DestinationReached += OnDestinationReached;
            VehicleScheduleHelper.Current.StationLeaved += OnStationLeaved;
            VehicleScheduleHelper.Current.MeasurementInvalidated += OnMeasurementInvalidated;
            VehicleScheduleHelper.Current.ScheduleChanged += OnScheduleChanged;
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
