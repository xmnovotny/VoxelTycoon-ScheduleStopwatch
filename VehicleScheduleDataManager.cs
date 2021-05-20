using System;
using System.Collections.Generic;
using System.Text;
using VoxelTycoon;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    class VehicleScheduleDataManager
    {
        private static VehicleScheduleDataManager _current;

        private Dictionary<Vehicle, VehicleScheduleData> _vehiclesData = new Dictionary<Vehicle, VehicleScheduleData>();

        private VehicleScheduleDataManager(){
        }

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

        internal void Initialize()
        {
            VehicleScheduleHelper.DestinationReached += OnDestinationReached;
            VehicleScheduleHelper.StationLeaved += OnStationLeaved;
            VehicleScheduleHelper.MeasurementInvalidated += OnMeasurementInvalidated;
            VehicleScheduleHelper.ScheduleChanged += OnScheduleChanged;
            LazyManager<VehicleManager>.Current.VehicleRemoved += OnRemoveVehicle;
            
        }

        internal void Deinitialize()
        {
            VehicleScheduleHelper.DestinationReached -= OnDestinationReached;
            VehicleScheduleHelper.StationLeaved -= OnStationLeaved;
            VehicleScheduleHelper.MeasurementInvalidated -= OnMeasurementInvalidated;
            VehicleScheduleHelper.ScheduleChanged -= OnScheduleChanged;
            LazyManager<VehicleManager>.Current.VehicleRemoved -= OnRemoveVehicle;
            _vehiclesData.Clear();
        }

        private VehicleScheduleData GetOrCreateVehicleScheduleData(Vehicle vehicle)
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

        public static VehicleScheduleDataManager Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new VehicleScheduleDataManager();
                }
                return _current;
            }
        }

        internal void OnRemoveVehicle(Vehicle vehicle)
        {
            _vehiclesData.Remove(vehicle);
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
