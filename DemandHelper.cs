using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Buildings;
using VoxelTycoon.Cities;
using VoxelTycoon.Researches;
using VoxelTycoon.Tracks;

namespace ScheduleStopwatch
{
    [HarmonyPatch]
    public class DemandHelper
    {
        public static bool IsInBasicDemand(VehicleStation station, IStorageNetworkNode node)
        {
            return GetBasicStationDemand(station).Contains(node);
        }

        public static List<IStorageNetworkNode> GetBasicStationDemand(VehicleStation station)
        {
            List<IStorageNetworkNode> targetNodes = new List<IStorageNetworkNode>();
            List<IStorageNetworkNode> sourceNodes = new List<IStorageNetworkNode>();
            station.GetConnectedNodes(targetNodes, sourceNodes);
            return targetNodes;
        }

        public static IEnumerable<IStorageNetworkNode> GetStationDemandNodes(VehicleStation station, bool additionalDemands = true, bool includeDisabled = false)
        {
            List<IStorageNetworkNode> targetNodes = new List<IStorageNetworkNode>();
            List<IStorageNetworkNode> sourceNodes = new List<IStorageNetworkNode>();
            station.GetConnectedNodes(targetNodes, sourceNodes);
            StorageNetworkManager manager = LazyManager<StorageNetworkManager>.Current;
            foreach (IStorageNetworkNode node in targetNodes)
            {
                if (includeDisabled || manager.GetIsEnabled(station, node) && (node is Store || node is Lab))
                {
                    yield return node;
                }
            }
            if (additionalDemands)
            {
                foreach (IStorageNetworkNode node in LazyManager<StationDemandManager>.Current.GetAdditionalDemandsEnum(station.Location))
                {
                    yield return node;
                }
            }
            yield break;
        }

        public static HashSet<IStorageNetworkNode> GetStationDemandNodesHashSet(VehicleStation station, bool additionalDemands = true, bool includeDisabled = false)
        {
            return new HashSet<IStorageNetworkNode>(GetStationDemandNodes(station, additionalDemands, includeDisabled));
        }

        public static Dictionary<Item, int> GetStationDemands(VehicleStation station, Dictionary<Item, int> demandsList = null, Dictionary<Item, int> unservicedDemands = null, bool additionalDemands = true)
        {
            Dictionary<Item, int> result = demandsList ?? new Dictionary<Item, int>();
            foreach (IStorageNetworkNode node in GetStationDemandNodes(station, additionalDemands))
            {
                GetNodeDemands(node, result, unservicedDemands);
            }
            return result;
        }

        public static void GetNodeDemands(IStorageNetworkNode node, Dictionary<Item, int> demands, Dictionary<Item, int> unservicedDemands = null)
        {
            if (node.Building is Store store)
            {
                AddCityDemand(store.Demand, demands);
                if (unservicedDemands != null && store.Demand.DeliveredCounter.Lifetime == 0)
                {
                    //unserviced demand
                    AddCityDemand(store.Demand, unservicedDemands);
                }
            }
            if (node.Building is Lab lab && lab.IsEnabled && lab.Research != null && !LazyManager<ResearchManager>.Current.IsCompleted(lab.Research))
            {
                AddLab(lab, demands);
            }
        }

        private static void AddCityDemand(CityDemand demand, Dictionary<Item, int> demandsList)
        {
            RecipeHelper.AddItem(demandsList, demand.Item, demand.Demand);
        }

        private static void AddLab(Lab lab, Dictionary<Item, int> demandsList)
        {
            foreach (Research.RequiredItem reqItem in lab.Research.GetItemsPerDay().ToList())
            {
                RecipeHelper.AddItem(demandsList, reqItem.Item, Mathf.RoundToInt(reqItem.Count * lab.SharedData.Efficiency * 30));
            }
        }
    }
}
