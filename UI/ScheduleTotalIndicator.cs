using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Localization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;
using VoxelTycoon.UI;
using TransfersPerStationCont = ScheduleStopwatch.VehicleScheduleCapacity.TransfersPerStationCont;

namespace ScheduleStopwatch.UI
{
    class ScheduleTotalIndicator: ScheduleIndicator
    {
        private Text _text;
        private TimeSpan? _lastTotalTime;
        private float? _lastMonthMultiplier;
        private IReadOnlyDictionary<Item, int> _lastTotalTransfers;
        private TransfersPerStationCont _lastTransfersPerStation;
        private CargoCapacityIndicator _capacityIndicator;
        private VehicleScheduleData _scheduleData;
        private static ScheduleTotalIndicator _template;
        private TransfersPerStationCont LastTransfersPerStation
        {
            get
            {
                if (_lastTransfersPerStation == null && _lastTotalTransfers != null)
                {
                    _lastTransfersPerStation = _scheduleData.Capacity.GetTransfersPerStation();
                }
                return _lastTransfersPerStation;
            }
        }
        private TransfersPerStationCont RouteTransfersPerStation
        {
            get
            {
                if (_lastTotalTransfers != null && _scheduleData.Vehicle.Route?.Vehicles.Count > 0)
                {
                    return LazyManager<RouteCapacityCache>.Current.GetRouteTransfersPerStation(_scheduleData.Vehicle.Route);
                }
                return null;
            }
        }

        private IReadOnlyDictionary<Item, int> RouteTotalTransfers
        {
            get
            {
                if (_lastTotalTransfers != null && _scheduleData.Vehicle.Route?.Vehicles.Count > 0)
                {
                    return LazyManager<RouteCapacityCache>.Current.GetRouteTotalTransfers(_scheduleData.Vehicle.Route);
                }
                return null;
            }
        }

        public static ScheduleTotalIndicator GetInstance(Transform parent)
        {
            if (_template == null)
            {
                CreateTemplate();
            }
            ScheduleTotalIndicator result = UnityEngine.Object.Instantiate<ScheduleTotalIndicator>(_template, parent);
            result.transform.name = "TotalDuration";
            return result;
        }

        public void Initialize(VehicleScheduleData data)
        {
            Transform timeIndicator = transform.Find("TimeIndicator");

            _text = timeIndicator.Find("TotalTimeText").GetComponent<Text>();
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            Tooltip.For(
                timeIndicator,
                () => _lastTotalTime.HasValue
                    ? locale.GetString("schedule_stopwatch/times_per_month").Format((Convert.ToSingle((30 * 86400) / _lastTotalTime.Value.TotalSeconds)).ToString("N1", LazyManager<LocaleManager>.Current.Locale.CultureInfo))
                    : locale.GetString("schedule_stopwatch/missing_time_segment"),
                null
            );
            _capacityIndicator = transform.GetComponentInChildren<CargoCapacityIndicator>();
            _capacityIndicator.Initialize(null, null);
            Tooltip.For(
                _capacityIndicator,
                () => GetCapacityTooltipText(),
                null
            );
            _scheduleData = data;
            UpdateValues(data, null);
        }

        public void UpdateValues(VehicleScheduleData data, RootTask _)
        {
            if (data != _scheduleData)
                throw new ArgumentException("Schedule data is not for this ScheduleTotalIndicator");

            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            _lastTransfersPerStation = null;
            _lastTotalTime = data.ScheduleAvereageDuration;
            _lastMonthMultiplier = data.ScheduleMonthlyMultiplier;
            if (_lastTotalTime.HasValue)
            {
                _text.text = locale.GetString("schedule_stopwatch/days_hours").Format(_lastTotalTime.Value.TotalDays.ToString("N0"), _lastTotalTime.Value.Hours.ToString("N0"));
            }
            else
            {
                _text.text = locale.GetString("schedule_stopwatch/unknown").ToUpper();
            }

            _lastTotalTransfers = data.Capacity.GetTotalTransfers();
            _capacityIndicator.UpdateItems(_lastTotalTransfers, _lastMonthMultiplier, RouteTotalTransfers);
        }

        private string GetCapacityTooltipText()
        {
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            if (_lastMonthMultiplier != null && _lastTotalTransfers != null && _lastTotalTransfers.Count>0)
            {
                IReadOnlyDictionary<Item, int> routeToalTransfers = RouteTotalTransfers;
                bool isRoute = routeToalTransfers != null && routeToalTransfers.Count>0;
                StringBuilder sb = new StringBuilder();
                sb.Append(StringHelper.Boldify(locale.GetString("schedule_stopwatch/estim_monthly_transf").ToUpper()));
                if (isRoute)
                {
                    sb.AppendLine().Append(StringHelper.Colorify(locale.GetString("schedule_stopwatch/estim_monthly_transf_hint"),UIColors.Solid.Text * 0.5f));
                }
                TransfersPerStationCont transfPerSt = LastTransfersPerStation;
                if (transfPerSt != null && transfPerSt.Count>0)
                {
                    TransfersPerStationCont routeTransfPerst = null;
                    if (isRoute)
                    {
                        routeTransfPerst = RouteTransfersPerStation;
                    }
                    foreach (KeyValuePair<VehicleStation, IReadOnlyDictionary<Item, int>> pair in transfPerSt)
                    {
                        StringBuilder stationSb = new StringBuilder();
                        string stationName = StringHelper.Boldify(pair.Key.Location.Name);
                        stationSb.Append(stationName);
                        IReadOnlyDictionary<Item, int> routeTransfers = routeTransfPerst?[pair.Key];

                        if (TooltipTextForStation(pair.Value, stationSb, routeTransfers, _lastMonthMultiplier.Value))
                        {
                            sb.AppendLine().Append(stationSb.ToString());
                        }
                    }
                    sb.AppendLine().Append(StringHelper.Colorify(StringHelper.Boldify(locale.GetString("schedule_stopwatch/total_transfer").ToUpper()), UIColors.Solid.Text * 0.8f));
                    foreach (KeyValuePair<Item, int> transfer in _lastTotalTransfers)
                    {
                        string countStr = (transfer.Value * _lastMonthMultiplier.Value).ToString("N0");
                        if (isRoute && routeToalTransfers.TryGetValue(transfer.Key, out int routeCount))
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

        private static void CreateTemplate()
        {
            var baseTemplate = UnityEngine.Object.Instantiate<Transform>(ScheduleIndicator.BaseTemplate);
            baseTemplate.gameObject.name = "TotalContainer";
            baseTemplate.gameObject.SetActive(false);
            Transform timeContainer = baseTemplate.Find("TimeIndicator");
            timeContainer.gameObject.AddComponent<CanvasRenderer>();
            timeContainer.gameObject.AddComponent<NonDrawingGraphic>();

            timeContainer.Find("Icon").DestroyGameObject(true);
            _template = baseTemplate.gameObject.AddComponent<ScheduleTotalIndicator>();
            Transform totalLabel = timeContainer.Find("Text");
            totalLabel.name = "Label";
            totalLabel.GetComponent<Text>().text = "∑";

            Transform textTransform2 = UnityEngine.Object.Instantiate<Transform>(totalLabel, totalLabel.parent);
            textTransform2.name = "TotalTimeText";
            Text text2 = textTransform2.GetComponent<Text>();
            text2.text = LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/unknown").ToUpper();

            CargoCapacityIndicator.GetInstance(baseTemplate.transform);
        }

        protected void OnEnable()
        {
            if (_scheduleData != null)
            {
                _scheduleData.SubscribeDataChanged(UpdateValues);
                UpdateValues(_scheduleData, null);
            }
        }
        protected void OnDisable()
        {
            if (_scheduleData != null)
            {
                _scheduleData.UnsubscribeDataChanged(UpdateValues);
            }
        }
    }
}
