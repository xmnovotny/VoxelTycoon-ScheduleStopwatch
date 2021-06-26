using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    public partial class VehicleScheduleData
    {
        private class TravelMeasurement : Measurement
        {
            private float? _startDistance, _endDistance;
            public float? Distance
            {
                get
                {
                    if (_startDistance != null && _endDistance != null)
                    {
                        return _endDistance.Value - _startDistance.Value;
                    }
                    return null;
                }
            }
            public TravelMeasurement(VehicleScheduleData data, RootTask task) : base(data, task) {
                _startDistance = _vehicleScheduleData.Vehicle.WorldTraveledDistanceCounter.Lifetime;
            }

            public override void Finish()
            {
                _endDistance = _vehicleScheduleData.Vehicle.WorldTraveledDistanceCounter.Lifetime;
                base.Finish();
            }

            public void Finish(RootTask task)
            {
                this.Task = task; //set last RootTask of whole travel through nonstop waypoints
                Finish();
            }

            public override void OnFinished()
            {
                _vehicleScheduleData?.OnTravelMeasurementFinish(this);
            }
            protected override void DoRead(StateBinaryReader reader, VehicleSchedule schedule, VehicleScheduleData data)
            {
                base.DoRead(reader, schedule, data);
                _startDistance = null;
                if (ScheduleStopwatch.GetSchemaVersion(typeof(VehicleScheduleData)) >= 2)
                {
                    if (reader.ReadBool())
                    {
                        _startDistance = reader.ReadFloat();
                    }
                }
            }

            internal override void Write(StateBinaryWriter writer)
            {
                base.Write(writer);
                writer.WriteBool(_startDistance.HasValue);
                if (_startDistance.HasValue)
                {
                    writer.WriteFloat(_startDistance.Value);
                }
            }
        }
    }
}
