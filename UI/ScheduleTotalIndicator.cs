using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Game.UI.Formatting;
using VoxelTycoon.Localization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;
using VoxelTycoon.UI;
using static ScheduleStopwatch.TaskDurationDataSet;
using static ScheduleStopwatch.VehicleScheduleCapacity;
using TransfersPerStationCont = ScheduleStopwatch.VehicleScheduleCapacity.TransfersPerStationCont;

namespace ScheduleStopwatch.UI
{
    class ScheduleTotalIndicator: ScheduleIndicator
    {
        private Text _text;
        private DurationData _lastTotalTime;
        private float? _lastMonthMultiplier;
        private IReadOnlyDictionary<Item, TransferData> _lastTotalTransfers;
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

        private IReadOnlyDictionary<Item, TransferData> RouteTotalTransfers
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

        public void Initialize(VehicleScheduleData data, Settings settings)
        {
            Transform timeIndicator = transform.Find("TimeIndicator");
            if (settings.ShowScheduleTotalTime)
            {
                _text = timeIndicator.Find("TotalTimeText").GetComponent<Text>();
                Locale locale = LazyManager<LocaleManager>.Current.Locale;
                Tooltip.For(timeIndicator,
                            GetTotalTimeTooltipText,
                            0);
            } else
            {
                timeIndicator.SetActive(false);
            }

            if (settings.ShowTotalTransferCapacity)
            {
                _capacityIndicator = transform.GetComponentInChildren<CargoCapacityIndicator>();
                _capacityIndicator.Initialize(null, null);
                Tooltip.For(
                    _capacityIndicator,
                    () => GetCapacityTooltipText(),
                    0
                );
            }
            _scheduleData = data;
            UpdateValues(data, null);
        }

        public void UpdateValues(VehicleScheduleData data, RootTask _)
        {
            if (data != _scheduleData)
                throw new ArgumentException("Schedule data is not for this ScheduleTotalIndicator");

            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            IReadOnlyDictionary<Item, TransferData> routeTotalTransfers = null;
            if (_capacityIndicator != null)
            {
                _lastTotalTransfers = data.Capacity.GetTotalTransfers();
                routeTotalTransfers = RouteTotalTransfers;
            }
            int itemsLimit = routeTotalTransfers != null ? 7 : 10;
            _lastTransfersPerStation = null;
            _lastMonthMultiplier = data.ScheduleMonthlyMultiplier;
            if (_text != null)
            {
                itemsLimit = routeTotalTransfers != null ? 3 : 5;
                _lastTotalTime = data.ScheduleAvereageDuration;
                if (_lastTotalTime != null)
                {
                    _text.text = locale.GetString("schedule_stopwatch/days_hours").Format(((int)_lastTotalTime.Duration.TotalDays).ToString("N0"), _lastTotalTime.Duration.Hours.ToString("N0"));
                    if (_lastTotalTime.Estimated)
                    {
                        _text.color = Color.gray;
                    } else
                    {
                        _text.color = Color.black;
                    }
                }
                else
                {
                    _text.text = locale.GetString("schedule_stopwatch/unknown").ToUpper();
                }
            }

            if (_capacityIndicator != null)
            {
                _lastTotalTransfers = data.Capacity.GetTotalTransfers();
                _capacityIndicator.UpdateItems(_lastTotalTransfers, _lastMonthMultiplier, routeTotalTransfers, itemsLimit: itemsLimit, transfDirection: TransferDirection.unloading);
            }
        }

        private string GetTotalTimeTooltipText()
        {
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            if (_lastTotalTime != null)
            {
                string result = locale.GetString("schedule_stopwatch/times_per_month").Format((Convert.ToSingle((30 * 86400) / _lastTotalTime.Duration.TotalSeconds)).ToString("N1", LazyManager<LocaleManager>.Current.Locale.CultureInfo));
                float? speed = _scheduleData.AverageSpeed;
                if (speed != null)
                {
                    result += "\n\n" + locale.GetString("schedule_stopwatch/average_schedule_speed").Format(StringHelper.Boldify(UIFormat.Units.FormatVelocity(speed.Value, true)), UIFormat.Units.VelocityUnits);
                } else
                {
                    result += "\n\n" + locale.GetString("schedule_stopwatch/average_schedule_speed").Format(locale.GetString("schedule_stopwatch/unknown"), "");
                }

                result += "\n" + locale.GetString("schedule_stopwatch/ratio_loading_total").Format(StringHelper.Boldify((_scheduleData.ScheduleStationLoadingAvereageDuration.Duration.TotalSeconds / _lastTotalTime.Duration.TotalSeconds * 100).ToString("N0")));
                return result;
            } else
            {
                return locale.GetString("schedule_stopwatch/missing_time_segment");
            }
        }

        private string GetCapacityTooltipText()
        {
            return ScheduleCapacityHelper.GetCapacityTooltipText(_lastMonthMultiplier, _lastTotalTransfers, LastTransfersPerStation, RouteTotalTransfers, RouteTransfersPerStation);
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
//            totalLabel.GetComponent<Text>().text = "∑";
            totalLabel.GetComponent<Text>().text = "";
            totalLabel.GetComponent<Text>().font = R.Fonts.FontAwesome5FreeSolid900;

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
