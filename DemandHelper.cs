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
        public static Dictionary<Item, int> GetStationDemands(VehicleStation station, Dictionary<Item, int> demandsList = null, Dictionary<Item, int> unservicedDemands = null)
        {
            Dictionary<Item, int> result = demandsList ?? new Dictionary<Item, int>();
            List<IStorageNetworkNode> targetNodes = new List<IStorageNetworkNode>();
            List<IStorageNetworkNode> sourceNodes = new List<IStorageNetworkNode>();
            station.GetConnectedNodes(targetNodes, sourceNodes);
            StorageNetworkManager manager = LazyManager<StorageNetworkManager>.Current;
            foreach (IStorageNetworkNode node in targetNodes)
            {
                if (manager.GetIsEnabled(station, node))
                {
                    GetNodeDemands(node, result, unservicedDemands);
                }
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
            if (node.Building is Lab lab && lab.enabled && lab.Research != null)
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
