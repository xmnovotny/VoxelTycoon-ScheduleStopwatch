using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI.VehicleUnitPickerWindowViews;
using VoxelTycoon.UI;
using VoxelTycoon.UI.Controls;

namespace ScheduleStopwatch.UI
{
    class CargoCapacityIndicator : MonoBehaviour
    {
        public enum IndicatorIcon { noIcon, loading, unloading };
        public enum TransferDirection { both, loading, unloading };
        public int ItemsCount { get; private set; }

        private readonly List<CargoCapacityIndicatorItem> indicatorItems = new List<CargoCapacityIndicatorItem>();

        private static CargoCapacityIndicator _template;

        public void Initialize(IReadOnlyDictionary<Item, int> items, float? multiplier, IReadOnlyDictionary<Item, int> routeTotal = null, IndicatorIcon icon = IndicatorIcon.noIcon)
        {
            foreach (CargoCapacityIndicatorItem indItem in indicatorItems)
            {
                indItem.DestroyGameObject();
            }
            ItemsCount = 0;
            indicatorItems.Clear();
            UpdateItems(items, multiplier, routeTotal);
        }

        public void UpdateItems(IReadOnlyDictionary<Item, int> items, float? multiplier, IReadOnlyDictionary<Item, int> routeTotal = null, TransferDirection transfDirection = TransferDirection.both)
        {
            int origCount = indicatorItems.Count;
            int index = 0;
            if (items != null && multiplier != null)
            {
                foreach (KeyValuePair<Item, int> itemData in items)
                {
                    if ((transfDirection == TransferDirection.loading && itemData.Value<0) || (transfDirection == TransferDirection.unloading && itemData.Value>0)) {
                        continue;
                    }

                    float? routeTotalCount = null;
                    if (routeTotal != null && routeTotal.TryGetValue(itemData.Key, out int i))
                    {
                        routeTotalCount = Math.Abs(i);
                    }
                    if (index < origCount)
                    {
                        indicatorItems[index].UpdateItemData(itemData.Key, Math.Abs(itemData.Value) * multiplier.Value, routeTotalCount);
                        indicatorItems[index].SetActive(true);
                    }
                    else
                    {
                        CargoCapacityIndicatorItem indicatorItem = CargoCapacityIndicatorItem.GetInstance(transform);
                        indicatorItems.Add(indicatorItem);
                        indicatorItem.Initialize(itemData.Key, Math.Abs(itemData.Value) * multiplier.Value, routeTotalCount);
                        indicatorItem.gameObject.SetActive(true);
                    }
                    index++;
                }
            }
            ItemsCount = index;
            for (; index < origCount; index++)
            {
                indicatorItems[index].gameObject.SetActive(false);
            }
        }

        public static CargoCapacityIndicator GetInstance(Transform parent)
        {
            if (_template == null)
            {
                _template = CreateTemplate();
            }
            CargoCapacityIndicator result = UnityEngine.GameObject.Instantiate<CargoCapacityIndicator>(_template, parent);
            result.gameObject.SetActive(true);
            result.name = "CargoCapacity";
            return result;
        }

        private static CargoCapacityIndicator CreateTemplate()
        {
            Transform tr = UnityEngine.GameObject.Instantiate<Transform>((new GameObject("CargoCapacity", typeof(RectTransform))).transform);
            tr.gameObject.SetActive(false);
            LayoutHelper.MakeLayoutGroup(tr, LayoutHelper.Orientation.Horizontal, new RectOffset(0, 0, 0, 0), 0f, 0, LayoutHelper.ChildSizing.ChildControlsSize);
            tr.gameObject.AddComponent<CanvasRenderer>();
            tr.gameObject.AddComponent<NonDrawingGraphic>();
            
            return tr.gameObject.AddComponent<CargoCapacityIndicator>();
        }
    }
}
