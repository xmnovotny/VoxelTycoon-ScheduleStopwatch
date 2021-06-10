using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    public class DurationsPerStationsContainer
    {
        public DurationsPerStationsContainer() { }

        public DurationsPerStationsContainer(VehicleScheduleData scheduleData)
        {
            AddTimes(scheduleData);
        }

        /** add times from scheduleData as new (not to the average), keeps old data when new data isn't available */
        public void NewTimes(VehicleScheduleData scheduleData)
        {
            MarkForOverwrite();
            AddTimes(scheduleData);
        }

        /** add times from scheduleData to the existing data for averaging */
        public void AddTimes(VehicleScheduleData scheduleData)
        {
            foreach ((RootTask endStationTask, VehicleStationLocation startStation, VehicleStationLocation endStation, List<VehicleStationLocation> nonstopStations) in scheduleData.GetNonNonstopScheduleParts())
            {
                TimeSpan? duration = scheduleData.GetAverageTravelDuration(endStationTask);
                if (duration != null)
                {
                    DurationDataSet travelTime = GetOrAddTravelTimeSet(startStation, endStation, nonstopStations);
                    travelTime.Add(duration.Value);
                }

                TimeSpan? stationDuration = scheduleData.GetAverageStationLoadingDuration(endStationTask);
                if (stationDuration != null)
                {
                    DurationDataSet stationTimeSet = GetOrAddStationTimeSet(endStation);
                    stationTimeSet.Add(stationDuration.Value);
                }
            }
        }

        public TimeSpan? GetTravelTime(VehicleStationLocation startStation, VehicleStationLocation endStation, List<VehicleStationLocation> nonstopStations = null)
        {
            int? hashCode = GetLocationsHashCode(nonstopStations);
            if (_travelTimes.TryGetValue((startStation, endStation, hashCode), out DurationDataSet travelTime))
            {
                return travelTime.TotalAverage;
            }
            return null;
        }

        public TimeSpan? GetStationTime(VehicleStationLocation station)
        {
            if (_stationTimes.TryGetValue(station, out DurationDataSet timeData))
            {
                return timeData.TotalAverage;
            }
            return null;
        }

        public void MarkForOverwrite()
        {
            foreach (DurationDataSet times in _travelTimes.Values)
            {
                times.MarkForOverwrite();
            }
            foreach (DurationDataSet times in _stationTimes.Values)
            {
                times.MarkForOverwrite();
            }
        }

        public void DumpToLog()
        {
            FileLog.Log("Travel times count: " + _travelTimes.Count);
            foreach (KeyValuePair<(VehicleStationLocation start, VehicleStationLocation end, int? nonstopHash), DurationDataSet> pair in _travelTimes)
            {
                FileLog.Log(String.Format("{0} => {1}: {2} {3}", pair.Key.start.Name, pair.Key.end.Name, pair.Value.TotalAverage.Value.TotalDays.ToString("N1"), 
                    pair.Key.nonstopHash != null ? String.Format("hash: {0}", pair.Key.nonstopHash.ToString()) : ""));
            }
        }

        private DurationDataSet GetOrAddStationTimeSet(VehicleStationLocation station)
        {
            if (!_stationTimes.TryGetValue(station, out DurationDataSet result))
            {
                result = new DurationDataSet(1);
                _stationTimes.Add(station, result);
            }
            return result;
        }

        private DurationDataSet GetOrAddTravelTimeSet(VehicleStationLocation start, VehicleStationLocation end, List<VehicleStationLocation> nonstopList)
        {
            int? hashCode = GetLocationsHashCode(nonstopList);
            if (!_travelTimes.TryGetValue((start, end, hashCode), out DurationDataSet travelTime))
            {
                travelTime = new DurationDataSet(1);
                _travelTimes.Add((start, end, hashCode), travelTime);
            }
            return travelTime;
        }

        private int? GetLocationsHashCode(List<VehicleStationLocation> locations)
        {
            if (locations == null || locations.Count == 0)
            {
                return null;
            }

            int num = 0;
            foreach (VehicleStationLocation location in locations)
            {
                num = num * -0x5AAAAAD7 + location.GetHashCode();
            }
            return num;
        }

        private readonly Dictionary<(VehicleStationLocation start, VehicleStationLocation end, int? nonstopHash), DurationDataSet> _travelTimes = new Dictionary<(VehicleStationLocation, VehicleStationLocation, int?), DurationDataSet>();
        private readonly Dictionary<VehicleStationLocation, DurationDataSet> _stationTimes = new Dictionary<VehicleStationLocation, DurationDataSet>();
    }
}
