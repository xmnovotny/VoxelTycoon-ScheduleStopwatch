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

        public ImmutableUniqueList<IStorageNetworkNode>? GetAdditionalDemands(VehicleStation station)
        {
            if (_additionalDemands.TryGetValue(station, out UniqueList<IStorageNetworkNode> demands))
            {
                return demands.ToImmutableUniqueList();
            }
            return null;
        }

        public IEnumerable<IStorageNetworkNode> GetAdditionalDemandsEnum(VehicleStation station)
        {
            if (_additionalDemands.TryGetValue(station, out UniqueList<IStorageNetworkNode> demands))
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
        public Dictionary<Item, int> GetCombinedStationsDemands(VehicleStation station, Dictionary<Item, int> demandsList = null, Dictionary<Item, int> unservicedDemands = null)
        {
            Dictionary<Item, int> result = demandsList ?? new Dictionary<Item, int>();
            DemandHelper.GetNodesDemands(GetDemandNodes(station), result, unservicedDemands);
            return result;
        }

        public IEnumerable<IStorageNetworkNode> GetCombinedStationDemandNodesEnum(VehicleStation station, bool additionalDemands = true)
        {
            foreach (IStorageNetworkNode node in GetDemandNodes(station, additionalDemands))
            {
                yield return node;
            }
        }

        public IEnumerable<VehicleStation> GetConnectedStationsEnum(VehicleStation station, bool includeOwnStation = false)
        {
            if (includeOwnStation)
            {
                yield return station;
            }
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
                _connectedBuildings.Add(demand.Building);
                bool result = GetOrCreateDemandsList(location.VehicleStation).Add(demand);
                if (result)
                {
                    OnDemandChange(location.VehicleStation);
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
                    OnDemandChange(location.VehicleStation);
                }
                return result;
            }
            return false;
        }

        public bool AddConnectedStation(VehicleStation station, VehicleStation stationToAdd)
        {
            if (stationToAdd != station)
            {
                _connectedBuildings.Add(stationToAdd);
                bool result = GetOrCreateConnectedStationsList(station).Add(stationToAdd);
                if (result)
                {
                    if (_additionalDemands.TryGetValue(station, out UniqueList<IStorageNetworkNode> additionalDemands))
                    {
                        //remove demands of added station from additional demands of own station
                        foreach (IStorageNetworkNode node in DemandHelper.GetStationDemandNodes(stationToAdd, false))
                        {
                            additionalDemands.QuickRemove(node);
                        }
                    }
                    OnConnectedStationChange(station);
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
                    OnConnectedStationChange(station);
                }
                return result;
            }
            return false;
        }

        public void OnConnectedStationChange(VehicleStation station)
        {
            _demandNodesCache.Remove((station, true));
            _demandNodesCache.Remove((station, false));
            ConnectedStationChange?.Invoke(station.Location);
        }

        public void OnDemandChange(VehicleStation station)
        {
            _demandNodesCache.Remove((station, true));
            _demandNodesCache.Remove((station, false));
            DemandChange?.Invoke(station);
        }

        public void OnNodeChange(IStorageNetworkNode node)
        {
            if (node.Building is VehicleStation station)
            {
                OnDemandChange(station);
                foreach (KeyValuePair<VehicleStation, UniqueList<VehicleStation>> pair in _connectedStations)
                {
                    if (pair.Key == station)
                    {
                        continue;
                    }
                    if (pair.Value.Contains(station))
                    {
                        OnDemandChange(pair.Key);
                    }
                }
            }
        }

        public void OnBuildingRemoved(Building building)
        {
            if (_connectedBuildings.Contains(building))
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
                            OnConnectedStationChange(pair.Key);
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
                _connectedBuildings.Remove(building);
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            LazyManager<BuildingManager>.Current.BuildingRemoved += OnBuildingRemoved;
            LazyManager<StorageNetworkManager>.Current.NodeChanged += OnNodeChange;
        }

        protected override void OnDeinitialize()
        {
            LazyManager<BuildingManager>.Current.BuildingRemoved -= OnBuildingRemoved;
            LazyManager<StorageNetworkManager>.Current.NodeChanged -= OnNodeChange;
            base.OnDeinitialize();
        }

        private HashSet<IStorageNetworkNode> GetDemandNodes(VehicleStation station, bool additionalDemands = true)
        {
            if (!_demandNodesCache.TryGetValue((station, additionalDemands), out HashSet<IStorageNetworkNode> result))
            {
                result = new HashSet<IStorageNetworkNode>(DemandHelper.GetStationDemandNodes(station, additionalDemands));
                foreach (VehicleStation addStation in GetConnectedStationsEnum(station))
                {
                    result.UnionWith(DemandHelper.GetStationDemandNodes(addStation, false, false));
                }
                _demandNodesCache.Add((station, additionalDemands), result);
            }
            return result;
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
        private readonly Dictionary<(VehicleStation station, bool additionalDemands), HashSet<IStorageNetworkNode>> _demandNodesCache = new Dictionary<(VehicleStation, bool), HashSet<IStorageNetworkNode>>();
        private readonly HashSet<Building> _connectedBuildings = new HashSet<Building>();
    }
}
