using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI.VehicleUnitPickerWindowViews;

namespace ScheduleStopwatch.UI
{
    class CargoCapacityIndicator : MonoBehaviour
    {
        private readonly List<CargoCapacityIndicatorItem> indicatorItems = new List<CargoCapacityIndicatorItem>();
        private static CargoCapacityIndicatorItem _itemTemplate;

        public void Initialize(IReadOnlyDictionary<Item, int> items, float? multiplier, IReadOnlyDictionary<Item, int> routeTotal = null)
        {
            foreach(CargoCapacityIndicatorItem indItem in indicatorItems)
            {
                indItem.DestroyGameObject();
            }
            indicatorItems.Clear();
            UpdateItems(items, multiplier, routeTotal);
        }

        public void UpdateItems(IReadOnlyDictionary<Item, int> items, float? multiplier, IReadOnlyDictionary<Item, int> routeTotal = null)
        {
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
                        CargoCapacityIndicatorItem indicatorItem = GetIndicatorItemInstance(base.transform);
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

        private CargoCapacityIndicatorItem GetIndicatorItemInstance(Transform parent)
        {
            if (_itemTemplate == null)
            {
                CreateItemTemplate();
            }
            return UnityEngine.Object.Instantiate<CargoCapacityIndicatorItem>(_itemTemplate, parent);
        }

        private void CreateItemTemplate()
        {
            DepotWindowVehicleListItemStoragesViewTooltipItem item = UnityEngine.Object.Instantiate<DepotWindowVehicleListItemStoragesViewTooltipItem>(R.Game.UI.DepotWindow.DepotWindowVehicleListItemStoragesViewTooltipItem);
            Transform itemTransform = item.transform;
            UnityEngine.Object.DestroyImmediate(item);

            _itemTemplate = itemTransform.gameObject.AddComponent<CargoCapacityIndicatorItem>();
            LayoutElement layout = itemTransform.GetComponent<LayoutElement>();
            layout.flexibleWidth = 0.00f;
        }
    }
}
