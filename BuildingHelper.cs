using System;
using System.Collections.Generic;
using System.Text;
using VoxelTycoon.Buildings;
using VoxelTycoon.Researches;
using VoxelTycoon.Tracks;

namespace ScheduleStopwatch
{
    public class BuildingHelper
    {
        public static string GetBuildingName(Building building)
        {
            if (building == null)
            {
                return "";
            }
            if (building is Lab lab)
            {
                return lab.Name;
            }
            if (building is VehicleStation station && station.Location != null)
            {
                return station.Location.Name;
            }
            if (building is VehicleDepot depot)
            {
                return depot.Name;
            }

            return building.DisplayName;

        }
    }
}
