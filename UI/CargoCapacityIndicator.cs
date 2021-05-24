using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI.VehicleUnitPickerWindowViews;
using VoxelTycoon.UI;

namespace ScheduleStopwatch.UI
{
    class CargoCapacityIndicator : MonoBehaviour
    {
        private readonly List<CargoCapacityIndicatorItem> indicatorItems = new List<CargoCapacityIndicatorItem>();

        private static CargoCapacityIndicator _template;

        public void Initialize(IReadOnlyDictionary<Item, int> items, float? multiplier, IReadOnlyDictionary<Item, int> routeTotal = null)
        {
//            _contentGroup = UnityEngine.GameObject.Instantiate<Transform>((new GameObject("CargoCapacity", typeof(RectTransform))).transform, transform);
//            LayoutHelper.MakeLayoutGroup(_contentGroup, LayoutHelper.Orientation.Horizontal, new RectOffset(0, 0, 0, 0), 0f, 0, LayoutHelper.ChildSizing.ChildControlsSize);
            foreach (CargoCapacityIndicatorItem indItem in indicatorItems)
            {
                indItem.DestroyGameObject();
            }
            indicatorItems.Clear();
            UpdateItems(items, multiplier, routeTotal);
        }

        public void UpdateItems(IReadOnlyDictionary<Item, int> items, float? multiplier, IReadOnlyDictionary<Item, int> routeTotal = null)
        {
            FileLog.Log("CapacityIndicatorUpdateData");
            FileLog.Log(XMNUtils.GameObjectDumper.DumpGameObject(transform.parent.gameObject));
            int origCount = indicatorItems.Count;
            int index = 0;
            if (items != null && multiplier != null)
            {
                foreach (KeyValuePair<Item, int> itemData in items)
                {
                    float? routeTotalCount = null;
                    if (routeTotal != null && routeTotal.TryGetValue(itemData.Key, out int i))
                    {
                        routeTotalCount = i;
                    }
                    if (index < origCount)
                    {
                        indicatorItems[index].UpdateItemData(itemData.Key, itemData.Value * multiplier.Value, routeTotalCount);
                        indicatorItems[index].SetActive(true);
                    }
                    else
                    {
                        CargoCapacityIndicatorItem indicatorItem = CargoCapacityIndicatorItem.GetInstance(transform);
                        indicatorItems.Add(indicatorItem);
                        indicatorItem.Initialize(itemData.Key, itemData.Value * multiplier.Value, routeTotalCount);
                        indicatorItem.gameObject.SetActive(true);
                    }
                    index++;
                }
            }
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
            return tr.gameObject.AddComponent<CargoCapacityIndicator>();
        }
    }
}
