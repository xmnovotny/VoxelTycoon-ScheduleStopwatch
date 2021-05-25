using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using VoxelTycoon;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;
using XMNUtils;
using static ScheduleStopwatch.TaskDurationDataSet;

namespace ScheduleStopwatch
{
    public partial class VehicleScheduleData
    {

        public Vehicle Vehicle { get; private set; }

        private Measurement _measurement;
        private bool _measurementInvalidated;

        private TaskDurationDataSet _travelData;
        private TaskDurationDataSet _stationLoadingData;
        private Action<VehicleScheduleData, RootTask> dataChanged; //called for both own data change data change from another vehicle in the route
        private Action<VehicleScheduleData> ownDataChanged; //called only for own data change (called before dataChanged and taskDataChanged)
        private Dictionary<RootTask, Action<VehicleScheduleData, RootTask>> _taskDataChanged = new Dictionary<RootTask, Action<VehicleScheduleData, RootTask>>(); //events for one task, called for both own data change data change from another vehicle in the route
        private bool _isDirty = true;
        private TimeSpan? _totalTravelAverage;
        private TimeSpan? _totalStationLoadingAverage;
        private TimeSpan? _totalAverage;
        private Snapshot _lastSnapshot;
        private VehicleScheduleCapacity _capacity;

        public TimeSpan? ScheduleTravelAvereageDuration
        {
            get
            {
                Invalidate();
                return _totalTravelAverage;
            }
        }
        public TimeSpan? ScheduleStationLoadingAvereageDuration
        {
            get
            {
                Invalidate();
                return _totalStationLoadingAverage;
            }
        }
        public TimeSpan? ScheduleAvereageDuration
        {
            get
            {
                Invalidate();
                return _totalAverage;
            }
        }

        public float? ScheduleMonthlyMultiplier
        {
            get
            {
                Invalidate();
                if (_totalAverage != null)
                {
                    return (Convert.ToSingle((30 * 86400) / _totalAverage.Value.TotalSeconds));
                }

                return null;
            }
        }

        public VehicleScheduleCapacity Capacity
        {
            get
            {
                if (_capacity == null)
                {
                    _capacity = new VehicleScheduleCapacity(Vehicle.Schedule);
                    _capacity.DataChanged += OnCapacityDataChanged;
                }
                return _capacity;
            }
        }

        public TimeSpan? GetAverageTravelDuration(RootTask task)
        {
            return _travelData.GetAverageDuration(task);
        }

        //gets average travel duration for all tasks, returns null when data for some task is missing
        public TimeSpan? GetAverageTravelDuration()
        {
            return _travelData.GetAverageDuration(_lastSnapshot.GetNonNonstopTasks());
        }

        public TimeSpan? GetAverageStationLoadingDuration(RootTask task)
        {
            return _stationLoadingData.GetAverageDuration(task);
        }

        /* handler will be called only when own schedule data is changed, not data of another vehicles in the same route */
        public void SubscribeTaskDataChanged(RootTask task, Action<VehicleScheduleData, RootTask> handler)
        {
            if (!_taskDataChanged.TryGetValue(task, out Action<VehicleScheduleData, RootTask> akce))
            {
                _taskDataChanged.Add(task, handler);
            }
            else
            {
                akce -= handler;
                akce += handler;
                _taskDataChanged[task] = akce;
            }
        }

        public void UnsubscribeTaskDataChanged(RootTask task, Action<VehicleScheduleData, RootTask> handler)
        {
            if (_taskDataChanged.TryGetValue(task, out Action<VehicleScheduleData, RootTask> akce))
            {
                akce -= handler;
                _taskDataChanged[task] = akce;
            }
        }

        /* handler will be called only when own schedule data is changed, not data of another vehicles in the same route */
        public void SubscribeDataChanged(Action<VehicleScheduleData, RootTask> handler, bool priority = false)
        {
            if (priority)
                dataChanged = handler + dataChanged;
            else 
                dataChanged += handler;
        }

        public void UnsubscribeDataChanged(Action<VehicleScheduleData, RootTask> handler)
        {
            dataChanged -= handler;
        }

        public void SubscribeOwnDataChanged(Action<VehicleScheduleData> handler)
        {
             ownDataChanged += handler;
        }

        public void UnsubscribeOwnDataChanged(Action<VehicleScheduleData> handler)
        {
            ownDataChanged -= handler;
        }

        private void Invalidate()
        {
            if (_isDirty)
            {
                _totalTravelAverage = _travelData.GetAverageDuration(_lastSnapshot.GetNonNonstopTasks());
                _totalStationLoadingAverage = _stationLoadingData.GetAverageDuration(_lastSnapshot.GetNonNonstopTasks());
                _totalAverage = _totalStationLoadingAverage.HasValue && _totalTravelAverage.HasValue ? _totalStationLoadingAverage + _totalTravelAverage : null;
                _isDirty = false;
            }
        }

        //called only when route has more than one vehicle
        public void CallDataChangedEventsForRoute(RootTask task)
        {
            VehicleRoute route = Vehicle.Route;
            if (route?.Vehicles.Count > 1)
            {
                int? taskIndex = task?.GetIndex();
                VehicleScheduleDataManager manager = Manager<VehicleScheduleDataManager>.Current;
                foreach (Vehicle vehicle in route.Vehicles.ToArray())
                {
                    RootTask localTask = taskIndex != null ? vehicle.Schedule.GetTasks()[taskIndex.Value] : null;
                    manager[vehicle]?.CallDataChangedEvents(localTask);
                }
            }
        }

        private void CallDataChangedEvents(RootTask task)
        {
            dataChanged?.Invoke(this, task);
            if (task == null)
            {
                foreach (Action<VehicleScheduleData, RootTask> action in _taskDataChanged.Values)
                {
                    action?.Invoke(this, null);
                }
            }
            else if (_taskDataChanged.TryGetValue(task, out Action<VehicleScheduleData, RootTask> action))
            {
                action?.Invoke(this, task);
            }
        }

        private void OnDataChanged(RootTask task)
        {
            MarkDirty();
            ownDataChanged?.Invoke(this);
            VehicleRoute route = Vehicle.Route;
            if (route?.Vehicles.Count > 1)
            {
                int? taskIndex = task?.GetIndex();
                VehicleScheduleDataManager manager = Manager<VehicleScheduleDataManager>.Current;
                foreach (Vehicle vehicle in route.Vehicles.ToArray())
                {
                    RootTask localTask = taskIndex != null ? vehicle.Schedule.GetTasks()[taskIndex.Value] : null;
                    manager[vehicle]?.CallDataChangedEvents(localTask);
                }
            }
            else
            {
                CallDataChangedEvents(task);
            }
        }

        private void OnCapacityDataChanged(VehicleScheduleCapacity _)
        {
            OnDataChanged(null);
        }

        internal VehicleScheduleData(Vehicle vehicle)
        {
            this.Vehicle = vehicle;
            _travelData = new TaskDurationDataSet();
            _stationLoadingData = new TaskDurationDataSet();
            _isDirty = true;
            _lastSnapshot = new Snapshot(vehicle.Schedule);
        }

        private void OnTravelMeasurementFinish(TravelMeasurement measurement)
        {
            _travelData.Add(measurement.Task, measurement.measuredTime);
//            NotificationUtils.ShowVehicleHint(Vehicle, String.Format("End travel measurement, days: {0} ({1})", measurement.measuredTime.TotalDays.ToString("N1"), GetAverageTravelDuration(measurement.Task).Value.TotalDays.ToString("N1")));
            this.OnDataChanged(measurement.Task);
        }

        private void OnStationLoadingMeasurementFinish(StationLoadingMeasurement measurement)
        {
            _stationLoadingData.Add(measurement.Task, measurement.measuredTime);
//            NotificationUtils.ShowVehicleHint(Vehicle, String.Format("End station loading measurement, hours: {0} ({1})", measurement.measuredTime.TotalHours.ToString("N1"), GetAverageStationLoadingDuration(measurement.Task).Value.TotalHours.ToString("N1")));
            this.OnDataChanged(measurement.Task);
        }

        //this is also called when train is stopped in the station and then started
        internal void OnDestinationReached(VehicleStation station, RootTask task)
        {
            if (_measurement is TravelMeasurement measurement)
            {
                if (task.Behavior == RootTaskBehavior.NonStop)
                {
                    return;
                }
                measurement.Finish(task);
            }

            if (!_measurementInvalidated)
                _measurement = new StationLoadingMeasurement(this, task);
            else
                _measurementInvalidated = false;
        }

        internal void OnStationLeaved(VehicleStation station, RootTask task)
        {
            if (_measurement is TravelMeasurement && task.Behavior == RootTaskBehavior.NonStop)
            {
                return;
            }
            if (_measurement is StationLoadingMeasurement measurement)
            {
                measurement.Finish();
            }
            _measurement = new TravelMeasurement(this, task);
            _measurementInvalidated = false;
        }

        internal void OnMeasurementInvalidated()
        {
            _measurement = null;
            _measurementInvalidated = true;
        }

        internal void OnStationRemoved(VehicleStation station)
        {
            _travelData.OnStationRemoved(station);
            _stationLoadingData.OnStationRemoved(station);
        }

        internal void OnScheduleChanged(RootTask task, bool minorChange)
        {
            Snapshot newSnapshot = new Snapshot(Vehicle.Schedule);
            if (!minorChange)
            {
                var comparsion = _lastSnapshot.CompareWithNewer(newSnapshot);
                if (comparsion.IsDifference) //in comparsion there aren't any new tasks, it is only difference in old tasks
                {
                    var invalidateMeasurement = false;
                    foreach (var removedTask in comparsion.removed)
                    {
                        _travelData.Remove(removedTask);
                        _stationLoadingData.Remove(removedTask);
                        if (_measurement != null && _measurement.Task == removedTask)
                        {
                            invalidateMeasurement = true;
                        }
                    }
                    foreach (var changedTask in comparsion.changed)
                    {
                        _stationLoadingData.Clear(changedTask);
                        if (_measurement is StationLoadingMeasurement measurement && measurement.Task == changedTask)
                        {
                            invalidateMeasurement = true;
                        }
                    }
                    foreach (var travelChangedTask in comparsion.incomingRouteChange)
                    {
                        _travelData.Clear(travelChangedTask);
                        if (_measurement is TravelMeasurement measurement && measurement.Task == travelChangedTask)
                        {
                            invalidateMeasurement = true;
                        }
                    }
                    if (invalidateMeasurement)
                    {
                        OnMeasurementInvalidated();
                    }
                }
                _lastSnapshot = newSnapshot;
                OnDataChanged(task);
            }
            else
            {
                _lastSnapshot = newSnapshot;
            }
            _capacity?.MarkDirty();
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }
        internal void Write(StateBinaryWriter writer)
        {
            _travelData.Write(writer);
            _stationLoadingData.Write(writer);
            writer.WriteBool(_measurement != null);
            if (_measurement != null)
            {
                _measurement.Write(writer);
            }
        }

        internal static VehicleScheduleData Read(StateBinaryReader reader, Vehicle vehicle, byte version)
        {
            VehicleScheduleData result = new VehicleScheduleData(vehicle)
            {
                _travelData = TaskDurationDataSet.Read(reader, vehicle.Schedule, version),
                _stationLoadingData = TaskDurationDataSet.Read(reader, vehicle.Schedule, version)
            };
            if (reader.ReadBool())
            {
                result._measurement = Measurement.Read(reader, vehicle.Schedule, result, version);
            }

            return result;
        }

        internal void OnVehicleRemoved()
        {
        }

        internal void OnVehicleEdited()
        {
            if (_capacity != null)
            {
                _capacity.OnVehicleEdited();
            }
        }
    }
}
