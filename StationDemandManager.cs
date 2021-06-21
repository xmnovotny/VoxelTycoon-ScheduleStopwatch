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
            if (location.IsDead)
            {
                return null;
            }
            if (_additionalDemands.TryGetValue(location.VehicleStation, out UniqueList<IStorageNetworkNode> demands))
            {
                return demands.ToImmutableUniqueList();
            }
            return null;
        }

        public IEnumerable<IStorageNetworkNode> GetAdditionalDemandsEnum(VehicleStationLocation location)
        {
            if (location.IsDead)
            {
                yield break;
            }
            if (_additionalDemands.TryGetValue(location.VehicleStation, out UniqueList<IStorageNetworkNode> demands))
            {
                for (int i = 0; i < demands.Count; i++)
                {
                    yield return demands[i];
                }
            }
            yield break;
        }

        public ImmutableUniqueList<VehicleStation>? GetConnectedStations(VehicleStation station)
        {
            if (_connectedStations.TryGetValue(station, out UniqueList<VehicleStation> stations))
            {
                return stations.ToImmutableUniqueList();
            }
            return null;
        }

        public HashSet<VehicleStation> GetConnectedStationsHashset(VehicleStation station, bool includeOwnStation = true)
        {
            HashSet<VehicleStation> result = new HashSet<VehicleStation>();
            if (includeOwnStation)
            {
                result.Add(station);
            }
            if (_connectedStations.TryGetValue(station, out UniqueList<VehicleStation> stations))
            {
                for(int i = 0; i < stations.Count; i++)
                {
                    result.Add(stations[i]);
                }
            }
            return result;

        }

        /** Returns total demands for selected station and its connected stations */
        public Dictionary<Item, int> GetCombinedStationsDemands(VehicleStation location, Dictionary<Item, int> demandsList = null, Dictionary<Item, int> unservicedDemands = null, bool additionalDemands = true)
        {
            //            Dictionary<Item, int> result = demandsList ?? new Dictionary<Item, int>();
            return null;
        }

        public IEnumerable<VehicleStation> GetConnectedStationsEnum(VehicleStation station)
        {
            if (_connectedStations.TryGetValue(station, out UniqueList<VehicleStation> stations))
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
                _connedtedBuildings.Add(demand.Building);
                bool result = GetOrCreateDemandsList(location.VehicleStation).Add(demand);
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
            if (location.IsDead)
            {
                return false;
            }
            if (_additionalDemands.TryGetValue(location.VehicleStation, out UniqueList<IStorageNetworkNode> list))
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

        public bool AddConnectedStation(VehicleStation station, VehicleStation stationToAdd)
        {
            if (stationToAdd != station)
            {
                _connedtedBuildings.Add(stationToAdd);
                bool result = GetOrCreateConnectedStationsList(station).Add(stationToAdd);
                if (result)
                {
                    ConnectedStationChange?.Invoke(station.Location);
                }
                return result;
            }
            return false;
        }

        public bool RemoveConnectedStation(VehicleStation station, VehicleStation stationToRemove)
        {
            if (_connectedStations.TryGetValue(station, out UniqueList<VehicleStation> list))
            {
                bool result = list.QuickRemove(stationToRemove);
                if (result)
                {
                    ConnectedStationChange?.Invoke(station.Location);
                }
                return result;
            }
            return false;
        }

        public void OnBuildingRemoved(Building building)
        {
            if (_connedtedBuildings.Contains(building))
            {
                if (building is VehicleStation station)
                {
                    if (_additionalDemands.Remove(station))
                    {
                        DemandChange?.Invoke(station);
                    }
                    if (_connectedStations.Remove(station))
                    {
                        ConnectedStationChange?.Invoke(station.Location);
                    }
                    foreach (KeyValuePair<VehicleStation, UniqueList<VehicleStation>> pair in _connectedStations)
                    {
                        if (pair.Value.QuickRemove(station))
                        {
                            ConnectedStationChange?.Invoke(pair.Key.Location);
                        }
                    }
                }
                if (building is IStorageNetworkNode node)
                {
                    foreach (KeyValuePair<VehicleStation, UniqueList<IStorageNetworkNode>> pair in _additionalDemands)
                    {
                        if (pair.Value.QuickRemove(node))
                        {
                            DemandChange?.Invoke(pair.Key);
                        }
                    }
                }
                _connedtedBuildings.Remove(building);
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            LazyManager<BuildingManager>.Current.BuildingRemoved += OnBuildingRemoved;
        }

        protected override void OnDeinitialize()
        {
            LazyManager<BuildingManager>.Current.BuildingRemoved -= OnBuildingRemoved;
            base.OnDeinitialize();
        }

        private UniqueList<IStorageNetworkNode> GetOrCreateDemandsList(VehicleStation station)
        {
            if (!_additionalDemands.TryGetValue(station, out UniqueList<IStorageNetworkNode> demandList))
            {
                demandList = new UniqueList<IStorageNetworkNode>();
                _additionalDemands.Add(station, demandList);
            }
            return demandList;
        } 

        private UniqueList<VehicleStation> GetOrCreateConnectedStationsList(VehicleStation station)
        {
            if (!_connectedStations.TryGetValue(station, out UniqueList<VehicleStation> stationList))
            {
                stationList = new UniqueList<VehicleStation>();
                _connectedStations.Add(station, stationList);
            }
            return stationList;
        }

        private readonly Dictionary<VehicleStation, UniqueList<IStorageNetworkNode>> _additionalDemands = new Dictionary<VehicleStation, UniqueList<IStorageNetworkNode>>();
        private readonly Dictionary<VehicleStation, UniqueList<VehicleStation>> _connectedStations = new Dictionary<VehicleStation, UniqueList<VehicleStation>>();
        private readonly Dictionary<VehicleStation, HashSet<IStorageNetworkNode>> _demandNodesCache = new Dictionary<VehicleStation, HashSet<IStorageNetworkNode>>();
        private readonly HashSet<Building> _connedtedBuildings = new HashSet<Building>();
    }
}
