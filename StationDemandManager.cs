using System;
using System.Collections.Generic;
using System.Text;
using VoxelTycoon;
using VoxelTycoon.Buildings;
using VoxelTycoon.Researches;
using VoxelTycoon.Tracks;

namespace ScheduleStopwatch
{
    public class StationDemandManager: LazyManager<StationDemandManager>
    {
        public Action<IStorageNetworkNode> DemandChange;
        public Action<VehicleStationLocation> ConnectedStationChange;

        public ImmutableUniqueList<IStorageNetworkNode>? GetAdditionalDemands(VehicleStationLocation location)
        {
            if (_additionalDemands.TryGetValue(location, out UniqueList<IStorageNetworkNode> demands))
            {
                return demands.ToImmutableUniqueList();
            }
            return null;
        }

        public IEnumerable<IStorageNetworkNode> GetAdditionalDemandsEnum(VehicleStationLocation location)
        {
            if (_additionalDemands.TryGetValue(location, out UniqueList<IStorageNetworkNode> demands))
            {
                for (int i = 0; i < demands.Count; i++)
                {
                    yield return demands[i];
                }
            }
            yield break;
        }

        public ImmutableUniqueList<VehicleStationLocation>? GetConnectedStations(VehicleStationLocation location)
        {
            if (_connecctedStations.TryGetValue(location, out UniqueList<VehicleStationLocation> stations))
            {
                return stations.ToImmutableUniqueList();
            }
            return null;
        }

        public bool AddDemand(VehicleStationLocation location, IStorageNetworkNode demand)
        {
            if (demand is Store || demand is Lab)
            {
                bool result = GetOrCreateDemandsList(location).Add(demand);
                if (result)
                {
                    DemandChange?.Invoke(location.VehicleStation);
                }
                return result;
            } else
            {
                throw new ArgumentException("Only Store and Lab can be added as a station demand", "demand");
            }
        }

        public bool RemoveDemand(VehicleStationLocation location, IStorageNetworkNode demand)
        {
            if (_additionalDemands.TryGetValue(location, out UniqueList<IStorageNetworkNode> list))
            {
                bool result = list.QuickRemove(demand);
                if (result)
                {
                    DemandChange?.Invoke(location.VehicleStation);
                }
                return result;
            }
            return false;
        }

        public bool AddConnectedStation(VehicleStationLocation location, VehicleStationLocation stationToAdd)
        {
            bool result = GetOrCreateConnectedStationsList(location).Add(stationToAdd);
            if (result)
            {
                ConnectedStationChange?.Invoke(location);
            }
            return result;
        }

        private UniqueList<IStorageNetworkNode> GetOrCreateDemandsList(VehicleStationLocation location)
        {
            if (!_additionalDemands.TryGetValue(location, out UniqueList<IStorageNetworkNode> demandList))
            {
                demandList = new UniqueList<IStorageNetworkNode>();
                _additionalDemands.Add(location, demandList);
            }
            return demandList;
        } 

        private UniqueList<VehicleStationLocation> GetOrCreateConnectedStationsList(VehicleStationLocation location)
        {
            if (!_connecctedStations.TryGetValue(location, out UniqueList<VehicleStationLocation> stationList))
            {
                stationList = new UniqueList<VehicleStationLocation>();
                _connecctedStations.Add(location, stationList);
            }
            return stationList;
        }

        private readonly Dictionary<VehicleStationLocation, UniqueList<IStorageNetworkNode>> _additionalDemands = new Dictionary<VehicleStationLocation, UniqueList<IStorageNetworkNode>>();
        private readonly Dictionary<VehicleStationLocation, UniqueList<VehicleStationLocation>> _connecctedStations = new Dictionary<VehicleStationLocation, UniqueList<VehicleStationLocation>>();
        
    }
}
