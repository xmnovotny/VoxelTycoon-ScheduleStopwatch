﻿using HarmonyLib;
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

            _loadCapacityIndicator = transform.Find("CargoCapacityLoad").GetComponent<CargoCapacityIndicator>();
            _loadCapacityIndicator.Initialize(null, null);

            _unloadCapacityIndicator = transform.Find("CargoCapacityUnload").GetComponent<CargoCapacityIndicator>();
            _unloadCapacityIndicator.Initialize(null, null);

            _loadingCapIcon = transform.Find("LoadingCapacityIcon");
            _unloadingCapIcon = transform.Find("UnloadingCapacityIcon");

            transform.gameObject.SetActive(true);
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
            _lastMonthMultiplier = data.ScheduleMonthlyMultiplier;
            _lastTaskTransfers = _scheduleData.Capacity.GetTransfers(Task);
            IReadOnlyDictionary<Item, int> routeTransfers = RouteTaskTransfers;


            _loadCapacityIndicator.UpdateItems(_lastTaskTransfers, _lastMonthMultiplier, routeTransfers, transfDirection: CargoCapacityIndicator.TransferDirection.loading);
            _loadCapacityIndicator.gameObject.SetActive(_loadCapacityIndicator.ItemsCount > 0);
            _loadingCapIcon.gameObject.SetActive(_loadCapacityIndicator.ItemsCount > 0);
        
            _unloadCapacityIndicator.UpdateItems(_lastTaskTransfers, _lastMonthMultiplier, routeTransfers, transfDirection: CargoCapacityIndicator.TransferDirection.unloading);
            _unloadCapacityIndicator.gameObject.SetActive(_unloadCapacityIndicator.ItemsCount > 0);
            _unloadingCapIcon.gameObject.SetActive(_unloadCapacityIndicator.ItemsCount > 0);
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

            Transform unloadingCapIcon = UnityEngine.Object.Instantiate<Transform>(iconTransform, baseTemplate.transform);
            unloadingCapIcon.name = "UnloadingCapacityIcon";
            Text iconText4 = unloadingCapIcon.GetComponent<Text>();
            iconText4.font = R.Fonts.FontAwesome5FreeSolid900;
            iconText4.text = "";

            CargoCapacityIndicator unloadIndicator = CargoCapacityIndicator.GetInstance(baseTemplate.transform);
            unloadIndicator.transform.name = "CargoCapacityUnload";

            Transform loadingCapIcon = UnityEngine.Object.Instantiate<Transform>(iconTransform, baseTemplate.transform);
            loadingCapIcon.name = "LoadingCapacityIcon";
            Text iconText3 = loadingCapIcon.GetComponent<Text>();
            iconText3.font = R.Fonts.FontAwesome5FreeSolid900;
            iconText3.text = "";

            CargoCapacityIndicator loadIndicator = CargoCapacityIndicator.GetInstance(baseTemplate.transform);
            loadIndicator.transform.name = "CargoCapacityLoad";
        }

        private IReadOnlyDictionary<Item, int> RouteTaskTransfers
        {
            get
            {
                if (_lastTaskTransfers != null && _scheduleData.Vehicle.Route?.Vehicles.Count > 0)
                {
                    return LazyManager<RouteCapacityCache>.Current.GetRouteTaskTransfers(_scheduleData.Vehicle.Route, Task);
                }
                return null;
            }
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
        private float? _lastMonthMultiplier;
        private IReadOnlyDictionary<Item, int> _lastTaskTransfers;
        private static ScheduleTaskIndicator _template;
        private VehicleScheduleData _scheduleData;
        private CargoCapacityIndicator _loadCapacityIndicator, _unloadCapacityIndicator;
        private Transform _loadingCapIcon, _unloadingCapIcon;

    }
}
