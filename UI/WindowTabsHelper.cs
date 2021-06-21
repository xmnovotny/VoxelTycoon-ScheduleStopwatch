using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.UI.Controls;

namespace ScheduleStopwatch.UI
{
    [HarmonyPatch]
    class WindowTabsHelper
    {
        static Transform _lastSelectedTabContent;

        private static Func<WindowTabs, Transform> getSelectedTabContentFunc = null;

        private static Transform GetSelectedTabContent(WindowTabs tabs)
        {
            if (getSelectedTabContentFunc == null)
            {
                MethodInfo minf = typeof(WindowTabs).GetMethod("get_SelectedTabContent", BindingFlags.NonPublic | BindingFlags.Instance);
                getSelectedTabContentFunc = (Func<WindowTabs, Transform>)Delegate.CreateDelegate(typeof(Func<WindowTabs, Transform>), minf);
            }
            return getSelectedTabContentFunc(tabs);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WindowTabs), "get_SelectedTabContent")]
        private static void WindowTabs_get_SelectedTabContent_pof(Transform __result)
        {
            _lastSelectedTabContent = __result;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WindowTabs), "RaiseTabSelectEvent")]
        private static void WindowTabs_RaiseTabSelectEvent_pof(WindowTabs __instance)
        {
            using (PooledList<IWindowTabSelectHandler> pooledList = PooledList<IWindowTabSelectHandler>.Take())
            {
                GetSelectedTabContent(__instance).GetComponentsInChildren<IWindowTabSelectHandler>(true, pooledList);
                foreach (IWindowTabSelectHandler windowTabSelectHandler in pooledList)
                {
                    windowTabSelectHandler.OnSelect();
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WindowTabs), "RaiseTabDeselectEvent")]
        private static void WindowTabs_RaiseTabDeselectEvent_pof(WindowTabs __instance)
        {
            using (PooledList<IWindowTabDeselectHandler> pooledList = PooledList<IWindowTabDeselectHandler>.Take())
            {
                GetSelectedTabContent(__instance).GetComponentsInChildren<IWindowTabDeselectHandler>(true, pooledList);
                foreach (IWindowTabDeselectHandler windowTabSelectHandler in pooledList)
                {
                    windowTabSelectHandler.OnDeselect();
                }
            }
        }

    }
}
