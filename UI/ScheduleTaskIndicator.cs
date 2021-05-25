using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Localization;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch.UI
{
    class ScheduleTaskIndicator : ScheduleIndicator
    {
        public RootTask Task { get; private set; }

        public static ScheduleTaskIndicator GetInstance(Transform parent)
        {
            if (_template == null)
            {
                CreateTemplate();
            }
            return UnityEngine.Object.Instantiate<ScheduleTaskIndicator>(_template, parent);
        }

        public void Initialize(RootTask task, VehicleScheduleData data)
        {
            FileLog.Log("TaskIndicator Initialize");
            Task = task;
            _scheduleData = data;
            transform.name = "StopWatchDuration";

            _travelTimeText = transform.Find("TimeIndicator/TravelTimeText").GetComponent<Text>();
            _loadingTimeText = transform.Find("TimeIndicator/LoadingTimeText").GetComponent<Text>();
            transform.gameObject.SetActive(true);
//            UpdateValues(data, task);
        }

        public void UpdateValues(VehicleScheduleData data, RootTask task)
        {
            if ((task != null && task != Task) || data != _scheduleData || data == null)
            {
                throw new ArgumentException("Schedule data or task is not for this ScheduleTaskIndicator");
            }
            TimeSpan? travel = data.GetAverageTravelDuration(Task);
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            if (travel.HasValue)
            {
                _travelTimeText.text = locale.GetString("schedule_stopwatch/days_hours").Format(travel.Value.TotalDays.ToString("N0"), travel.Value.Hours.ToString("N0"));
            }
            else
            {
                _travelTimeText.text = locale.GetString("schedule_stopwatch/unknown").ToUpper();
            }
            TimeSpan? loading = data.GetAverageStationLoadingDuration(Task);
            if (loading.HasValue)
            {
                _loadingTimeText.text = locale.GetString("schedule_stopwatch/hours_minutes").Format(loading.Value.TotalHours.ToString("N0"), loading.Value.Minutes.ToString("N0"));
            }
            else
            {
                _loadingTimeText.text = locale.GetString("schedule_stopwatch/unknown").ToUpper();
            }
        }

        private static void CreateTemplate()
        {
            var baseTemplate = UnityEngine.Object.Instantiate<Transform>(ScheduleIndicator.BaseTemplate);
            baseTemplate.gameObject.SetActive(false);
            _template = baseTemplate.gameObject.AddComponent<ScheduleTaskIndicator>();
            Transform timeContainer = baseTemplate.Find("TimeIndicator");
            Transform iconTransform = timeContainer.transform.Find("Icon");
            iconTransform.name = "TravelTimeIcon";
            Text iconText = iconTransform.GetComponent<Text>();
            iconText.text = "";
            iconText.font = R.Fonts.Ketizoloto;

            Transform textTransform = timeContainer.transform.Find("Text");
            textTransform.name = "TravelTimeText";
            Text text = textTransform.GetComponent<Text>();
            text.text = LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/unknown").ToUpper();

            Transform loadingIcon = UnityEngine.Object.Instantiate<Transform>(iconTransform, iconTransform.parent);
            loadingIcon.name = "LoadingTimeIcon";
            Text iconText2 = loadingIcon.GetComponent<Text>();
            iconText2.text = "";

            Transform loadingTextTransf = UnityEngine.Object.Instantiate<Transform>(textTransform, textTransform.parent);
            loadingTextTransf.name = "LoadingTimeText";
        }

        protected void OnEnable()
        {
            FileLog.Log("TaskInicator OnEnable");
            if (_scheduleData != null)
            {
                _scheduleData.SubscribeTaskDataChanged(Task, UpdateValues);
                FileLog.Log("TaskInicator OnEnable Subscribe");

                UpdateValues(_scheduleData, Task);
            }
        }

        protected void OnDisable()
        {
            FileLog.Log("TaskInicator OnDisable");
            if (_scheduleData != null && Task != null)
            {
                _scheduleData.UnsubscribeTaskDataChanged(Task, UpdateValues);
            }
        }

        private Text _travelTimeText, _loadingTimeText;
        /*        private TimeSpan? _lastTotalTime;
                private float? _lastMonthMultiplier;
                private IReadOnlyDictionary<Item, int> _lastTotalTransfers;
                private IReadOnlyDictionary<Item, int> _lastTotalRouteTransfers;
        private CargoCapacityIndicator _capacityIndicator;*/
        private static ScheduleTaskIndicator _template;
        private VehicleScheduleData _scheduleData;
    }
}
