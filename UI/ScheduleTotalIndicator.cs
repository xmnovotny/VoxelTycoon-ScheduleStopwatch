using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Localization;
using VoxelTycoon.Tracks.Tasks;
using VoxelTycoon.UI;

namespace ScheduleStopwatch.UI
{
    class ScheduleTotalIndicator: ScheduleIndicator
    {
        private Text _text;
        private TimeSpan? _lastTotalTime;
        private float? _lastMonthMultiplier;
        private IReadOnlyDictionary<Item, int> _lastTotalTransfers;
        private IReadOnlyDictionary<Item, int> _lastTotalRouteTransfers;
        private CargoCapacityIndicator _capacityIndicator;
        private VehicleScheduleData _scheduleData;
        private static ScheduleTotalIndicator _template;

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
            _scheduleData = data;
            UpdateValues(data, null);
        }

        public void UpdateValues(VehicleScheduleData data, RootTask _)
        {
            if (data != _scheduleData)
                throw new ArgumentException("Schedule data is not for this ScheduleTotalIndicator");

            Locale locale = LazyManager<LocaleManager>.Current.Locale;
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
            _lastTotalRouteTransfers = data.Capacity.GetRouteTotalTransfers();
            _capacityIndicator.UpdateItems(_lastTotalTransfers, _lastMonthMultiplier, _lastTotalRouteTransfers);
        }

        private static void CreateTemplate()
        {
            var baseTemplate = UnityEngine.Object.Instantiate<Transform>(ScheduleIndicator.BaseTemplate);
            baseTemplate.gameObject.name = "TotalContainer";
            baseTemplate.gameObject.SetActive(false);
            Transform timeContainer = baseTemplate.Find("TimeIndicator");
            timeContainer.gameObject.AddComponent<CanvasRenderer>();
            timeContainer.gameObject.AddComponent<NonDrawingGraphic>();
            FileLog.Log(XMNUtils.GameObjectDumper.DumpGameObject(baseTemplate.gameObject));

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
