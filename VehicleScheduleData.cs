using System;
using System.Collections.Generic;
using VoxelTycoon;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;
using XMNUtils;
using static ScheduleStopwatch.TaskDurationDataSet;

namespace ScheduleStopwatch
{
    [SchemaVersion(2)]
    public partial class VehicleScheduleData
    {
        public Vehicle Vehicle { get; private set; }

        private Measurement _measurement;
        private bool _measurementInvalidated;

        private TaskTravelDurationDataSet _travelData;
        private TaskDurationDataSet _stationLoadingData;
        private Action<VehicleScheduleData, RootTask> dataChanged; //called for both own data change data change from another vehicle in the route
        private Action<VehicleScheduleData> ownDataChanged; //called only for own data change (called before dataChanged and taskDataChanged)
        private Dictionary<RootTask, Action<VehicleScheduleData, RootTask>> _taskDataChanged = new Dictionary<RootTask, Action<VehicleScheduleData, RootTask>>(); //events for one task, called for both own data change data change from another vehicle in the route
        private bool _isDirty = true;
        private bool _notificationPending = false;
        private DurationData _totalTravelAverage;
        private DurationData _totalStationLoadingAverage;
        private DurationData _totalAverage;
        private float? _averageSpeed; //average speed in m/s
        private Snapshot _lastSnapshot;
        private VehicleScheduleCapacity _capacity;
        private bool _notificationsTurnedOff = false;
        private DurationsPerStationsContainer _durationPerStation; //not for actual values, but for get initial values after schedule change (keeps old deleted values when there are no new data)

        public DurationData ScheduleTravelAvereageDuration
        {
            get
            {
                Invalidate();
                return _totalTravelAverage;
            }
        }
        public DurationData ScheduleStationLoadingAvereageDuration
        {
            get
            {
                Invalidate();
                return _totalStationLoadingAverage;
            }
        }
        public DurationData ScheduleAvereageDuration
        {
            get
            {
                Invalidate();
                return _totalAverage;
            }
        }

        public float? AverageSpeed
        {
            get
            {
                Invalidate();
                return _averageSpeed;
            }
        }

        public float? ScheduleMonthlyMultiplier
        {
            get
            {
                Invalidate();
                if (_totalAverage != null)
                {
                    return (Convert.ToSingle((30 * 86400) / _totalAverage.Duration.TotalSeconds));
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

        /** turns off data change notifications */
        public bool NotificationsTurnedOff
        {
            get
            {
                return _notificationsTurnedOff;
            }
            internal set
            {
                if (_notificationsTurnedOff != value)
                {
                    _notificationsTurnedOff = value;
                    if (!value && _notificationPending)
                    {
                        OnDataChanged(null);
                    }
                }
            }
        }


        public DurationData GetAverageTravelDuration(RootTask task)
        {
            return _travelData.GetAverageDuration(task);
        }

        //gets average travel duration for all tasks, returns null when data for some task is missing
        public DurationData GetAverageTravelDuration()
        {
            return _travelData.GetAverageDuration(_lastSnapshot.GetNonNonstopTasks());
        }

        public DurationData GetAverageStationLoadingDuration(RootTask task)
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

        public IEnumerable<(VehicleStationLocation location, bool nonstop, RootTask task)> GetSnapshotLocationsWithNonstopInfo()
        {
            return _lastSnapshot.GetLocationsWithNonstopInfo();
        }

        private void Invalidate()
        {
            if (_isDirty)
            {
                _totalTravelAverage = _travelData.GetAverageDuration(_lastSnapshot.GetNonNonstopTasks());
                _totalStationLoadingAverage = _stationLoadingData.GetAverageDuration(_lastSnapshot.GetNonNonstopTasks());
                _totalAverage = _totalStationLoadingAverage != null && _totalTravelAverage != null ? _totalStationLoadingAverage + _totalTravelAverage : null;
                _averageSpeed = null;
                float? distance;
                if (_totalTravelAverage != null && (distance = _travelData.GetTravelledDistance(_lastSnapshot.GetNonNonstopTasks())) != null)
                {
                    _averageSpeed = distance.Value / ((float)_totalTravelAverage.Duration.TotalSeconds / TimeManager.GameSecondsPerSecond) * 5f;
                }
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
        public IEnumerable<(RootTask endStationTask, VehicleStationLocation startStation, VehicleStationLocation endStation, List<VehicleStationLocation> nonstopStations)> GetNonNonstopScheduleParts()
        {
            List<VehicleStationLocation> firstNonstopList = new List<VehicleStationLocation>();
            List<VehicleStationLocation> currentNonstopList = new List<VehicleStationLocation>();
            VehicleStationLocation firstNonNonstop = null, lastNonNonstop = null;
            RootTask firstNonNostopTask = null;
            foreach (TaskSnapshot snapshot in _lastSnapshot.TaskSnapshots)
            {
                if (snapshot.nonstop)
                {
                    if (firstNonNonstop == null)
                    {
                        firstNonstopList.Add(snapshot.location);
                    }
                    else
                    {
                        currentNonstopList.Add(snapshot.location);
                    }
                }
                else
                {
                    if (firstNonNonstop == null)
                    {
                        firstNonNonstop = snapshot.location;
                        firstNonNostopTask = snapshot.task;
                    }
                    if (lastNonNonstop != null)
                    {
                        yield return (snapshot.task, lastNonNonstop, snapshot.location, currentNonstopList.Count > 0 ? new List<VehicleStationLocation>(currentNonstopList) : null);
                    }
                    currentNonstopList.Clear();
                    lastNonNonstop = snapshot.location;
                }
            }
            if (firstNonNonstop != null && lastNonNonstop != null)
            {
                yield return (firstNonNostopTask, lastNonNonstop, firstNonNonstop, currentNonstopList.Count > 0 ? new List<VehicleStationLocation>(currentNonstopList) : null);
            }
            yield break;
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

        private void OnDataChanged(RootTask task, bool notifyRoute = true)
        {
            MarkDirty();
            if (NotificationsTurnedOff)
            {
                _notificationPending = true;
                return;
            }
            _notificationPending = false;
            ownDataChanged?.Invoke(this);
            VehicleRoute route = Vehicle.Route;
            if (notifyRoute && route?.Vehicles.Count > 1)
            {
                CallDataChangedEventsForRoute(task);
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
            _travelData = new TaskTravelDurationDataSet();
            _stationLoadingData = new TaskDurationDataSet();
            _isDirty = true;
            _lastSnapshot = new Snapshot(vehicle.Schedule);
        }


        /* Add own average values to the provided vehicle data (skip when own data aren't complete) */
        internal void AddAverageValuesToVehicleData(VehicleScheduleData data)
        {
            if (data.Vehicle.Route != Vehicle.Route)
            {
                throw new InvalidOperationException("Vehicles must have the same route for copy average values");
            }
            if (GetAverageTravelDuration() == null)
            {
                return;
            }
            _travelData.AddAverageValuesToDataSet(data._travelData, data.Vehicle);
            _stationLoadingData.AddAverageValuesToDataSet(data._stationLoadingData, data.Vehicle);
            data.OnDataChanged(null);
        }

        /** calculates the running average of all stored data and sets it as the new single record, marks it for overwrite with any new data and reduce number of elements for calculating running average */
        internal void AdjustDataAfterCopy(int dataBufferSize = 10)
        {
            _travelData.AdjustDataAfterCopy(dataBufferSize);
            _stationLoadingData.AdjustDataAfterCopy(dataBufferSize);
            this.OnDataChanged(null);
        }

        internal void ChangeDataBufferSize(int dataBufferSize)
        {
            _travelData.ChangeBufferSize(dataBufferSize);
            _stationLoadingData.ChangeBufferSize(dataBufferSize);
            this.OnDataChanged(null);
        }

        internal void ClearAllData()
        {
            _travelData.Clear();
            _stationLoadingData.Clear();
            this.OnDataChanged(null);
        }

        private void OnTravelMeasurementFinish(TravelMeasurement measurement)
        {
            _travelData.Add(measurement.Task, measurement.measuredTime, measurement.Distance);
//            NotificationUtils.ShowVehicleHint(Vehicle, String.Format("End travel measurement, days: {0} ({1}), distance: {2}", measurement.measuredTime.TotalDays.ToString("N1"), GetAverageTravelDuration(measurement.Task).Value.TotalDays.ToString("N1"), measurement.Distance != null ? measurement.Distance.Value.ToString("N3") : ""));
            this.OnDataChanged(measurement.Task);
        }

        private void OnStationLoadingMeasurementFinish(StationLoadingMeasurement measurement)
        {
            _stationLoadingData.Add(measurement.Task, measurement.measuredTime);
//            NotificationUtils.ShowVehicleHint(Vehicle, String.Format("End station loading measurement, hours: {0} ({1})", measurement.measuredTime.TotalHours.ToString("N1"), GetAverageStationLoadingDuration(measurement.Task).Value.TotalHours.ToString("N1")));
            this.OnDataChanged(measurement.Task);
        }

        /** tries to fill unknown travel and station loading times from own old values / another schedule part or from global values based on station locations */
        private void FillUnknownTimes()
        {
            foreach ((RootTask endStationTask, VehicleStationLocation startStation, VehicleStationLocation endStation, List<VehicleStationLocation> nonstopStations) in this.GetNonNonstopScheduleParts())
            {
                VehicleScheduleDataManager manager = Manager<VehicleScheduleDataManager>.Current;
                if (GetAverageTravelDuration(endStationTask) == null)
                {
                    TimeSpan? duration = _durationPerStation?.GetTravelTime(startStation, endStation, nonstopStations);
                    if (duration == null)
                    {
                        duration = manager.GetGlobalTravelDuration(startStation, endStation, nonstopStations);
                    }
                    if (duration != null)
                    {
                        _travelData.Add(endStationTask, duration.Value);
                        _travelData.MarkForOverwrite(endStationTask);
                    }
                }
                if (GetAverageStationLoadingDuration(endStationTask) == null)
                {
                    TimeSpan? duration = _durationPerStation?.GetStationTime(endStation);
                    if (duration == null)
                    {
                        duration = manager.GetGlobalStationDuration(endStation);
                    }
                    if (duration != null)
                    {
                        _stationLoadingData.Add(endStationTask, duration.Value);
                        _stationLoadingData.MarkForOverwrite(endStationTask);
                    }
                }
            }
        }

        //this is also called when train is stopped in the station and then started
        internal void OnDestinationReached(VehicleStationLocation station, RootTask task)
        {
            if (Vehicle.Schedule.TraverseOrder == VehicleScheduleTraverseOrder.BackAndForth)
            {
                _measurement = null;
                return;
            }
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

        internal void OnStationLeaved(VehicleStationLocation station, RootTask task)
        {
            if (Vehicle.Schedule.TraverseOrder == VehicleScheduleTraverseOrder.BackAndForth)
            {
                _measurement = null;
                return;
            }
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

        internal void OnStationRemoved(VehicleStationLocation station)
        {
            _travelData.OnStationRemoved(station);
            _stationLoadingData.OnStationRemoved(station);
        }

        internal void OnScheduleChanged(RootTask task, bool minorChange)
        {
            Snapshot newSnapshot = new Snapshot(Vehicle.Schedule);
            SnapshotComparsion comparsion = _lastSnapshot.CompareWithNewer(newSnapshot);
            bool isChanged = comparsion.IsDifference || _lastSnapshot.Count != newSnapshot.Count;

            if (isChanged)
            {
                if (_durationPerStation == null)
                {
                    _durationPerStation = new DurationsPerStationsContainer(this);
                }
                else
                {
                    _durationPerStation.NewTimes(this);
                }
                Manager<VehicleScheduleDataManager>.Current.InvalidateDurationPerStation();
            }

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
                    _stationLoadingData.MarkForOverwrite(changedTask);
                    if (_measurement is StationLoadingMeasurement measurement && measurement.Task == changedTask)
                    {
                        invalidateMeasurement = true;
                    }
                }
                foreach (var travelChangedTask in comparsion.incomingRouteChange)
                {
                    _travelData.Clear(travelChangedTask);
                    if (_measurement is TravelMeasurement)
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
            if (isChanged)
            {
                FillUnknownTimes();
            }
            _capacity?.MarkDirty();
            OnDataChanged(task, false); //do not notify route - when schedule is changed, OnScheduleChanged event will be called for each vehicle in the route
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

        internal static VehicleScheduleData Read(StateBinaryReader reader, Vehicle vehicle)
        {
            VehicleScheduleData result = new VehicleScheduleData(vehicle)
            {
                _travelData = TaskTravelDurationDataSet.Read(reader, vehicle.Schedule),
                _stationLoadingData = TaskDurationDataSet.Read(reader, vehicle.Schedule)
            };
            if (reader.ReadBool())
            {
                result._measurement = Measurement.Read(reader, vehicle.Schedule, result);
            }

            return result;
        }

        internal void OnVehicleRemoved()
        {
        }

        internal void OnVehicleEdited()
        {
            this._travelData.MarkAllForOverwrite();
            this._stationLoadingData.MarkAllForOverwrite();
            MarkDirty();
            if (_capacity != null)
            {
                _capacity.OnVehicleEdited();
            }
        }

    }
}
