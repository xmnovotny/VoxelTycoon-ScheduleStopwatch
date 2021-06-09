using System;
using System.Collections.Generic;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    [SchemaVersion(2)]
    public class TaskTravelDurationDataSet: TaskDurationDataSet
    {
        private readonly Dictionary<RootTask, float> _distanceData = new Dictionary<RootTask, float>();

        public TaskTravelDurationDataSet(int bufferSize = 10) : base(bufferSize)
        {
        }

        public void Add(RootTask task, TimeSpan duration, float? distance)
        {
            base.Add(task, duration);
            if (distance != null)
            {
                _distanceData[task] = distance.Value;
            }
        }

        public new TaskTravelDurationDataSet GetCopyWithAverageValues(Vehicle newVehicle, int dataBufferSize = 10)
        {
            TaskTravelDurationDataSet result = new TaskTravelDurationDataSet(dataBufferSize);
            CopyAverageValues(result, newVehicle);
            CopyDistanceData(result, newVehicle);
            return result;
        }

        public float? GetTravelledDistance(IEnumerable<RootTask> tasks)
        {
            float result = default;
            foreach (RootTask task in tasks)
            {
                if (!_distanceData.TryGetValue(task, out float distance))
                {
                    return null;
                }
                result += distance;
            }

            return result;
        }

        public override void Clear()
        {
            base.Clear();
            _distanceData.Clear();
        }

        public override void Clear(RootTask task)
        {
            base.Clear(task);
            _distanceData.Remove(task);
        }
        public override void OnStationRemoved(VehicleStation station)
        {
            base.OnStationRemoved(station);
            foreach (RootTask task in _distanceData.Keys)
            {
                if (task.Destination.VehicleStationLocation.VehicleStation == station)
                {
                    _distanceData.Remove(task);
                }
            }
        }

        public override void Remove(RootTask task)
        {
            base.Remove(task);
            _distanceData.Remove(task);
        }

        internal new static TaskTravelDurationDataSet Read(StateBinaryReader reader, VehicleSchedule schedule)
        {
            TaskTravelDurationDataSet result = new TaskTravelDurationDataSet();
            result.DoRead(reader, schedule);
            return result;
        }



        private void CopyDistanceData(TaskTravelDurationDataSet newDataSet, Vehicle newVehicle)
        {
            foreach (KeyValuePair<RootTask, float> pair in _distanceData)
            {
                newDataSet._distanceData.Add(newVehicle.Schedule.GetTasks()[pair.Key.GetIndex()], pair.Value);
            }
        }

        internal override void Write(StateBinaryWriter writer)
        {
            base.Write(writer);
            writer.WriteInt(_distanceData.Count);
            foreach (KeyValuePair<RootTask, float> pair in _distanceData)
            {
                writer.WriteInt(pair.Key.GetIndex());
                writer.WriteFloat(pair.Value);
            }
        }

        protected override void DoRead(StateBinaryReader reader, VehicleSchedule schedule)
        {
            base.DoRead(reader, schedule);
            if (ScheduleStopwatch.GetSchemaVersion(typeof(TaskDurationDataSet)) >= 2)
            {
                int count = reader.ReadInt();

                for (int i = 0; i < count; i++)
                {
                    int taskIndex = reader.ReadInt();
                    float value = reader.ReadFloat();
                    if (taskIndex > -1)
                    {
                        _distanceData.Add(schedule.GetTasks()[taskIndex], value);
                    } else
                    {
                        ScheduleStopwatch.logger.Log(UnityEngine.LogType.Warning, "RootTask index = -1");
                    }
                }
            }
        }
    }
}
