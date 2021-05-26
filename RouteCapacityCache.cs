using System;
using System.Collections.Generic;
using VoxelTycoon;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    using TransfersPerStationCont = VehicleScheduleCapacity.TransfersPerStationCont;

    public class RouteCapacityCache : LazyManager<RouteCapacityCache>
    {

        private readonly Dictionary<VehicleRoute, CacheData> _cache = new Dictionary<VehicleRoute, CacheData>();

        private CacheData this[VehicleRoute route]
        {
            get
            {
                if (_cache.TryGetValue(route, out CacheData result))
                {
                    return result;
                }
                return null;
            }
        }

        private CacheData GetOrCreateCacheData(VehicleRoute route)
        {
            if (!_cache.TryGetValue(route, out CacheData result))
            {
                result = new CacheData(route);
                _cache.Add(route, result);
            }
            return result;
        }

        private void OnVehicleRouteChange(Vehicle vehicle, VehicleRoute oldRoute, VehicleRoute newRoute)
        {
            if (oldRoute != null && _cache.TryGetValue(oldRoute, out CacheData oldData))
            {
                oldData.OnRouteRemovedFromVehicle(vehicle, oldRoute);
            }
            if (newRoute != null && _cache.TryGetValue(newRoute, out CacheData newData))
            {
                newData.OnRouteAddedToVehicle(vehicle);
            }
            if (oldRoute?.Vehicles.Count == 0)
            {
                OnRemoveRoute(oldRoute);
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            VehicleScheduleHelper.Current.VehicleRouteChanged += OnVehicleRouteChange;
        }

        protected override void OnDeinitialize()
        {
            VehicleScheduleHelper.Current.VehicleRouteChanged -= OnVehicleRouteChange;
            foreach (CacheData data in _cache.Values)
            {
                data.OnRouteDataRemove();
            }
            _cache.Clear();
            base.OnDeinitialize();
        }

        public IReadOnlyDictionary<Item, int> GetRouteTotalTransfers(VehicleRoute route)
        {
            return GetOrCreateCacheData(route).TotalTransfers;
        }

        public IReadOnlyDictionary<Item, int> GetRouteTaskTransfers(VehicleRoute route, RootTask task)
        {
            return GetOrCreateCacheData(route).GetTransfers(task);
        }
        public TransfersPerStationCont GetRouteTransfersPerStation(VehicleRoute route)
        {
            return GetOrCreateCacheData(route).TransfersPerStation;
        }

        private void OnRemoveRoute(VehicleRoute route)
        {
            if (_cache.TryGetValue(route, out CacheData routeData))
            {
                routeData.OnRouteDataRemove();
                _cache.Remove(route);
            }
        }

        private class CacheData
        {
            public VehicleRoute route;
            private IReadOnlyDictionary<Item, int> _totalTransfers;
            /** route transfers per task (by task index) */
            private readonly Dictionary<int, IReadOnlyDictionary<Item, int>> _transfers = new Dictionary<int, IReadOnlyDictionary<Item, int>>();
            private TransfersPerStationCont _perStationTrasf;
            private bool _loadedTotalTransfers, _loadedPerStationTransf;

            public CacheData(VehicleRoute route)
            {
                VehicleScheduleDataManager manager = Manager<VehicleScheduleDataManager>.Current;
                this.route = route;
                foreach (Vehicle vehicle in route.Vehicles.ToArray())
                {
                    manager.GetOrCreateVehicleScheduleData(vehicle).SubscribeOwnDataChanged(OnVehicleDataChanged);
                }
            }

            public IReadOnlyDictionary<Item, int> TotalTransfers
            {
                get
                {
                    if (!_loadedTotalTransfers)
                    {
                        if (route.Vehicles.Count > 1)
                        {
                            _totalTransfers = Manager<VehicleScheduleDataManager>.Current[route.Vehicles[0]]?.Capacity.GetRouteTotalTransfers();
                        }
                        _loadedTotalTransfers = true;
                    } else
                    {
                    }
                    return _totalTransfers;
                }
            }

            public TransfersPerStationCont TransfersPerStation
            {
                get
                {
                    if (!_loadedPerStationTransf)
                    {
                        if (route.Vehicles.Count > 1)
                        {
                            _perStationTrasf = Manager<VehicleScheduleDataManager>.Current[route.Vehicles[0]]?.Capacity.GetRouteTransfersPerStation();
                        }
                        _loadedPerStationTransf = true;
                    } else
                    {
                    }
                    return _perStationTrasf;
                }
            }

            public IReadOnlyDictionary<Item, int> GetTransfers(RootTask task)
            {
                if (task.Vehicle.Route != route)
                {
                    throw new ArgumentException("Wrong route in the tasks vehicle", "task");
                }
                IReadOnlyDictionary<Item, int> result;
                if (!_transfers.TryGetValue(task.GetIndex(), out result))
                {
                    result = Manager<VehicleScheduleDataManager>.Current[task.Vehicle]?.Capacity.GetRouteTaskTransfers(task);
                    _transfers.Add(task.GetIndex(), result);
                }
                return result;
            }

            public void MarkDirty()
            {
                _loadedTotalTransfers = false;
                _loadedPerStationTransf = false;
                _totalTransfers = null;
                _perStationTrasf = null;
                _transfers.Clear();
            }

            public void OnRouteAddedToVehicle(Vehicle vehicle)
            {
                MarkDirty();
                VehicleScheduleData data = Manager<VehicleScheduleDataManager>.Current.GetOrCreateVehicleScheduleData(vehicle);
                data.SubscribeOwnDataChanged(OnVehicleDataChanged);
                data.CallDataChangedEventsForRoute(null);
            }
            public void OnRouteRemovedFromVehicle(Vehicle vehicle, VehicleRoute oldRoute)
            {
                MarkDirty();
                VehicleScheduleData data = Manager<VehicleScheduleDataManager>.Current[vehicle];
                if (data != null)
                {
                    data.UnsubscribeOwnDataChanged(OnVehicleDataChanged);
                    if (oldRoute.Vehicles.Count > 0)
                    {
                        data = Manager<VehicleScheduleDataManager>.Current[oldRoute.Vehicles[0]];
                        data.CallDataChangedEventsForRoute(null);
                    }
                }
            }

            public void OnRouteDataRemove()
            {
                VehicleScheduleDataManager manager = Manager<VehicleScheduleDataManager>.Current;
                if (manager != null)
                {
                    foreach (Vehicle vehicle in route.Vehicles.ToArray())
                    {
                        manager[vehicle]?.UnsubscribeOwnDataChanged(OnVehicleDataChanged);
                    }
                }
            }

            private void OnVehicleDataChanged(VehicleScheduleData _)
            {
                MarkDirty();
            }
        }

    }
}
