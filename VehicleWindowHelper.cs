using HarmonyLib;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using UnityEngine.UI;
using VoxelTycoon.UI.Controls;
using System.Collections.Generic;
using VoxelTycoon.Tracks.Tasks;
using System;

namespace ScheduleStopwatch
{
    [HarmonyPatch]
    class VehicleWindowHelper
    {
        private struct Indicator
        {
            public Text travel, loading;
        }

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

            public void Invalidate(VehicleScheduleData data, RootTask task)
            {
                if (indicators.TryGetValue(task, out var indicator))
                {
                    TimeSpan? travel = data.GetAverageTravelDuration(task);
                    if (travel.HasValue) {
                        indicator.travel.text = string.Format("{0}d {1}h", travel.Value.TotalDays.ToString("N0"), travel.Value.Hours.ToString("N0"));
                    } else
                    {
                        indicator.travel.text = "UNKNOWN";
                    }
                    TimeSpan? loading = data.GetAverageStationLoadingDuration(task);
                    if (loading.HasValue)
                    {
                        indicator.loading.text = string.Format("{0}h {1}m", loading.Value.TotalHours.ToString("N0"), loading.Value.Minutes.ToString("N0"));
                    }
                    else
                    {
                        indicator.loading.text = "UNKNOWN";
                    }
                }

                if (totalIndicator != null)
                {
                    TimeSpan? time = data.ScheduleAvereageDuration;
                    totalIndicator.text = time.HasValue ? string.Format("{0}d {1}h", time.Value.TotalDays.ToString("N0"), time.Value.Hours.ToString("N0")) : "Unknown".ToUpper();
                }
            }

            public void Invalidate(VehicleScheduleData data)
            {
                foreach (RootTask task in indicators.Keys)
                {
                    Invalidate(data, task);
                }
            }

        }

        private static Transform TaskTimeTemplate, TotalTimeTemplate;
        private static readonly Dictionary<VehicleWindow, WindowData> _windows = new Dictionary<VehicleWindow, WindowData>();

        static VehicleWindowHelper()
        {
            Settings.Current.Subscribe(OnSettingsChanged);
        }

        private static void OnSettingsChanged()
        {
            foreach (var window in _windows.Keys)
            {
                VehicleWindowScheduleTab tab = window.transform.GetComponentInChildren<VehicleWindowScheduleTab>();
                if (tab && !tab.EditMode)
                {
                    tab.FillWithTasks();
                }
            }
        }

        private static void CreateTemplates(VehicleWindowScheduleTabSeparatorView separatorView)
        {
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
            text.text = "Unknown".ToUpper();

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
            totalLabel.GetComponent<Text>().text = "TOTAL TIME: ";

            Transform textTransform2 = TotalTimeTemplate.Find("LoadingTimeText");
            textTransform2.name = "TotalTimeText";
            Text text2 = textTransform2.GetComponent<Text>();
            text2.text = "Unknown".ToUpper();
        }

        private static void CreateTotalTimeIndicator(VehicleWindowScheduleTabSeparatorView separatorView, VehicleWindowScheduleTab scheduleTab)
        {
            Transform transform = UnityEngine.Object.Instantiate<Transform>(TotalTimeTemplate, separatorView.transform);
            transform.name = "TotalDuration";
            transform.SetSiblingIndex(0);
            transform.gameObject.SetActive(true);

            _windows[scheduleTab.Window].totalIndicator = transform.Find("TotalTimeText").GetComponent<Text>();
        }

        private static void CreateTaskTimeIndicator(VehicleWindowScheduleTabSeparatorView separatorView, VehicleWindowScheduleTab scheduleTab, RootTask task)
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

        private static void CreateTimeIndicators(VehicleWindowScheduleTabSeparatorView separatorView, VehicleWindowScheduleTab scheduleTab, int insertIndex)
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
            _windows.Add(__instance, new WindowData(__instance));
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindow), "Initialize")]
        private static void VehicleWindow_Initialize_pof(VehicleWindow __instance)
        {
            if (_windows.TryGetValue(__instance, out WindowData windowData))
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
            if (_windows.TryGetValue(__instance, out WindowData windowData))
            {
                VehicleScheduleDataManager.Current.UnsubscribeDataChanged(__instance.Vehicle, windowData.Invalidate);
                _windows.Remove(__instance);
            }
        }
        #endregion
        #region VehicleWindowScheduleTab
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VehicleWindowScheduleTab), "FillWithTasks")]
        private static void VehicleWindowScheduleTab_FillWithTasks_prf(VehicleWindowScheduleTab __instance)
        {
            if (_windows.TryGetValue(__instance.Window, out WindowData data))
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
            if (scheduleData != null && _windows.TryGetValue(__instance.Window, out WindowData data))
            {
                data.Invalidate(scheduleData);
            }
        }

        #endregion

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindowScheduleTabSeparatorView), "Initialize")]
        private static void VehicleWindowScheduleTabSeparatorView_Initialize_pof(VehicleWindowScheduleTabSeparatorView __instance, VehicleWindowScheduleTab scheduleTab, int? insertIndex)
        {
            if (TaskTimeTemplate == null)
                CreateTemplates(__instance);
            if (!scheduleTab.EditMode && insertIndex.HasValue)
            {
                CreateTimeIndicators(__instance, scheduleTab, insertIndex.Value);
            }
        }
        #endregion
    }
}
