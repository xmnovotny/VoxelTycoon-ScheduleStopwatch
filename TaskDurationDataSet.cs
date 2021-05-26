using System;
using System.Collections.Generic;
using VoxelTycoon;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    public class TaskDurationDataSet
    {
        private readonly int _bufferSize;
        private readonly Dictionary<RootTask, DurationDataSet> _data = new Dictionary<RootTask, DurationDataSet>();

        protected DurationDataSet GetOrCreateDataSetForTask(RootTask task)
        {
            if (!_data.ContainsKey(task))
            {
                _data.Add(task, new DurationDataSet(_bufferSize));
            }

            return _data[task];
        }

        public TaskDurationDataSet(int bufferSize = DurationDataSet.DEFAULT_BUFFER_SIZE)
        {
            this._bufferSize = bufferSize;
        }

        public void Add(RootTask task, TimeSpan duration)
        {
            DurationDataSet dataSet = GetOrCreateDataSetForTask(task);
            dataSet.Add(duration);
        }

        public TimeSpan? GetAverageDuration(RootTask task)
        {
            if (!_data.TryGetValue(task, out DurationDataSet dataSet))
            {
                return null;
            }

            return dataSet.Average;
        }
        //Gets sum of average duration for all tasks in the list. If some task is missing data, returns null
        public TimeSpan? GetAverageDuration(IEnumerable<RootTask> tasks)
        {
            TimeSpan result = default;
            foreach (RootTask task in tasks)
            {
                TimeSpan? duration = GetAverageDuration(task);
                if (!duration.HasValue)
                {
                    return null;
                }
                result += duration.Value;
            }

            return result;
        }

        public void OnStationRemoved(VehicleStation station)
        {
            foreach (RootTask task in _data.Keys)
            {
                if (task.Destination.VehicleStationLocation.VehicleStation == station)
                {
                    _data.Remove(task);
                }
            }
        }

        public void Clear()
        {
            _data.Clear();
        }

        public void Clear(RootTask task)
        {
            if (_data.TryGetValue(task, out DurationDataSet dataSet))
            {
                dataSet.Clear();
            }
        }

        /** marks task data for overwrite with next new data (all old data will be discarded when new data are added) */
        public void MarkForOverwrite(RootTask task)
        {
            if (_data.TryGetValue(task, out DurationDataSet dataSet))
            {
                dataSet.MarkForOverwrite();
            }
        }

        public void Remove(RootTask task)
        {
            _data.Remove(task);
        }

        internal void Write(StateBinaryWriter writer)
        {
            writer.WriteInt(_data.Count);
            foreach (var pair in _data)
            {
                writer.WriteInt(pair.Key.GetIndex());
                pair.Value.Write(writer);
            }
        }
        internal static TaskDurationDataSet Read(StateBinaryReader reader, VehicleSchedule schedule, byte version) 
        {
            TaskDurationDataSet result = new TaskDurationDataSet();
            int count = reader.ReadInt();

            for(int i = 0; i < count; i++)
            {
                int taskIndex = reader.ReadInt();
                RootTask task = schedule.GetTasks()[taskIndex];
                result._data.Add(task, DurationDataSet.Read(reader, version));
            }
            return result;
        }

    }
}
