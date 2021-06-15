using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Buildings;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Localization;
using VoxelTycoon.Recipes;
using VoxelTycoon.Tracks;
using VoxelTycoon.UI;
using static ScheduleStopwatch.VehicleScheduleCapacity;

namespace ScheduleStopwatch.UI
{
    [Harmony]
    public class StationWindowOverviewTabExtender : MonoBehaviour
    {

        public StationWindowOverviewTab OverviewTab { get; private set; }
        public StationWindow StationWindow { get; private set; }

        public void Initialize(StationWindow window)
        {
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            StationWindow = window;
            OverviewTab = gameObject.GetComponent<StationWindowOverviewTab>();
            if (OverviewTab == null)
            {
                throw new NullReferenceException("Component StationWindowOverviewTab not found");
            }
            Transform content = base.transform.Find("Body/WindowScrollView").GetComponent<ScrollRect>().content;
            _unloadedContainer = CreateContainer(content, locale.GetString("schedule_stopwatch/monthly_unloaded_items").ToUpper());
            _unloadedContainer.SetSiblingIndex(2);
            _unloadedItemsContainer = _unloadedContainer.Find("Content");
            _unloadedContainerTitle = _unloadedContainer.Find("Label").gameObject.GetComponent<Text>();

            Tooltip.For(
                _unloadedContainerTitle.transform,
                () => GetEstimatatedNeededItemsTooltipText(),
                null
            );

            _loadedContainer = CreateContainer(content,locale.GetString("schedule_stopwatch/monthly_loaded_items").ToUpper());
            _loadedContainer.SetSiblingIndex(3);
            _loadedItemsContainer = _loadedContainer.Find("Content");
            _loadedContainerTitle = _loadedContainer.Find("Label").gameObject.GetComponent<Text>();

            Tooltip.For(
                _loadedContainerTitle.transform,
                () => GetEstimatatedNeededItemsTooltipText(),
                null
            );
            this._offset = Time.unscaledTime;
        }

        protected void Update()
        {
            if (!this.StationWindow.Location.IsDead)
            {
                if (TimeHelper.OncePerUnscaledTime(1f, this._offset))
                {
                    this.Invalidate();
                }
            }
        }

        private void OnSelect()
        {
            this._offset = Time.unscaledTime;
            Invalidate();
        }

        private Dictionary<Item, float> GetEstimatatedNeededItems()
        {
            if (_lastNeededItems == null)
            {
                Dictionary<Item, float> neededItems = _lastNeededItems = new Dictionary<Item, float>();
                IReadOnlyDictionary<Item, TransferData> lastTransfers = LastTransfers;
                _neededItemsPerItem = new Dictionary<Item, Dictionary<Item, float>>();
                if (lastTransfers.Count == 0)
                {
                    return neededItems;
                }

                List<Item> finalItems = new List<Item>();
                foreach (KeyValuePair<Item, TransferData> pair in lastTransfers)
                {
                    if (pair.Value.unload > 0)
                    {
                        finalItems.Add(pair.Key);
                    }
                }
                RecipeHelper helper = LazyManager<RecipeHelper>.Current;

                Dictionary<Item, int> itemsForCalculation = new Dictionary<Item, int>();
                Dictionary<Item, int> unservicedDemands = new Dictionary<Item, int>();

                RecipeHelper.AddItems(itemsForCalculation, lastTransfers, TransferDirection.loading);

                if (StationWindow.Location.VehicleStation != null)
                {
                    _lastDemands = new Dictionary<Item, int>();
                    DemandHelper.GetStationDemands(StationWindow.Location.VehicleStation, _lastDemands, unservicedDemands);
                    if (_lastDemands.Count>0)
                    {
                        RecipeHelper.AddItems(itemsForCalculation, _lastDemands);
                    }
                }

                foreach (KeyValuePair<Item, int> pair in itemsForCalculation)
                {
                    Dictionary<Item, float> subNeededItems = _neededItemsPerItem[pair.Key] = new Dictionary<Item, float>();
                    List<RecipeItem> ingredients = null;
                    bool isUnserviced = unservicedDemands.TryGetValue(pair.Key, out int unservicedCount) && unservicedCount == pair.Value;

                    if (!isUnserviced && (!lastTransfers.TryGetValue(pair.Key, out TransferData transfData) || transfData.unload == 0))
                    {
                        //calculate ingredients only for items that are not unloaded at the station
                        ingredients = helper.GetIngredients(pair.Key, finalItems, pair.Value);
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
            }
            return _lastNeededItems;
        }

        //needed items for specific item
        private Dictionary<Item, float> GetEstimatatedNeededItems(Item item)
        {
            if (_neededItemsPerItem == null)
            {
                GetEstimatatedNeededItems();
            }
            if (_neededItemsPerItem.TryGetValue(item, out Dictionary<Item, float> result)) {
                return result;
            }
            return null;
        }

        private string GetEstimatatedNeededItemsTooltipText()
        {
            Dictionary<Item, float> neededItems = GetEstimatatedNeededItems();
            RecipeHelper helper = LazyManager<RecipeHelper>.Current;
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            if (neededItems.Count>0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(StringHelper.Boldify(LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/needed_items_to_produce").ToUpper()));

                foreach (KeyValuePair<Item, float> pair in neededItems)
                {
                    if (pair.Value > 0)
                    {
                        string text = pair.Value.ToString("N0");
                        sb.AppendLine().Append(StringHelper.FormatCountString(pair.Key.DisplayName, text));
                        if (!LastTransfers.TryGetValue(pair.Key, out TransferData transfCount) || transfCount.load > 0)
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
        
        private string GetEstimatedItemsForOneLoadItemTooltipText(Item item, int displayAmount)
        {
            Dictionary<Item, float> neededItems = GetEstimatatedNeededItems(item);
            RecipeHelper helper = LazyManager<RecipeHelper>.Current;
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            StringBuilder sb = new StringBuilder(StringHelper.FormatCountString(item.DisplayName, displayAmount));
            if (neededItems != null && neededItems.Count > 0)
            {
                sb.AppendLine().AppendLine().Append(StringHelper.Boldify(LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/needed_items_to_produce_this").ToUpper()));

                foreach (KeyValuePair<Item, float> pair in neededItems)
                {
                    if (pair.Value > 0)
                    {
                        string text = pair.Value.ToString("N0");
                        sb.AppendLine().Append(StringHelper.FormatCountString(pair.Key.DisplayName, text));
                        if (!LastTransfers.TryGetValue(pair.Key, out TransferData transfCount) || transfCount.load > 0)
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

        private void AddIngredients(List<RecipeItem> itemsToAdd, Dictionary<Item, float> totalItems)
        {
            foreach(RecipeItem itemToAdd in itemsToAdd)
            {
                if (!totalItems.TryGetValue(itemToAdd.Item, out float count))
                {
                    count = 0;
                } 
                totalItems[itemToAdd.Item] = count + itemToAdd.Count;
            }
        }

        private void Invalidate()
        {
            Settings settings = Settings.Current;
            _lastTransfers = null;
            _lastNeededItems = null;
            _neededItemsPerItem = null;
            _lastDemands = null;
            if (settings.ShowStationLoadedItems || settings.ShowStationUnloadedItems)
            {
                ImmutableList<Vehicle> vehicles = LazyManager<VehicleStationLocationManager>.Current.GetServicedVehicles(StationWindow.Location);
                IReadOnlyDictionary<Item, TransferData> transfers = LastTransfers;
                FillContainerWithItems(_loadedContainer, _loadedItemsContainer, settings.ShowStationLoadedItems ? transfers : null, TransferDirection.loading, itemTooltipTextFunc: GetEstimatedItemsForOneLoadItemTooltipText);
                FillContainerWithItems(_unloadedContainer, _unloadedItemsContainer, settings.ShowStationUnloadedItems ? transfers : null, TransferDirection.unloading, GetEstimatatedNeededItems());
                bool incomplete = IncompleteTransfers;
                if (_lastIncomplete != incomplete)
                {
                    _lastIncomplete = incomplete;
                    Locale locale = LazyManager<LocaleManager>.Current.Locale;
                    _loadedContainerTitle.text = locale.GetString("schedule_stopwatch/monthly_loaded_items").ToUpper() + (incomplete ? " (" + locale.GetString("schedule_stopwatch/partial").ToUpper() + ")" : "");
                    _unloadedContainerTitle.text = locale.GetString("schedule_stopwatch/monthly_unloaded_items").ToUpper() + (incomplete ? " (" + locale.GetString("schedule_stopwatch/partial").ToUpper() + ")" : "");
                }
            } else
            {
                _loadedContainer.SetActive(false);
                _unloadedContainer.SetActive(false);
            }
        }

        private void FillContainerWithItems(Transform container, Transform itemContainer, IReadOnlyDictionary<Item, TransferData> transfers, TransferDirection direction, Dictionary<Item, float> neededItems = null, Func<Item, int, string> itemTooltipTextFunc = null) 
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
                    ResourceView view = this.GetResourceView(resourceViews, count, itemContainer);
                    Panel renderer = view.gameObject.transform.Find("ValueContainer").GetComponent<Panel>();
                    if (neededItems != null && neededItems.TryGetValue(pair.Key, out float neededCount))
                    {
                        view.Show(null, null, LazyManager<IconRenderer>.Current.GetItemIcon(pair.Key.AssetId), StringHelper.Simplify((double)value), StringHelper.FormatCountString(pair.Key.DisplayName, value.ToString("N0") + "/" + neededCount.ToString("N0")));
                        float ratio = value / neededCount;
                        if (ratio > 1.2f)
                        {
                            renderer.color = Color.blue;
                        }
                        else
                        if (ratio < 0.9f)
                        {
                            renderer.color = Color.red;
                        } else
                        {
                            renderer.color = new Color(0, 0.88f, 0);
                        }
                    }
                    else
                    {
                        view.ShowItem(pair.Key, null, value);
                        renderer.color = _resourceViewOrigColor;
                    }
                    if (itemTooltipTextFunc != null)
                    {
                        view.GetComponent<TooltipTarget>().DynamicText = delegate { return itemTooltipTextFunc.Invoke(pair.Key, value); };
                    }
                    count++;
                }
            }
            container.gameObject.SetActive(count > 0);
            while (count < resourceViews.Count)
            {
                resourceViews[count].gameObject.SetActive(false);
                count++;
            }
        }

        private ResourceView GetResourceView(List<ResourceView> resourceViews, int i, Transform parent)
        {
            if (i < resourceViews.Count)
            {
                return resourceViews[i];
            }
            return UnityEngine.Object.Instantiate<ResourceView>(R.Game.UI.ResourceView, parent);
        }

        private void CreateTemplate()
        {
            _template = UnityEngine.Object.Instantiate<Transform>(R.Game.UI.StationWindow.StationWindowOverviewTab.transform.Find("Body/WindowScrollView").GetComponent<ScrollRect>().content.Find("Sources"));
            _template.gameObject.SetActive(false);
            _resourceViewOrigColor = R.Game.UI.ResourceView.gameObject.transform.Find("ValueContainer").GetComponent<Panel>().color;
        }

        private Transform CreateContainer(Transform parent, string title)
        {
            if (!_template)
            {
                CreateTemplate();
            }
            Transform result = GameObject.Instantiate<Transform>(_template, parent);
            result.Find("Label").gameObject.GetComponent<Text>().text = title;
            return result;
        }

        private void OnSettingsChanged()
        {
            Invalidate();
        }

        protected void OnEnable()
        {
            Settings.Current.Subscribe(OnSettingsChanged);
        }

        protected void OnDisable()
        {
            Settings.Current.Unsubscribe(OnSettingsChanged);
        }

        private Dictionary<Item, int> DemandedItems
        {
            get
            {
                if (_neededItemsPerItem == null)
                {
                    GetEstimatatedNeededItems();
                }
                return _lastDemands;
            }
        }

        private IReadOnlyDictionary<Item, TransferData> LastTransfers
        {
            get
            {
                if (_lastTransfers == null)
                {
                    ImmutableList<Vehicle> vehicles = LazyManager<VehicleStationLocationManager>.Current.GetServicedVehicles(StationWindow.Location);
                    _lastTransfers = Manager<VehicleScheduleDataManager>.Current.GetStationTaskTransfersSum(vehicles, StationWindow.Location, out bool incompleteTransfers);
                    _incompleteTransfers = incompleteTransfers;
                }
                return _lastTransfers;
            }
        }

        private bool IncompleteTransfers
        {
            get
            {
                if (_incompleteTransfers == null)
                {
                    IReadOnlyDictionary<Item, TransferData> _ = LastTransfers;
                }
                return _incompleteTransfers.Value;
            }
        }

        private Transform _loadedContainer, _loadedItemsContainer;
        private Transform _unloadedContainer, _unloadedItemsContainer;
        private Text _loadedContainerTitle, _unloadedContainerTitle;
        private Transform _template;
        private float _offset;
        private bool _lastIncomplete = false;
        private bool? _incompleteTransfers = null;
        private IReadOnlyDictionary<Item, TransferData> _lastTransfers;
        private Dictionary<Item, float> _lastNeededItems;
        private Dictionary<Item, Dictionary<Item, float>> _neededItemsPerItem;
        private Dictionary<Item, int> _lastDemands;
        private Color _resourceViewOrigColor;

        private static Transform _actualTargetItemsContainer;
//        private enum Direction {unload, load};

        #region HARMONY
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationWindowOverviewTab), "Initialize")]
        private static void VehicleWindowScheduleTab_Initialize_prf(VehicleWindowScheduleTab __instance, StationWindow window)
        {
            StationWindowOverviewTabExtender tabExt = __instance.gameObject.AddComponent<StationWindowOverviewTabExtender>();
            tabExt.Initialize(window);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationWindowOverviewTab), "Initialize")]
        private static void VehicleWindowScheduleTab_Initialize_pof(VehicleWindowScheduleTab __instance, Transform ____targetsContainer)
        {
            ____targetsContainer.Find<Text>("Label").text += " " + LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/monthly_demand").ToUpper();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationWindowOverviewTab), "OnSelect")]
        private static void StationWindowOverviewTab_OnSelect_pof(StationWindowOverviewTab __instance)
        {
            StationWindowOverviewTabExtender tabExt = __instance.gameObject.GetComponent<StationWindowOverviewTabExtender>();
            if (tabExt != null)
            {
                tabExt.OnSelect();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationWindowOverviewTab), "InvalidateSourcesAndTargets")]
        private static void StationWindowOverviewTab_InvalidateSourcesAndTargets_prf(Transform ____targetsItemsContainer)
        {
            _actualTargetItemsContainer = ____targetsItemsContainer;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(StationWindowOverviewTab), "InvalidateSourcesAndTargets")]
        private static void StationWindowOverviewTab_InvalidateSourcesAndTargets_fin()
        {
            _actualTargetItemsContainer = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ResourceView), "ShowItem")]
        [HarmonyPatch(new Type[] { typeof(Item) })]
        private static void ResourceView_ShowItem_pof(ResourceView __instance, Item item)
        {
            if (__instance.transform.parent == _actualTargetItemsContainer)
            {
                StationWindowOverviewTabExtender tabExt = __instance.transform.GetComponentInParent<StationWindowOverviewTabExtender>();
                if (tabExt)
                {
                    Dictionary<Item, int> demandedItems = tabExt.DemandedItems;
                    if (demandedItems != null && demandedItems.TryGetValue(item, out int count))
                    {
                        __instance.ShowItem(item, count);
                    }
                }
            }
        }
        #endregion
    }
}
