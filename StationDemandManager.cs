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
            if (_connectedStations.TryGetValue(location, out UniqueList<VehicleStationLocation> stations))
            {
                return stations.ToImmutableUniqueList();
            }
            return null;
        }

        public HashSet<VehicleStationLocation> GetConnectedStationsHashset(VehicleStationLocation location, bool includeOwnStation = true)
        {
            HashSet<VehicleStationLocation> result = new HashSet<VehicleStationLocation>();
            if (includeOwnStation)
            {
                result.Add(location);
            }
            if (_connectedStations.TryGetValue(location, out UniqueList<VehicleStationLocation> stations))
            {
                for(int i = 0; i < stations.Count; i++)
                {
                    result.Add(stations[i]);
                }
            }
            return result;

        }

        public IEnumerable<VehicleStationLocation> GetConnectedStationsEnum(VehicleStationLocation location)
        {
            if (_connectedStations.TryGetValue(location, out UniqueList<VehicleStationLocation> stations))
            {
                for (int i = 0; i < stations.Count; i++)
                {
                    yield return stations[i];
                }
            }
            yield break;
        }

        public bool AddDemand(VehicleStationLocation location, IStorageNetworkNode demand)
        {
            if (demand is Store || demand is Lab)
            {
                if (DemandHelper.IsInBasicDemand(location.VehicleStation, demand))
                {
                    return false;
                }
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
            if (stationToAdd != location)
            {
                bool result = GetOrCreateConnectedStationsList(location).Add(stationToAdd);
                if (result)
                {
                    ConnectedStationChange?.Invoke(location);
                }
                return result;
            }
            return false;
        }

        public bool RemoveConnectedStation(VehicleStationLocation location, VehicleStationLocation stationToRemove)
        {
            if (_connectedStations.TryGetValue(location, out UniqueList<VehicleStationLocation> list))
            {
                bool result = list.QuickRemove(stationToRemove);
                if (result)
                {
                    ConnectedStationChange?.Invoke(location);
                }
                return result;
            }
            return false;
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
            if (!_connectedStations.TryGetValue(location, out UniqueList<VehicleStationLocation> stationList))
            {
                stationList = new UniqueList<VehicleStationLocation>();
                _connectedStations.Add(location, stationList);
            }
            return stationList;
        }

        private readonly Dictionary<VehicleStationLocation, UniqueList<IStorageNetworkNode>> _additionalDemands = new Dictionary<VehicleStationLocation, UniqueList<IStorageNetworkNode>>();
        private readonly Dictionary<VehicleStationLocation, UniqueList<VehicleStationLocation>> _connectedStations = new Dictionary<VehicleStationLocation, UniqueList<VehicleStationLocation>>();
        
    }
}
