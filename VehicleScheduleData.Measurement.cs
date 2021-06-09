using System;
using VoxelTycoon;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    public partial class VehicleScheduleData
    {
        private abstract class Measurement
        {
            protected VehicleScheduleData _vehicleScheduleData;
            public DateTime? startTime, endTime;
            public RootTask Task { get; protected set; }
            public bool IsFinished { get; private set; }
            public TimeSpan measuredTime
            {
                get
                {
                    if (IsFinished)
                    {
                        return endTime.Value - startTime.Value;
                    }
                    throw new InvalidOperationException("Measurement is not finished");
                }
            }

            public Measurement(VehicleScheduleData data, RootTask task)
            {
                startTime = TimeManager.Current.DateTime;
                this.Task = task;
                _vehicleScheduleData = data;
            }

            public virtual void Finish()
            {
                endTime = TimeManager.Current.DateTime;
                IsFinished = true;
                OnFinished();
            }

            public abstract void OnFinished();

            internal virtual void Write(StateBinaryWriter writer)
            {
                writer.WriteInt(Task.GetIndex());
                MeasurementSurrogate.Write(writer, this);
                writer.WriteLong(startTime.Value.Ticks);
            }

            internal static Measurement Read(StateBinaryReader reader, VehicleSchedule schedule, VehicleScheduleData data)
            {
                RootTask task = schedule.GetTasks()[reader.ReadInt()];
                Measurement result = MeasurementSurrogate.Read(reader, data, task);
                result.DoRead(reader, schedule, data);
                return result;
            }

            protected virtual void DoRead(StateBinaryReader reader, VehicleSchedule schedule, VehicleScheduleData data)
            {
                startTime = new DateTime(reader.ReadLong());
            }

        }
    }
}
