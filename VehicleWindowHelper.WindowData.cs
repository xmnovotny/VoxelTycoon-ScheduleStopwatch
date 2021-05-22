using VoxelTycoon;
using VoxelTycoon.Game.UI;
using UnityEngine.UI;
using System.Collections.Generic;
using VoxelTycoon.Tracks.Tasks;
using System;
using VoxelTycoon.Localization;
using ScheduleStopwatch.UI;

namespace ScheduleStopwatch
{
    partial class VehicleWindowHelper
    {
        private class WindowData
        {
            public VehicleWindow window;

            public WindowData(VehicleWindow window)
            {
                this.window = window;
                indicators = new Dictionary<RootTask, Indicator>();
            }

            public readonly Dictionary<RootTask, Indicator> indicators;
            public Text totalIndicator;

            public TimeSpan? lastTotalTime;
            public IReadOnlyDictionary<Item, int> lastTotalTransfers, lastTotalRouteTransfers;
            public float? lastMonthMultiplier;

            public void Invalidate(VehicleScheduleData data, RootTask task)
            {
                if (task == null)
                {
                    Invalidate(data);
                }
                else
                {
                    InvalidateIndividual(data, task);
                    InvalidateTotal(data);
                }
            }

            public void Invalidate(VehicleScheduleData data)
            {
                foreach (RootTask task in indicators.Keys)
                {
                    InvalidateIndividual(data, task);
                }
                InvalidateTotal(data);
            }

            private void InvalidateTotal(VehicleScheduleData data)
            {
                if (totalIndicator != null)
                {
                    Locale locale = LazyManager<LocaleManager>.Current.Locale;
                    lastTotalTime = data.ScheduleAvereageDuration;
                    lastMonthMultiplier = data.ScheduleMonthlyMultiplier;
                    if (lastTotalTime.HasValue)
                    {
                        totalIndicator.text = locale.GetString("schedule_stopwatch/days_hours").Format(lastTotalTime.Value.TotalDays.ToString("N0"), lastTotalTime.Value.Hours.ToString("N0"));
                    }
                    else
                    {
                        totalIndicator.text = locale.GetString("schedule_stopwatch/unknown").ToUpper();
                    }

                    lastTotalTransfers = data.Capacity.GetTotalTransfers();
                    lastTotalRouteTransfers = data.Capacity.GetRouteTotalTransfers();
                    totalIndicator.transform.parent.GetComponent<CargoCapacityIndicator>().UpdateItems(lastTotalTransfers, lastMonthMultiplier, lastTotalRouteTransfers);
                }
            }

            private void InvalidateIndividual(VehicleScheduleData data, RootTask task)
            {
                Locale locale = LazyManager<LocaleManager>.Current.Locale;
                if (indicators.TryGetValue(task, out Indicator indicator))
                {
                    TimeSpan? travel = data.GetAverageTravelDuration(task);
                    if (travel.HasValue)
                    {
                        indicator.travel.text = locale.GetString("schedule_stopwatch/days_hours").Format(travel.Value.TotalDays.ToString("N0"), travel.Value.Hours.ToString("N0"));
                    }
                    else
                    {
                        indicator.travel.text = locale.GetString("schedule_stopwatch/unknown").ToUpper();
                    }
                    TimeSpan? loading = data.GetAverageStationLoadingDuration(task);
                    if (loading.HasValue)
                    {
                        indicator.loading.text = locale.GetString("schedule_stopwatch/hours_minutes").Format(loading.Value.TotalHours.ToString("N0"), loading.Value.Minutes.ToString("N0"));
                    }
                    else
                    {
                        indicator.loading.text = locale.GetString("schedule_stopwatch/unknown").ToUpper();
                    }
                }
            }


        }
    }
}
