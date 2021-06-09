using System;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    public partial class VehicleScheduleData
    {
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
