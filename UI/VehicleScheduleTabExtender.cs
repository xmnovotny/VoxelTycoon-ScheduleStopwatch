using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch.UI
{
    [Harmony]
    class VehicleScheduleTabExtender : MonoBehaviour
    {
        public VehicleWindowScheduleTab ScheduleTab { get; private set; }
        public VehicleScheduleData ScheduleData { get; private set; }

        public void Initialize(VehicleScheduleData vehicleScheduleData)
        {
            ScheduleTab = gameObject.GetComponent<VehicleWindowScheduleTab>();
            if (ScheduleTab == null)
            {
                throw new NullReferenceException("Component VehicleWindowScheduleTab not found");
            }
            ScheduleData = vehicleScheduleData;
        }

        private void CreateTimeIndicators(VehicleWindowScheduleTabSeparatorView separatorView, int insertIndex)
        {
            Settings settings = Settings.Current;
            RootTask task = ScheduleTab.Vehicle.Schedule.GetTasks()[insertIndex];
            if (insertIndex == 0 && settings.ShowScheduleTotalTime)
            {
                _totalIndicator = ScheduleTotalIndicator.GetInstance(separatorView.transform);
                _totalIndicator.transform.SetSiblingIndex(0);
                _totalIndicator.Initialize(ScheduleData);
                _totalIndicator.gameObject.SetActive(true);

            }
            if (settings.ShowIndividualTaskTimes && task.Behavior != RootTaskBehavior.NonStop)
            {
                ScheduleTaskIndicator indicator = ScheduleTaskIndicator.GetInstance(separatorView.transform);
                indicator.Initialize(task, ScheduleData);
                indicator.gameObject.SetActive(true);
            }
        }

        private void OnSettingsChanged()
        {
            if (ScheduleTab != null)
            {
                ScheduleTab.FillWithTasks();
            }
        }

        protected void OnEnable()
        {
            Settings.Current.Subscribe(OnSettingsChanged);
        }

        protected void OnDisable()
        {
            Settings.Current.Unsubscribe(OnSettingsChanged);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindowScheduleTabSeparatorView), "Initialize")]
        private static void VehicleWindowScheduleTabSeparatorView_Initialize_pof(VehicleWindowScheduleTabSeparatorView __instance, VehicleWindowScheduleTab scheduleTab, int? insertIndex)
        {
            if (!scheduleTab.EditMode && insertIndex.HasValue)
            {
                VehicleScheduleTabExtender tabExt = scheduleTab.gameObject.GetComponent<VehicleScheduleTabExtender>();
                if (tabExt != null)
                {
                    tabExt.CreateTimeIndicators(__instance, insertIndex.Value);
                }
            }
        }

        private ScheduleTotalIndicator _totalIndicator;
        private readonly Dictionary<RootTask, ScheduleTaskIndicator> _taskIndicators = new Dictionary<RootTask, ScheduleTaskIndicator>();
    }
}
