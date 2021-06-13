using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI.VehicleUnitPickerWindowViews;
using VoxelTycoon.UI;
using static ScheduleStopwatch.VehicleScheduleCapacity;

namespace ScheduleStopwatch.UI
{
    class CargoCapacityIndicator : MonoBehaviour
    {
        public int ItemsCount { get; private set; }

        private readonly List<CargoCapacityIndicatorItem> indicatorItems = new List<CargoCapacityIndicatorItem>();
        private Text _overFlowText;
        private Transform _overflowCont;

        private static CargoCapacityIndicator _template;

        public void Initialize(IReadOnlyDictionary<Item, TransferData> items, float? multiplier, IReadOnlyDictionary<Item, TransferData> routeTotal = null, int itemsLimit = 0, TransferDirection transfDirection = TransferDirection.both)
        {
            foreach (CargoCapacityIndicatorItem indItem in indicatorItems)
            {
                indItem.DestroyGameObject();
            }
            ItemsCount = 0;
            _overflowCont = transform.Find("OverflowText");
            _overFlowText = _overflowCont.GetComponentInChildren<Text>();
            indicatorItems.Clear();
            UpdateItems(items, multiplier, routeTotal, itemsLimit: itemsLimit, transfDirection: transfDirection);
        }

        public void UpdateItems(IReadOnlyDictionary<Item, TransferData> items, float? multiplier, IReadOnlyDictionary<Item, TransferData> routeTotal = null, TransferDirection transfDirection = TransferDirection.both, int itemsLimit = 0)
        {
            int origCount = indicatorItems.Count;
            int index = 0;
            int overflowCount = 0;
            if (items != null && multiplier != null)
            {
                IReadOnlyDictionary<Item, TransferData> itemsToDisplay = routeTotal ?? items;
                foreach (KeyValuePair<Item, TransferData> itemData in itemsToDisplay)
                {
                    if ((transfDirection == TransferDirection.loading && itemData.Value.load == 0) || (transfDirection == TransferDirection.unloading && itemData.Value.unload == 0)) {
                        continue;
                    }

                    float? routeTotalCount = null;
                    float itemsCountValue = 0;
                    if (routeTotal != null)
                    {
                        routeTotalCount = itemData.Value.Get(transfDirection);
                        if (items.TryGetValue(itemData.Key, out TransferData i))
                        {
                            itemsCountValue = i.Get(transfDirection);
                        } 
                    } else
                    {
                        itemsCountValue = Math.Abs(itemData.Value.Get(transfDirection));
                    }

                    if (itemsCountValue == 0 && (routeTotalCount == null || routeTotalCount.Value == 0))
                    {
                        continue; //do not display zero values
                    }

                    if (itemsLimit > 0 && index >= itemsLimit)
                    {
                        overflowCount++;
                        continue;
                    }

                    if (index < origCount)
                    {
                        indicatorItems[index].UpdateItemData(itemData.Key, itemsCountValue * multiplier.Value, routeTotalCount);
                        indicatorItems[index].SetActive(true);
                    }
                    else
                    {
                        CargoCapacityIndicatorItem indicatorItem = CargoCapacityIndicatorItem.GetInstance(transform);
                        indicatorItems.Add(indicatorItem);
                        indicatorItem.Initialize(itemData.Key, itemsCountValue * multiplier.Value, routeTotalCount);
                        indicatorItem.gameObject.SetActive(true);
                    }
                    index++;
                }
            }
            ItemsCount = index;
            if (overflowCount > 0)
            {
                indicatorItems[index-1].UpdateAsOverflowCount(overflowCount+1);
                indicatorItems[index-1].SetActive(true);
            }
            else
            {
                _overflowCont.SetActive(false);
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
            Transform tr = UnityEngine.GameObject.Instantiate<Transform>(new GameObject("CargoCapacity", typeof(RectTransform)).transform);
            tr.gameObject.SetActive(false);
            LayoutHelper.MakeLayoutGroup(tr, LayoutHelper.Orientation.Horizontal, new RectOffset(0, 0, 0, 0), 0f, 0, LayoutHelper.ChildSizing.ChildControlsSize);
            tr.gameObject.AddComponent<CanvasRenderer>();
            tr.gameObject.AddComponent<NonDrawingGraphic>();

            //overflow indicator
            Text overflowText = UnityEngine.Object.Instantiate<Text>(R.Game.UI.DepotWindow.DepotWindowVehicleListItemStoragesViewTooltipItem.GetComponentInChildren<Text>(), tr);
            overflowText.transform.name = "OverflowText";
            overflowText.gameObject.SetActive(false);
            overflowText.color = Color.black;
            LayoutElement layout = overflowText.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = 0;
            layout.minWidth = 20;

//            FileLog.Log(XMNUtils.GameObjectDumper.DumpGameObject(R.Game.UI.DepotWindow.DepotWindowVehicleListItemStoragesViewTooltipItem.gameObject));
            return tr.gameObject.AddComponent<CargoCapacityIndicator>();
        }
    }
}
