using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VoxelTycoon;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

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
                ImmutableList<RootTask> tasks = schedule.GetTasks();
                int count = tasks.Count;
                for (int i = 0; i < count; i++)
                {
                    RootTask task = tasks[i];
                    _taskSnapshots.Add(new TaskSnapshot(task, i));
                }
            }

            public IEnumerable<RootTask> GetNonNonstopTasks()
            {
                foreach(TaskSnapshot snapshot in _taskSnapshots)
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
                SnapshotComparsion result = new SnapshotComparsion();
                if (_taskSnapshots.Count > 0)
                {
                    Dictionary<RootTask, int> oldTaskToIndex = TaskToIndex;
                    Dictionary<RootTask, int> newTaskToIndex = newSnapshot.TaskToIndex;
                    bool travelChanged = false;
                    int? lastNewIndex = null, expectedNewIndex;
                    RootTask firstNonNonstopInOld = null;

                    if (newTaskToIndex.TryGetValue(_taskSnapshots[_taskSnapshots.Count - 1].task, out var i))
                        lastNewIndex = i;
                     
                    //find missing and changed
                    foreach (TaskSnapshot oldSnapshot in _taskSnapshots)
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
                        result.incomingRouteChange.Add(firstNonNonstopInOld);
                    }
                }

                return result;
            }
        }
    }
}
