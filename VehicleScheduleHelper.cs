using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;
using VoxelTycoon.UI;
using XMNUtils;

namespace ScheduleStopwatch
{
    [HarmonyPatch]
    class VehicleScheduleHelper
    {
        static public event Action<Vehicle, VehicleStation, RootTask> DestinationReached;
        static public event Action<Vehicle, VehicleStation, RootTask> StationLeaved;
        static public event Action<Vehicle> MeasurementInvalidated;
        static public event Action<Vehicle, RootTask, bool> ScheduleChanged;

        static private Func<RootTask, int> getRootTaskVersionFunc;

        /** iterates through tasks that do not have nonstop behaviour nonstop */
        static public IEnumerable<RootTask> GetNonNonstopTasks(Vehicle vehicle)
        {
            ImmutableList<RootTask> tasks = vehicle.Schedule.GetTasks();
            int count = tasks.Count;
            for (int i = 0; i < count; i++)
            {
                RootTask task = tasks[i];
                if (task.Behavior != RootTaskBehavior.NonStop)
                {
                    yield return task;
                }
            }
            yield break;
        }

        static private void OnMeasurementInvalidated(Vehicle vehicle)
        {
            NotificationUtils.ShowVehicleHint(vehicle, "OnMeasurementInvalidated");
            MeasurementInvalidated?.Invoke(vehicle);
        }

        static private void OnStationLeaved(Vehicle vehicle, VehicleStation vehicleStation, RootTask rootTask)
        {
//            NotificationUtils.ShowVehicleHint(vehicle, "OnStationLeaved " + vehicleStation.Location.Name);
            StationLeaved?.Invoke(vehicle, vehicleStation, rootTask);
        }

        public static int GetRootTaskVersion(RootTask rootTask)
        {
            if (getRootTaskVersionFunc == null)
            {
                MethodInfo minf = typeof(RootTask).GetMethod("GetVersion", BindingFlags.NonPublic | BindingFlags.Instance);
                getRootTaskVersionFunc = (Func<RootTask, int>)Delegate.CreateDelegate(typeof(Func<RootTask, int>), minf);
            }
            return getRootTaskVersionFunc(rootTask);
        }

        static private void OnDestinationReached(Vehicle vehicle, VehicleStation vehicleStation, RootTask task)
        {
 //           NotificationUtils.ShowVehicleHint(vehicle, "OnDestinationReached " + vehicleStation.Location.Name);
            DestinationReached?.Invoke(vehicle, vehicleStation, task);
        }

        static private void OnScheduleChanged(Vehicle vehicle, RootTask task, bool minorChange=false, bool notifyRoute=false)
        {
            if (notifyRoute && vehicle.Route != null)
            {
                ImmutableList<Vehicle> vehicles = vehicle.Route.Vehicles;
                for (int i = 0; i < vehicles.Count; i++)
                {
                    ScheduleChanged?.Invoke(vehicles[i], task, minorChange);
                    NotificationUtils.ShowVehicleHint(vehicles[i], "OnScheduleChanged" + (minorChange ? " minor" : ""));
                }            
            }
            else
            {
                ScheduleChanged?.Invoke(vehicle, task, minorChange);
                NotificationUtils.ShowVehicleHint(vehicle, "OnScheduleChanged" + (minorChange ? " minor" : ""));
            }
        }

        static private void OnSubtaskChanged(SubTask subTask, bool minorChange = true, bool notifyRoute = false)
        {
            OnScheduleChanged(subTask.Vehicle, subTask.ParentTask, minorChange, notifyRoute);
        }

        #region Harmony
        #region RootTask
        //VehicleSchedule.CurrentTask is still set to the finished task
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RootTask), "OnStop")]
        static private void RootTask_OnStop_pof(RootTask __instance)
        {
            Vehicle vehicle = __instance.Vehicle;
            VehicleStation vehicleStation = __instance.Destination?.VehicleStationLocation?.VehicleStation;
            if (vehicle) { 
                if (vehicleStation && __instance.IsCompleted)
                {
                    OnStationLeaved(vehicle, vehicleStation, __instance);
                } else
                {
                    OnMeasurementInvalidated(vehicle);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RootTask), "SkipSubTask")]
        static private void RootTask_SkipSubTask_pof(RootTask __instance)
        {
            Vehicle vehicle = __instance.Vehicle;
            if (vehicle)
            {
                OnMeasurementInvalidated(vehicle);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RootTask), "SetCurrentTask")]
        static private void RootTask_SetCurrentTask_pof(RootTask __instance, int ____subTaskIndex)
        {
            Vehicle vehicle = __instance.Vehicle;
            if (vehicle && ____subTaskIndex == 0)
            {
                VehicleStation vehicleStation = __instance.Destination?.VehicleStationLocation?.VehicleStation;
                if (vehicleStation)
                {
                    OnDestinationReached(vehicle, vehicleStation, __instance);
                } else
                {
                    OnMeasurementInvalidated(vehicle);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RootTask), "AddSubTask")]
        static private void RootTask_AddSubTask_pof(RootTask __instance)
        {
            Vehicle vehicle = __instance.Vehicle;
            if (vehicle)
            {
                OnScheduleChanged(vehicle, __instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RootTask), "RemoveSubTask")]
        static private void RootTask_RemoveSubTask_pof(RootTask __instance)
        {
            Vehicle vehicle = __instance.Vehicle;
            if (vehicle)
            {
                OnScheduleChanged(vehicle, __instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RootTask), "MoveSubTask")]
        static private void RootTask_MoveSubTask_pof(RootTask __instance)
        {
            Vehicle vehicle = __instance.Vehicle;
            if (vehicle)
            {
                OnScheduleChanged(vehicle, __instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RootTask), "SetDestinationAndNotifyRoute")]
        static private void RootTask_SetDestinationAndNotifyRoute_prf(RootTask __instance, VehicleDestination destination, out VehicleDestination __state)
        {
            __state = __instance.Destination;
        }

        /**
         * __state = old destination
         * destination = new destination
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RootTask), "SetDestinationAndNotifyRoute")]
        static private void RootTask_SetDestinationAndNotifyRoute_pof(RootTask __instance, VehicleDestination destination, VehicleDestination __state)
        {
            Vehicle vehicle = __instance.Vehicle;
            if (vehicle)
            {
                bool minor = (__state != null && __state.VehicleStationLocation.VehicleStation == destination.VehicleStationLocation.VehicleStation);
                OnScheduleChanged(vehicle, __instance, minor, true);
            }
        }

        #endregion
        #region VehicleSchedule
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleSchedule), "RemoveTask")]
        static private void VehicleSchedule_RemoveTask_pof(VehicleSchedule __instance, RootTask task)
        {
            Vehicle vehicle = __instance.Vehicle;
            if (vehicle)
            {
                OnScheduleChanged(vehicle, task);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleSchedule), "AddTask")]
        static private void VehicleSchedule_AddTask_pof(VehicleSchedule __instance, RootTask task)
        {
            Vehicle vehicle = __instance.Vehicle;
            if (vehicle)
            {
                OnScheduleChanged(vehicle, task);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleSchedule), "MoveTask")]
        static private void VehicleSchedule_MoveTask_pof(VehicleSchedule __instance, int newIndex, List<RootTask> ____tasks)
        {
            Vehicle vehicle = __instance.Vehicle;
            if (vehicle)
            {
                OnScheduleChanged(vehicle, ____tasks[newIndex]);
            }
        }
        #endregion

        /** will be called when new root task behaviour is picked */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindowScheduleTabBehaviorPropertyPicker), "WrapCallback")]
        static private void VehicleWindowScheduleTabBehaviorPropertyPicker_WrapCallback_pof(RootTask ____task)
        {
            Vehicle vehicle = ____task?.Vehicle;
            if (vehicle)
            {
                OnScheduleChanged(vehicle, ____task, notifyRoute: true);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitsTask), "SetTargetUnitIndexes")]
        static private void UnitsTask_SetTargetUnitIndexes_pof(UnitsTask __instance)
        {
            Vehicle vehicle = __instance?.Vehicle;
            if (vehicle)
            {
                OnSubtaskChanged(__instance);
            }
        }

        #region VehicleWindowScheduleTabRefitPropertyView

        private static RefitTask _itemChangeRefitTask;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(VehicleWindowScheduleTabRefitPropertyView), "Initialize")]
        static private void VehicleWindowScheduleTabRefitPropertyView_Initialize_pof(VehicleWindowScheduleTabRefitPropertyView __instance , RefitTask task, bool editMode, ref GridPickerEx ____gridPicker)
        {
            _itemChangeRefitTask = null;
            if (editMode && task != null)
            {
                Button component = __instance.transform.GetComponent<Button>();
                component.onClick.AddListener(delegate ()
                {
                    _itemChangeRefitTask = task;
                });
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GridPickerHelper), "PickItemToRefitVehicleUnits")]
        static private void GridPickerHelper_PickItemToRefitVehicleUnits_prf(ref Action<Item> callback)
        {
            RefitTask task = _itemChangeRefitTask;
            if (task != null && task.Vehicle != null)
            {
                Item origItem = task.Item;
                callback += delegate (Item item)
                {
                    if (origItem != item)
                    {
                        OnSubtaskChanged(task, notifyRoute: true);
                    }
                };
            }
            _itemChangeRefitTask = null;
        }
        #endregion
        #endregion
    }
}
