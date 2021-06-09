using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    public partial class VehicleScheduleData
    {
        private class StationLoadingMeasurement : Measurement
        {
            public StationLoadingMeasurement(VehicleScheduleData data, RootTask rootTask) : base(data, rootTask) { }

            public override void OnFinished()
            {
                _vehicleScheduleData?.OnStationLoadingMeasurementFinish(this);
            }
        }
    }
}
