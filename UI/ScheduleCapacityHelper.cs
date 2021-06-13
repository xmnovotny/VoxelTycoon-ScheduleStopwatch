using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Localization;
using VoxelTycoon.Tracks;
using VoxelTycoon.UI;
using TransfersPerStationCont = ScheduleStopwatch.VehicleScheduleCapacity.TransfersPerStationCont;

namespace ScheduleStopwatch.UI
{
    class ScheduleCapacityHelper
    {
        public static bool TooltipTextForStation(IReadOnlyDictionary<Item, int> transfers, StringBuilder strBuilder, IReadOnlyDictionary<Item, int> routeTransfers, float monthMultiplier)
        {
            bool added = false;
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            foreach (KeyValuePair<Item, int> transfer in transfers)
            {
                string countStr = (Math.Abs(transfer.Value) * monthMultiplier).ToString("N0");
                if (routeTransfers != null && routeTransfers.TryGetValue(transfer.Key, out int routeCount))
                {
                    countStr += "/" + Math.Abs(routeCount).ToString();
                }
                if (transfer.Value > 0)
                {
                    strBuilder.AppendLine().Append(StringHelper.Colorify(StringHelper.Format(locale.GetString("schedule_stopwatch/loaded_items_count"), StringHelper.FormatCountString(transfer.Key.DisplayName, countStr)), Color.blue * 0.8f));
                    added = true;
                }
                else if (transfer.Value < 0)
                {
                    strBuilder.AppendLine().Append(StringHelper.Colorify(StringHelper.Format(locale.GetString("schedule_stopwatch/unloaded_items_count"), StringHelper.FormatCountString(transfer.Key.DisplayName, countStr)), Color.green * 0.9f));
                    added = true;
                }
            }

            return added;
        }

        public static string GetCapacityTooltipText(float? monthMultiplier, IReadOnlyDictionary<Item, int> totalTransfers, TransfersPerStationCont transfPerSt, IReadOnlyDictionary<Item, int> routeToatlTransfers = null, TransfersPerStationCont routeTransfPerStation = null)
        {
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            if (monthMultiplier != null && ((totalTransfers != null && totalTransfers.Count > 0) || (transfPerSt != null && transfPerSt.Count > 0)))
            {
                bool isRoute = routeToatlTransfers != null && routeToatlTransfers.Count > 0;
                StringBuilder sb = new StringBuilder();
                sb.Append(StringHelper.Boldify(locale.GetString("schedule_stopwatch/estim_monthly_transf").ToUpper()));
                if (isRoute)
                {
                    sb.AppendLine().Append(StringHelper.Colorify(locale.GetString("schedule_stopwatch/estim_monthly_transf_hint"), UIColors.Solid.Text * 0.5f));
                }
                if (transfPerSt != null && transfPerSt.Count > 0)
                {
                    foreach (KeyValuePair<VehicleStationLocation, IReadOnlyDictionary<Item, int>> pair in transfPerSt)
                    {
                        StringBuilder stationSb = new StringBuilder();
                        string stationName = StringHelper.Boldify(pair.Key.Name);
                        stationSb.Append(stationName);
                        IReadOnlyDictionary<Item, int> routeTransfers = routeTransfPerStation?[pair.Key];

                        if (TooltipTextForStation(pair.Value, stationSb, routeTransfers, monthMultiplier.Value))
                        {
                            sb.AppendLine().AppendLine().Append(stationSb.ToString());
                        }
                    }
                }
                if (totalTransfers != null && totalTransfers.Count > 0)
                {
                    sb.AppendLine().AppendLine().Append(StringHelper.Colorify(StringHelper.Boldify(locale.GetString("schedule_stopwatch/total_transfer").ToUpper()), UIColors.Solid.Text * 0.8f));
                    foreach (KeyValuePair<Item, int> transfer in totalTransfers)
                    {
                        string countStr = (transfer.Value * monthMultiplier.Value).ToString("N0");
                        if (isRoute && routeToatlTransfers.TryGetValue(transfer.Key, out int routeCount))
                        {
                            countStr += "/" + routeCount.ToString();
                        }
                        sb.AppendLine().Append(StringHelper.FormatCountString(transfer.Key.DisplayName, countStr));
                    }
                }
                return sb.ToString();
            }
            return "";
        }
    }
}
