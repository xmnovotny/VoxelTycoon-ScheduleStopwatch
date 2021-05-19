using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;
using XMNUtils;
using static ScheduleStopwatch.TaskDurationDataSet;

namespace ScheduleStopwatch
{
    public partial class VehicleScheduleData
    {

        private struct TaskSnapshot
        {
            public VehicleStationLocation location;
            public int index, version;
            public bool nonstop;
            public RootTask task;
            public TaskSnapshot(RootTask task, int index)
            {
                this.task = task;
                location = task.Destination.VehicleStationLocation;
                version = VehicleScheduleHelper.GetRootTaskVersion(task);
                nonstop = task.Behavior == RootTaskBehavior.NonStop;
                this.index = index;
            }
        }

        private class SnapshotComparsion
        {
            public HashSet<RootTask> removed;
            public HashSet<RootTask> changed; //change in the RootTask
            public HashSet<RootTask> incomingRouteChange; //task with singifinant change for measurment travel time from the task before

            public SnapshotComparsion()
            {
                removed = new HashSet<RootTask>();
                changed = new HashSet<RootTask>();
                incomingRouteChange = new HashSet<RootTask>();
            }

            public bool IsDifference
            {
                get
                {
                    return removed.Count > 0 || changed.Count > 0 || incomingRouteChange.Count > 0;
                }
            }
        }

        private class Snapshot
        {
            public ReadOnlyCollection<TaskSnapshot> TaskSnapshots
            {
                get
                {
                    return _taskSnapshots.AsReadOnly();
                }
            }
            public Dictionary<RootTask, int> TaskToIndex
            {
                get
                {
                    if (_taskToIndex == null)
                    {
                        _taskToIndex = new Dictionary<RootTask, int>();
                        foreach (var snapshot in _taskSnapshots)
                        {
                            _taskToIndex.Add(snapshot.task, snapshot.index);
                        }
                    }
                    return _taskToIndex;
                }
            }

            public TaskSnapshot this[int index]
            {
                get
                {
                    if (index<_taskSnapshots.Count)
                    {
                        return _taskSnapshots[index];
                    }

                    throw new IndexOutOfRangeException("Index out of range");
                }
            }
            public int Count
            {
                get
                {
                    return _taskSnapshots.Count;
                }
            }

            private Dictionary<RootTask, int> _taskToIndex;
            private readonly List<TaskSnapshot> _taskSnapshots = new List<TaskSnapshot>();

            public Snapshot(VehicleSchedule schedule)
            {
                var tasks = schedule.GetTasks();
                var count = tasks.Count;
                for (int i = 0; i < count; i++)
                {
                    var task = tasks[i];
                    _taskSnapshots.Add(new TaskSnapshot(task, i));
                }
            }

            public IEnumerable<RootTask> GetNonNonstopTasks()
            {
                foreach(var snapshot in _taskSnapshots)
                {                    
                    if (!snapshot.nonstop)
                    {
                        yield return snapshot.task;
                    }
                }
                yield break;
            }

            public SnapshotComparsion CompareWithNewer(Snapshot newSnapshot)
            {
                var result = new SnapshotComparsion();
                if (_taskSnapshots.Count > 0)
                {
                    var oldTaskToIndex = TaskToIndex;
                    var newTaskToIndex = newSnapshot.TaskToIndex;
                    bool travelChanged = false;
                    int? lastNewIndex = null, expectedNewIndex;
                    RootTask firstNonNonstopInOld = null;

                    if (newTaskToIndex.TryGetValue(_taskSnapshots[_taskSnapshots.Count - 1].task, out var i))
                        lastNewIndex = i;
                     
                    //find missing and changed
                    foreach (var oldSnapshot in _taskSnapshots)
                    {
                        if (lastNewIndex.HasValue)
                            expectedNewIndex = (lastNewIndex.Value + 1) % newSnapshot.Count;
                        else
                            expectedNewIndex = null;

                        if (!oldSnapshot.nonstop && firstNonNonstopInOld == null)
                        {
                            firstNonNonstopInOld = oldSnapshot.task;
                        }

                        if ((!newTaskToIndex.TryGetValue(oldSnapshot.task, out var idx) || newSnapshot[idx].nonstop))
                        {
                            if (!oldSnapshot.nonstop)
                            {
                                result.removed.Add(oldSnapshot.task);
                            }
                            travelChanged = true;
                            lastNewIndex = null;
                        }
                        else
                        {
                            if ((expectedNewIndex.HasValue && expectedNewIndex.Value != idx) || oldSnapshot.nonstop && !newSnapshot[idx].nonstop || newSnapshot[idx].location != oldSnapshot.location)
                            {
                                travelChanged = true;
                                lastNewIndex = null;
                            }
                            if (!oldSnapshot.nonstop)
                            {
                                if (newSnapshot[idx].version != oldSnapshot.version)
                                {
                                    result.changed.Add(oldSnapshot.task);
                                }
                                if (travelChanged == true)
                                {
                                    result.incomingRouteChange.Add(oldSnapshot.task);
                                    travelChanged = newSnapshot[idx].location != oldSnapshot.location;  //when the location is changed, we must delete incoming and outcoming travel times
                                }
                                lastNewIndex = idx;
                            }
                        }
                    }

                    if (travelChanged && !result.incomingRouteChange.Contains(firstNonNonstopInOld))
                    {
                        FileLog.Log("Last element changed");
                        result.incomingRouteChange.Add(firstNonNonstopInOld);
                    }
                }

                return result;
            }
        }

        public Vehicle Vehicle { get; private set; }

        private Measurement _measurement;
        private bool _measurementInvalidated;

        private TaskDurationDataSet _travelData;
        private TaskDurationDataSet _stationLoadingData;
        private Action<VehicleScheduleData, RootTask> dataChanged;
        private bool _isDirty = true;
        private TimeSpan? _totalTravelAverage;
        private TimeSpan? _totalStationLoadingAverage;
        private TimeSpan? _totalAverage;
        private Snapshot _lastSnapshot;

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

        public void SubscribeDataChanged(Action<VehicleScheduleData, RootTask> handler)
        {
            dataChanged += handler;
        }

        public void UnsubscribeDataChanged(Action<VehicleScheduleData, RootTask> handler)
        {
            dataChanged -= handler;
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

        private void OnDataChanged(RootTask task)
        {
            MarkDirty();
            dataChanged?.Invoke(this, task);
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
            NotificationUtils.ShowVehicleHint(Vehicle, String.Format("End travel measurement, days: {0} ({1})", measurement.measuredTime.TotalDays.ToString("N1"), GetAverageTravelDuration(measurement.Task).Value.TotalDays.ToString("N1")));
            this.OnDataChanged(measurement.Task);
        }

        private void OnStationLoadingMeasurementFinish(StationLoadingMeasurement measurement)
        {
            _stationLoadingData.Add(measurement.Task, measurement.measuredTime);
            NotificationUtils.ShowVehicleHint(Vehicle, String.Format("End station loading measurement, hours: {0} ({1})", measurement.measuredTime.TotalHours.ToString("N1"), GetAverageStationLoadingDuration(measurement.Task).Value.TotalHours.ToString("N1")));
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
            StationLoadingMeasurement measurement = _measurement as StationLoadingMeasurement;
            if (measurement != null)
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
            FileLog.Log("OnScheduleChange");
            Snapshot newSnapshot = new Snapshot(Vehicle.Schedule);
            if (!minorChange)
            {
                FileLog.Log("TestChange");
                var comparsion = _lastSnapshot.CompareWithNewer(newSnapshot);
                if (comparsion.IsDifference) //in comparsion there aren't any new tasks, it is only difference in old tasks
                {
                    var invalidateMeasurement = false;
                    FileLog.Log("Changed");
                    foreach (var removedTask in comparsion.removed)
                    {
                        FileLog.Log("Task Removed: " + removedTask.Destination.Name);
                        _travelData.Remove(removedTask);
                        _stationLoadingData.Remove(removedTask);
                        if (_measurement != null && _measurement.Task == removedTask)
                        {
                            invalidateMeasurement = true;
                        }
                    }
                    foreach (var changedTask in comparsion.changed)
                    {
                        FileLog.Log("Task Changed: " + changedTask.Destination.Name);
                        _stationLoadingData.Clear(changedTask);
                        if (_measurement is StationLoadingMeasurement measurement && measurement.Task == changedTask)
                        {
                            invalidateMeasurement = true;
                        }
                    }
                    foreach (var travelChangedTask in comparsion.incomingRouteChange)
                    {
                        FileLog.Log("Incoming Route Changed: " + travelChangedTask.Destination.Name);
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
                FileLog.Log("MinorChange");
                _lastSnapshot = newSnapshot;
            }
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
    }
}
