using HarmonyLib;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using UnityEngine.UI;
using VoxelTycoon.UI.Controls;
using System.Collections.Generic;
using VoxelTycoon.Tracks.Tasks;
using System;
using VoxelTycoon.UI;
using VoxelTycoon.Localization;
using VoxelTycoon.Game.UI.VehicleUnitPickerWindowViews;
using ScheduleStopwatch.UI;

namespace ScheduleStopwatch
{
    [HarmonyPatch]
    partial class VehicleWindowHelper: LazyManager<VehicleWindowHelper>
    {
        private struct Indicator
        {
            public Text travel, loading;
        }

        private Transform TaskTimeTemplate, TotalTimeTemplate;
        private Dictionary<VehicleWindow, WindowData> _windows = new Dictionary<VehicleWindow, WindowData>();

        protected override void OnInitialize()
        {
            Settings.Current.Subscribe(OnSettingsChanged);
        }

        private void OnSettingsChanged()
        {
            FileLog.Log("OnSettingsChange, count: "+_windows.Count.ToString());
            foreach (var window in _windows.Keys)
            {
                VehicleWindowScheduleTab tab = window.transform.GetComponentInChildren<VehicleWindowScheduleTab>();
                if (tab && !tab.EditMode)
                {
                    tab.FillWithTasks();
                }
            }
        }

        private void CreateTemplates(VehicleWindowScheduleTabSeparatorView separatorView)
        {
            Locale locale = LazyManager<LocaleManager>.Current.Locale;

            TaskTimeTemplate = UnityEngine.Object.Instantiate<Transform>(separatorView.transform.Find("AddStop"));
            GameObject.DestroyImmediate(TaskTimeTemplate.GetComponent<ButtonEx>());
            GameObject.DestroyImmediate(TaskTimeTemplate.GetComponent<ClickableDecorator>());
            Transform iconTransform = TaskTimeTemplate.Find("Icon");
            iconTransform.name = "TravelTimeIcon";
            Text iconText = iconTransform.GetComponent<Text>();
            iconText.text = "";
            iconText.font = R.Fonts.Ketizoloto;

            Transform textTransform = TaskTimeTemplate.Find("Text");
            textTransform.name = "TravelTimeText";
            Text text = textTransform.GetComponent<Text>();
            text.text = locale.GetString("schedule_stopwatch/unknown").ToUpper();

            Transform loadingIcon = UnityEngine.Object.Instantiate<Transform>(iconTransform, iconTransform.parent);
            loadingIcon.name = "LoadingTimeIcon";
            Text iconText2 = loadingIcon.GetComponent<Text>();
            iconText2.text = "";

            Transform loadingTextTransf = UnityEngine.Object.Instantiate<Transform>(textTransform, textTransform.parent);
            loadingTextTransf.name = "LoadingTimeText";

            TotalTimeTemplate = UnityEngine.Object.Instantiate<Transform>(TaskTimeTemplate);
            TotalTimeTemplate.Find("TravelTimeIcon").DestroyGameObject(true);
            TotalTimeTemplate.Find("LoadingTimeIcon").DestroyGameObject(true);
            Transform totalLabel = TotalTimeTemplate.Find("TravelTimeText");
            totalLabel.name = "Label";
            totalLabel.GetComponent<Text>().text = "∑";

            Transform textTransform2 = TotalTimeTemplate.Find("LoadingTimeText");
            textTransform2.name = "TotalTimeText";
            Text text2 = textTransform2.GetComponent<Text>();
            text2.text = locale.GetString("schedule_stopwatch/unknown").ToUpper();
        }

        private void CreateTotalTimeIndicator(VehicleWindowScheduleTabSeparatorView separatorView, VehicleWindowScheduleTab scheduleTab)
        {
            Transform transform = UnityEngine.Object.Instantiate<Transform>(TotalTimeTemplate, separatorView.transform);
            transform.name = "TotalDuration";
            transform.SetSiblingIndex(0);
            transform.gameObject.SetActive(true);

            WindowData windowData = _windows[scheduleTab.Window];

            windowData.totalIndicator = transform.Find("TotalTimeText").GetComponent<Text>();
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            Tooltip.For(
                windowData.totalIndicator.transform, 
                () => windowData.lastTotalTime.HasValue 
                    ? locale.GetString("schedule_stopwatch/times_per_month").Format((Convert.ToSingle((30 * 86400) / windowData.lastTotalTime.Value.TotalSeconds)).ToString("N1", LazyManager<LocaleManager>.Current.Locale.CultureInfo)) 
                    : locale.GetString("schedule_stopwatch/missing_time_segment"), 
                null
            );
            windowData.totalIndicator.transform.parent.gameObject.AddComponent<CargoCapacityIndicator>();
        }

        private void CreateTaskTimeIndicator(VehicleWindowScheduleTabSeparatorView separatorView, VehicleWindowScheduleTab scheduleTab, RootTask task)
        {
            Transform transform = UnityEngine.Object.Instantiate<Transform>(TaskTimeTemplate, separatorView.transform);
            transform.name = "StopWatchDuration";
            transform.gameObject.SetActive(true);

            Indicator indicator;
            indicator.travel = transform.Find("TravelTimeText").GetComponent<Text>();
            indicator.loading = transform.Find("LoadingTimeText").GetComponent<Text>();

            WindowData data = _windows[scheduleTab.Window];
            data.indicators.Add(task, indicator);
        }

        private void CreateTimeIndicators(VehicleWindowScheduleTabSeparatorView separatorView, VehicleWindowScheduleTab scheduleTab, int insertIndex)
        {
            Settings settings = Settings.Current;
            RootTask task = scheduleTab.Vehicle.Schedule.GetTasks()[insertIndex];
            if (insertIndex == 0 && settings.ShowScheduleTotalTime)
            {
                CreateTotalTimeIndicator(separatorView, scheduleTab);
            }
            if (settings.ShowIndividualTaskTimes && task.Behavior != RootTaskBehavior.NonStop)
            {
                CreateTaskTimeIndicator(separatorView, scheduleTab, task);
            }
        }

        #region HARMONY
        #region VehicleWindow
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VehicleWindow), "Initialize")]
        private static bool VehicleWindow_Initialize_prf(VehicleWindow __instance)
        {
            Current._windows.Add(__instance, new WindowData(__instance));
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindow), "Initialize")]
        private static void VehicleWindow_Initialize_pof(VehicleWindow __instance)
        {
            if (Current._windows.TryGetValue(__instance, out WindowData windowData))
            {
                VehicleScheduleDataManager.Current.SubscribeDataChanged(__instance.Vehicle, windowData.Invalidate);
                VehicleScheduleData scheduleData = VehicleScheduleDataManager.Current[__instance.Vehicle];
                if (scheduleData != null)
                {
                    windowData.Invalidate(scheduleData);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindow), "OnClose")]
        private static void VehicleWindow_OnClose_pof(VehicleWindow __instance)
        {
            FileLog.Log("OnClose");
            if (Current._windows.TryGetValue(__instance, out WindowData windowData))
            {
                FileLog.Log("OnCloseRemove");
                VehicleScheduleDataManager.Current.UnsubscribeDataChanged(__instance.Vehicle, windowData.Invalidate);
                Current._windows.Remove(__instance);
            }
        }
        #endregion
        #region VehicleWindowScheduleTab
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VehicleWindowScheduleTab), "FillWithTasks")]
        private static void VehicleWindowScheduleTab_FillWithTasks_prf(VehicleWindowScheduleTab __instance)
        {
            if (Current._windows.TryGetValue(__instance.Window, out WindowData data))
            {
                data.indicators.Clear();
                data.totalIndicator = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindowScheduleTab), "FillWithTasks")]
        private static void VehicleWindowScheduleTab_FillWithTasks_pof(VehicleWindowScheduleTab __instance)
        {
            VehicleScheduleData scheduleData = VehicleScheduleDataManager.Current[__instance.Vehicle];
            if (scheduleData != null && Current._windows.TryGetValue(__instance.Window, out WindowData data))
            {
                data.Invalidate(scheduleData);
            }
        }

        #endregion

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindowScheduleTabSeparatorView), "Initialize")]
        private static void VehicleWindowScheduleTabSeparatorView_Initialize_pof(VehicleWindowScheduleTabSeparatorView __instance, VehicleWindowScheduleTab scheduleTab, int? insertIndex)
        {
            VehicleWindowHelper current = Current;
            if (current.TaskTimeTemplate == null)
                current.CreateTemplates(__instance);
            if (!scheduleTab.EditMode && insertIndex.HasValue)
            {
                current.CreateTimeIndicators(__instance, scheduleTab, insertIndex.Value);
            }
        }
        #endregion
    }
}
