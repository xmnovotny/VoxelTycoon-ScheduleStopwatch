using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Localization;
using VoxelTycoon.Tracks;
using VoxelTycoon.UI;
using static ScheduleStopwatch.VehicleScheduleCapacity;
using TransfersPerStationCont = ScheduleStopwatch.VehicleScheduleCapacity.TransfersPerStationCont;

namespace ScheduleStopwatch.UI
{
    class ScheduleCapacityHelper
    {
        public static bool TooltipTextForStation(IReadOnlyDictionary<Item, TransferData> transfers, StringBuilder strBuilder, IReadOnlyDictionary<Item, TransferData> routeTransfers, float monthMultiplier)
        {
            bool added = false;
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            foreach (KeyValuePair<Item, TransferData> transfer in transfers)
            {
                string loadStr = (transfer.Value.load * monthMultiplier).ToString("N0");
                string unloadStr = (transfer.Value.unload * monthMultiplier).ToString("N0");
                if (routeTransfers != null && routeTransfers.TryGetValue(transfer.Key, out TransferData routeCount))
                {
                    loadStr += "/" + routeCount.load.ToString();
                    unloadStr += "/" + routeCount.unload.ToString();
                }
                if (transfer.Value.load > 0)
                {
                    strBuilder.AppendLine().Append(StringHelper.Colorify(StringHelper.Format(locale.GetString("schedule_stopwatch/loaded_items_count"), StringHelper.FormatCountString(transfer.Key.DisplayName, loadStr)), Color.blue * 0.8f));
                    added = true;
                }
                else if (transfer.Value.unload > 0)
                {
                    strBuilder.AppendLine().Append(StringHelper.Colorify(StringHelper.Format(locale.GetString("schedule_stopwatch/unloaded_items_count"), StringHelper.FormatCountString(transfer.Key.DisplayName, unloadStr)), Color.green * 0.9f));
                    added = true;
                }
            }

            return added;
        }

        public static string GetCapacityTooltipText(float? monthMultiplier, IReadOnlyDictionary<Item, TransferData> totalTransfers, TransfersPerStationCont transfPerSt, IReadOnlyDictionary<Item, TransferData> routeToatlTransfers = null, TransfersPerStationCont routeTransfPerStation = null)
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
                    foreach (KeyValuePair<VehicleStationLocation, IReadOnlyDictionary<Item, TransferData>> pair in transfPerSt)
                    {
                        StringBuilder stationSb = new StringBuilder();
                        string stationName = StringHelper.Boldify(pair.Key.Name);
                        stationSb.Append(stationName);
                        IReadOnlyDictionary<Item, TransferData> routeTransfers = routeTransfPerStation?[pair.Key];

                        if (TooltipTextForStation(pair.Value, stationSb, routeTransfers, monthMultiplier.Value))
                        {
                            sb.AppendLine().AppendLine().Append(stationSb.ToString());
                        }
                    }
                }
                if (totalTransfers != null && totalTransfers.Count > 0)
                {
                    sb.AppendLine().AppendLine().Append(StringHelper.Colorify(StringHelper.Boldify(locale.GetString("schedule_stopwatch/total_transfer").ToUpper()), UIColors.Solid.Text * 0.8f));
                    foreach (KeyValuePair<Item, TransferData> transfer in totalTransfers)
                    {
                        string countStr = (transfer.Value.unload * monthMultiplier.Value).ToString("N0");
                        if (isRoute && routeToatlTransfers.TryGetValue(transfer.Key, out TransferData routeCount))
                        {
                            countStr += "/" + routeCount.unload.ToString();
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
