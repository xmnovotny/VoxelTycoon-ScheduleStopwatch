using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Buildings;
using VoxelTycoon.Localization;
using VoxelTycoon.Recipes;
using VoxelTycoon.Tracks;
using VoxelTycoon.UI;
using static ScheduleStopwatch.VehicleScheduleCapacity;

namespace ScheduleStopwatch.UI
{

    /// <summary>
    /// Defines the <see cref="StationWindowLogisticHelper" />.
    /// </summary>
    public class StationWindowLogisticHelper : LazyManager<StationWindowLogisticHelper>
    {
        /// <summary>
        /// The FillContainerWithItems.
        /// </summary>
        /// <param name="container">The container<see cref="Transform"/>Container with label and items container</param>
        /// <param name="itemContainer">The itemContainer<see cref="Transform"/>Container with items</param>
        /// <param name="transfers">The transfers<see cref="IReadOnlyDictionary{Item, TransferData}"/>.</param>
        /// <param name="direction">The direction<see cref="TransferDirection"/>.</param>
        /// <param name="neededItems">The neededItems<see cref="Dictionary{Item, float}"/>.</param>
        /// <param name="itemTooltipTextFunc">The itemTooltipTextFunc<see cref="Func{Item, int, string}"/>Function to get tooltip text (item, count)(</param>
        internal void FillContainerWithItems(Transform container, Transform itemContainer, IReadOnlyDictionary<Item, TransferData> transfers, TransferDirection direction, Dictionary<Item, float> neededItems = null, Func<Item, int, string> itemTooltipTextFunc = null)
        {
            int count = 0;
            if (transfers == null)
            {
                container.gameObject.SetActive(false);
                return;
            }
            List<ResourceView> resourceViews = new List<ResourceView>();
            itemContainer.transform.GetComponentsInChildren<ResourceView>(resourceViews);
            foreach (KeyValuePair<Item, TransferData> pair in transfers)
            {
                int value = pair.Value.Get(direction);
                if (value > 0)
                {
                    float? neededCount = null;
                    if (neededItems != null && neededItems.TryGetValue(pair.Key, out float neededAmount))
                    {
                        neededCount = neededAmount;
                    }
                    AddOneItemToContainer(resourceViews, count, itemContainer, pair.Key, value, neededCount, itemTooltipTextFunc);
                    count++;
                }
            }
            container.gameObject.SetActive(count > 0);
            DeactivateResourceViews(resourceViews, count);
        }

        internal void FillContainerWithNeededItems(Transform container, Transform itemContainer, Dictionary<Item, float> items, IReadOnlyDictionary<Item, TransferData> transfers = null, Func<Item, int, string> itemTooltipTextFunc = null)
        {
            int count = 0;
            if (items == null)
            {
                container.gameObject.SetActive(false);
                return;
            }
            List<ResourceView> resourceViews = new List<ResourceView>();
            itemContainer.transform.GetComponentsInChildren<ResourceView>(resourceViews);
            foreach (KeyValuePair<Item, float> pair in items)
            {
                int value = pair.Value.RoundToInt();
                if (value > 0)
                {
                    float? transferred = null;
                    if (transfers != null && transfers.TryGetValue(pair.Key, out TransferData transfData))
                    {
                        transferred = transfData.unload > 0 ? transfData.unload : null;
                    }
                    AddOneItemToContainer(resourceViews, count, itemContainer, pair.Key, value, transferred, itemTooltipTextFunc);
                    count++;
                }
            }
            container.gameObject.SetActive(count > 0);
            DeactivateResourceViews(resourceViews, count);
        }
        internal void FillContainerWithNeededItems(Transform container, Transform itemContainer, Dictionary<Item, int> items, IReadOnlyDictionary<Item, TransferData> transfers = null, Func<Item, int, string> itemTooltipTextFunc = null)
        {
            int count = 0;
            if (items == null)
            {
                container.gameObject.SetActive(false);
                return;
            }
            List<ResourceView> resourceViews = new List<ResourceView>();
            itemContainer.transform.GetComponentsInChildren<ResourceView>(resourceViews);
            foreach (KeyValuePair<Item, int> pair in items)
            {
                int value = pair.Value;
                if (value > 0)
                {
                    float? transferred = null;
                    if (transfers != null && transfers.TryGetValue(pair.Key, out TransferData transfData))
                    {
                        transferred = transfData.unload > 0 ? transfData.unload : null;
                    }
                    AddOneItemToContainer(resourceViews, count, itemContainer, pair.Key, value, transferred, itemTooltipTextFunc);
                    count++;
                }
            }
            container.gameObject.SetActive(count > 0);
            DeactivateResourceViews(resourceViews, count);
        }

        internal void DeactivateResourceViews(List<ResourceView> resourceViews, int count)
        {
            while (count < resourceViews.Count)
            {
                resourceViews[count].gameObject.SetActive(false);
                count++;
            }
        }

        internal void AddOneItemToContainer(List<ResourceView> resourceViews, int viewIndex, Transform itemContainer, Item item, int itemAmount, float? neededCount, Func<Item, int, string> itemTooltipTextFunc)
        {
            ResourceView view = this.GetResourceView(resourceViews, viewIndex, itemContainer);
            Panel valueCont = view.gameObject.transform.Find("ValueContainer").GetComponent<Panel>();
            Transform demandCont = view.transform.Find("DemandContainer");
            if (neededCount != null)
            {
                view.Show(null, null, LazyManager<IconRenderer>.Current.GetItemIcon(item.AssetId), StringHelper.Simplify((double)itemAmount), StringHelper.FormatCountString(item.DisplayName, itemAmount.ToString("N0") + "/" + neededCount.Value.ToString("N0")));

                demandCont.Find<Text>("Value").text = StringHelper.Simplify(neededCount.Value);
                demandCont.gameObject.SetActive(true);
                float ratio = itemAmount / neededCount.Value;
                if (ratio > 1.05f)
                {
                    valueCont.color = Color.blue;
                }
                else
                if (ratio < 0.9f)
                {
                    valueCont.color = Color.red;
                }
                else
                {
                    valueCont.color = new Color(0, 0.88f, 0);
                }
            }
            else
            {
                view.ShowItem(item, null, itemAmount);
                valueCont.color = _resourceViewOrigColor;
                demandCont.gameObject.SetActive(false);
            }
            if (itemTooltipTextFunc != null)
            {
                view.GetComponent<TooltipTarget>().DynamicText = delegate { return itemTooltipTextFunc.Invoke(item, itemAmount); };
            }
        }

        /// <summary>
        /// The CreateItemsContainer.
        /// </summary>
        /// <param name="parent">The parent<see cref="Transform"/>.</param>
        /// <param name="title">The title<see cref="string"/>.</param>
        /// <param name="name">The name<see cref="string"/>.</param>
        /// <returns>The <see cref="Transform"/>.</returns>
        public Transform CreateItemsContainer(Transform parent, string title, string name = null)
        {
            if (!_itemsContainerTemplate)
            {
                CreateItemsContainerTemplate();
            }
            Transform result = GameObject.Instantiate<Transform>(_itemsContainerTemplate, parent);
            if (name != null)
            {
                result.name = name;
            }
            result.Find("Label").gameObject.GetComponent<Text>().text = title;
            return result;
        }

        /// <summary>
        /// The GetEstimatatedNeededItems.
        /// </summary>
        /// <param name="station">The station<see cref="VehicleStation"/>.</param>
        /// <param name="transfers">The transfers<see cref="IReadOnlyDictionary{Item, TransferData}"/>.</param>
        /// <param name="neededItemsPerItem">The neededItemsPerItem<see cref="Dictionary{Item, Dictionary{Item, float}}"/>.</param>
        /// <param name="demands">The demands<see cref="Dictionary{Item, int}"/>.</param>
        /// <returns>The <see cref="Dictionary{Item, float}"/>.</returns>
        public Dictionary<Item, float> GetEstimatatedNeededItems(VehicleStation station, IReadOnlyDictionary<Item, TransferData> transfers, out Dictionary<Item, Dictionary<Item, float>> neededItemsPerItem, out Dictionary<Item, int> demands, Dictionary<Recipe, float> recipesNeeded = null)
        {
            Dictionary<Item, float> neededItems = new Dictionary<Item, float>();
            neededItemsPerItem = new Dictionary<Item, Dictionary<Item, float>>();
            demands = null;
            if (transfers.Count == 0)
            {
                return neededItems;
            }

            List<Item> finalItems = new List<Item>();
            foreach (KeyValuePair<Item, TransferData> pair in transfers)
            {
                if (pair.Value.unload > 0)
                {
                    finalItems.Add(pair.Key);
                }
            }
            RecipeHelper helper = LazyManager<RecipeHelper>.Current;

            Settings settings = Settings.Current;
            Dictionary<Item, int> itemsForCalculation = new Dictionary<Item, int>();
            Dictionary<Item, int> unservicedDemands = settings.CalculateUnservicedDemands ? null : new Dictionary<Item, int>();

            RecipeHelper.AddItems(itemsForCalculation, transfers, TransferDirection.loading);

            if (station != null)
            {
                demands = new Dictionary<Item, int>();
                LazyManager<StationDemandManager>.Current.GetCombinedStationsDemands(station, demands, unservicedDemands);
                if (demands.Count > 0)
                {
                    RecipeHelper.AddItems(itemsForCalculation, demands);
                }
            }

            foreach (KeyValuePair<Item, int> pair in itemsForCalculation)
            {
                Dictionary<Item, float> subNeededItems = neededItemsPerItem[pair.Key] = new Dictionary<Item, float>();
                List<RecipeItem> ingredients = null;
                bool isUnserviced = unservicedDemands != null && unservicedDemands.TryGetValue(pair.Key, out int unservicedCount) && unservicedCount == pair.Value;

                if (!isUnserviced && (!transfers.TryGetValue(pair.Key, out TransferData transfData) || transfData.unload == 0))
                {
                    //calculate ingredients only for items that are not unloaded at the station
                    ingredients = helper.GetIngredients(pair.Key, finalItems, pair.Value, recipesNeeded);
                }
                if (ingredients != null && ingredients.Count > 0)
                {
                    AddIngredients(ingredients, neededItems);
                    AddIngredients(ingredients, subNeededItems);
                }
                else
                {
                    //no ingredients = raw item / item is transferred (unloaded and loaded) / unserviced demand, we add it to needed items
                    if (!neededItems.TryGetValue(pair.Key, out float count))
                    {
                        count = 0;
                    }
                    neededItems[pair.Key] = count + pair.Value;
                    subNeededItems[pair.Key] = pair.Value;
                }

            }
            return neededItems;
        }

        /// <summary>
        /// The GetEstimatatedNeededItemsTooltipText.
        /// </summary>
        /// <param name="neededItems">The neededItems<see cref="Dictionary{Item, float}"/>.</param>
        /// <param name="transfers">The transfers<see cref="IReadOnlyDictionary{Item, TransferData}"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public string GetEstimatatedNeededItemsTooltipText(Dictionary<Item, float> neededItems, IReadOnlyDictionary<Item, TransferData> transfers)
        {
            RecipeHelper helper = LazyManager<RecipeHelper>.Current;
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            if (neededItems.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(StringHelper.Boldify(LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/needed_items_to_produce").ToUpper()));

                foreach (KeyValuePair<Item, float> pair in neededItems)
                {
                    if (pair.Value > 0)
                    {
                        string text = pair.Value.ToString("N0");
                        sb.AppendLine().Append(StringHelper.FormatCountString(pair.Key.DisplayName, text));
                        if (!transfers.TryGetValue(pair.Key, out TransferData transfCount) || transfCount.load > 0)
                        {
                            (int? itemCountPerMonth, Mine mine) = helper.GetMinedItemsPerMineAndMonth(pair.Key);
                            if (itemCountPerMonth != null)
                            {
                                sb.Append(" (" + locale.GetString("schedule_stopwatch/number_of_mines").Format((pair.Value / itemCountPerMonth.Value).ToString("N1"), mine.DisplayName) + ")");
                            }
                        }
                    }
                }
                return sb.ToString();
            }

            return "";
        }

        public (IReadOnlyDictionary<Item, TransferData> tranfers, bool incompleteTransfers, bool estimatedTransfers) GetTransfers(VehicleStation station)
        {
            TaskTransfers transfersSum = new TaskTransfers();
            bool incompleteTransfers = false;
            bool estimatedTransfers = false;

            foreach (VehicleStation connStation in LazyManager<StationDemandManager>.Current.GetConnectedStationsEnum(station, true))
            {
                ImmutableList<Vehicle> vehicles = LazyManager<VehicleStationLocationManager>.Current.GetServicedVehicles(connStation.Location);
                Manager<VehicleScheduleDataManager>.Current.GetStationTaskTransfersSum(vehicles, connStation.Location, out bool incomplTransf, out bool isEstimated, transfersSum);
                incompleteTransfers |= incomplTransf;
                estimatedTransfers |= isEstimated;
            }
            return (transfersSum.Transfers, incompleteTransfers, estimatedTransfers);
        }


        /// <summary>
        /// The AddIngredients.
        /// </summary>
        /// <param name="itemsToAdd">The itemsToAdd<see cref="List{RecipeItem}"/>.</param>
        /// <param name="totalItems">The totalItems<see cref="Dictionary{Item, float}"/>.</param>
        private void AddIngredients(List<RecipeItem> itemsToAdd, Dictionary<Item, float> totalItems)
        {
            foreach (RecipeItem itemToAdd in itemsToAdd)
            {
                if (!totalItems.TryGetValue(itemToAdd.Item, out float count))
                {
                    count = 0;
                }
                totalItems[itemToAdd.Item] = count + itemToAdd.Count;
            }
        }

        /// <summary>
        /// The CreateItemsContainerTemplate.
        /// </summary>
        private void CreateItemsContainerTemplate()
        {
            _itemsContainerTemplate = UnityEngine.Object.Instantiate<Transform>(R.Game.UI.StationWindow.StationWindowOverviewTab.transform.Find("Body/WindowScrollView").GetComponent<ScrollRect>().content.Find("Sources"));
            _itemsContainerTemplate.gameObject.SetActive(false);
            GridLayoutGroup group = _itemsContainerTemplate.Find<GridLayoutGroup>("Content");

            RectOffset padding = group.padding;
            padding.top = 5;
            padding.bottom = 0;
            group.padding = padding;

            Vector2 spacing = group.spacing;
            spacing.y = 26;
            group.spacing = spacing;
        }

        /// <summary>
        /// The GetResourceView.
        /// </summary>
        /// <param name="resourceViews">The resourceViews<see cref="List{ResourceView}"/>.</param>
        /// <param name="i">The i<see cref="int"/>.</param>
        /// <param name="parent">The parent<see cref="Transform"/>.</param>
        /// <returns>The <see cref="ResourceView"/>.</returns>
        private ResourceView GetResourceView(List<ResourceView> resourceViews, int i, Transform parent)
        {
            if (i < resourceViews.Count)
            {
                return resourceViews[i];
            }
            if (_resourceViewTemplate == null)
            {
                CreateResourceViewTemplate();
            }
            ResourceView result = UnityEngine.Object.Instantiate<ResourceView>(_resourceViewTemplate, parent);
            return result;
        }

        /// <summary>
        /// The CreateResourceViewTemplate.
        /// </summary>
        private void CreateResourceViewTemplate()
        {
            _resourceViewOrigColor = R.Game.UI.ResourceView.gameObject.transform.Find("ValueContainer").GetComponent<Panel>().color;
            _resourceViewTemplate = UnityEngine.Object.Instantiate<ResourceView>(R.Game.UI.ResourceView, null);
            RectTransform valueContainer = _resourceViewTemplate.transform.Find<RectTransform>("ValueContainer");
            RectTransform newContainer = UnityEngine.Object.Instantiate<RectTransform>(valueContainer, _resourceViewTemplate.transform);
            newContainer.SetAsFirstSibling();
            newContainer.name = "DemandContainer";
            newContainer.SetActive(false);
            newContainer.GetComponent<Panel>().color = Color.grey;
            Vector2 pos = newContainer.anchoredPosition;
            pos.y = 33;
            newContainer.anchoredPosition = pos;
        }

        private ResourceView _resourceViewTemplate;
        private Color _resourceViewOrigColor;
        private Transform _itemsContainerTemplate;
    }
}
