using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Cities;
using VoxelTycoon.Game.UI;

namespace ScheduleStopwatch.UI
{
    [HarmonyPatch]
    class CityWindowDemandBugfix
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CityWindowDemandsTabHeader), "Invalidate")]
        private static void CityWindowDemandsTabHeader_Invalidate_pof(CityWindowDemandsTabHeader __instance, City ____city, Text ____nextPopulationText)
        {
            if (____city.Type == CityType.Industrial)
            {
                int demandLimit = ____city.DemandLimit;
                int newPopulation = ____city.GetPopulation(demandLimit - ____city.ExtraDemandLimit + 1);
                if (newPopulation <= 0)
                {
                    newPopulation = Mathf.Max(newPopulation, ____city.Population);
                }
                if (____city.Status != CityStatus.Megapolis)
                {
                    int newStatusPopulation = ____city.GetPopulation(____city.Status + 1);
                    if (newStatusPopulation < newPopulation)
                    {
                        int newPopulation2 = ____city.GetPopulation(demandLimit - ____city.ExtraDemandLimit);
                        newPopulation = Mathf.Min(Mathf.Max(newPopulation2, newStatusPopulation));
                    }
                }
                ____nextPopulationText.text = StringHelper.Nicify(newPopulation);
            }
        }
    }
}
