using System;
using System.Collections.Generic;
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

            public void Finish()
            {
                endTime = TimeManager.Current.DateTime;
                IsFinished = true;
                OnFinished();
            }

            public abstract void OnFinished();

            internal void Write(StateBinaryWriter writer)
            {
                writer.WriteInt(Task.GetIndex());
                MeasurementSurrogate.Write(writer, this);
                writer.WriteLong(startTime.Value.Ticks);
            }

            internal static Measurement Read(StateBinaryReader reader, VehicleSchedule schedule, VehicleScheduleData data, byte version)
            {
                RootTask task = schedule.GetTasks()[reader.ReadInt()];
                Measurement result = MeasurementSurrogate.Read(reader, data, task);
                result.startTime = new DateTime(reader.ReadLong());
                return result;
            }

        }

        private class StationLoadingMeasurement : Measurement
        {
            public StationLoadingMeasurement(VehicleScheduleData data, RootTask rootTask) : base(data, rootTask) { }

            public override void OnFinished()
            {
                _vehicleScheduleData?.OnStationLoadingMeasurementFinish(this);
            }
        }
        private class TravelMeasurement : Measurement
        {
            public TravelMeasurement(VehicleScheduleData data, RootTask task) : base(data, task) { }
            public void Finish(RootTask task)
            {
                this.Task = task; //set last RootTask of whole travel through nonstop waypoints
                base.Finish();
            }

            public override void OnFinished()
            {
                _vehicleScheduleData?.OnTravelMeasurementFinish(this);
            }
        }

        private class MeasurementSurrogate
        {
            internal static void Write(StateBinaryWriter writer, Measurement measurement)
            {
                if (measurement is StationLoadingMeasurement)
                {
                    writer.WriteByte(0);
                } else 
                if (measurement is TravelMeasurement)
                {
                    writer.WriteByte(1);
                } else
                {
                    throw new ArgumentException("Unknown class " + measurement.GetType().Name);
                }
            }

            internal static Measurement Read(StateBinaryReader reader, VehicleScheduleData data, RootTask task)
            {
                byte id = reader.ReadByte();
                switch (id)
                {
                    case 0:
                        return new StationLoadingMeasurement(data, task);
                    case 1:
                        return new TravelMeasurement(data, task);
                }

                throw new ArgumentException("Unknown id " + id.ToString());
            }
        }
    }
}
