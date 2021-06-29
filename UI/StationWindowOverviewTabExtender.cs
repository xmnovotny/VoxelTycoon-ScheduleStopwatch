using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Buildings;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Game.UI.StorageNetworking;
using VoxelTycoon.Localization;
using VoxelTycoon.Recipes;
using VoxelTycoon.Tracks;
using VoxelTycoon.UI;
using VoxelTycoon.UI.Controls;
using static ScheduleStopwatch.VehicleScheduleCapacity;
using Debug = System.Diagnostics.Debug;

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
            _unloadedContainer = LazyManager<StationWindowLogisticHelper>.Current.CreateItemsContainer(content, locale.GetString("schedule_stopwatch/monthly_unloaded_items").ToUpper(), "UnloadedItems");
            _unloadedContainer.SetSiblingIndex(2);
            _unloadedItemsContainer = _unloadedContainer.Find("Content");
            _unloadedContainerTitle = _unloadedContainer.Find("Label").gameObject.GetComponent<Text>();

            Tooltip.For(
                _unloadedContainerTitle.transform,
                () => LazyManager<StationWindowLogisticHelper>.Current.GetEstimatatedNeededItemsTooltipText(GetEstimatatedNeededItems(), LastTransfers),
                null
            );

            _loadedContainer = LazyManager<StationWindowLogisticHelper>.Current.CreateItemsContainer(content,locale.GetString("schedule_stopwatch/monthly_loaded_items").ToUpper(), "LoadedItems");
            _loadedContainer.SetSiblingIndex(3);
            _loadedItemsContainer = _loadedContainer.Find("Content");
            _loadedContainerTitle = _loadedContainer.Find("Label").gameObject.GetComponent<Text>();

            Tooltip.For(
                _loadedContainerTitle.transform,
                () => LazyManager<StationWindowLogisticHelper>.Current.GetEstimatatedNeededItemsTooltipText(GetEstimatatedNeededItems(), LastTransfers),
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
                _lastNeededItems = LazyManager<StationWindowLogisticHelper>.Current.GetEstimatatedNeededItems(StationWindow.Location.VehicleStation, LastTransfers, out _neededItemsPerItem, out _lastDemands);
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

        // ReSharper disable Unity.PerformanceAnalysis
        private void Invalidate()
        {
            Settings settings = Settings.Current;
            _lastTransfers = null;
            _lastNeededItems = null;
            _neededItemsPerItem = null;
            _lastDemands = null;
            _estimatedTransfers = null;
            _incompleteTransfers = null;
            if (settings.ShowStationLoadedItems || settings.ShowStationUnloadedItems)
            {
                IReadOnlyDictionary<Item, TransferData> transfers = LastTransfers;
                LazyManager<StationWindowLogisticHelper>.Current.FillContainerWithItems(_loadedContainer, _loadedItemsContainer, settings.ShowStationLoadedItems ? transfers : null, TransferDirection.loading, itemTooltipTextFunc: GetEstimatedItemsForOneLoadItemTooltipText);
                LazyManager<StationWindowLogisticHelper>.Current.FillContainerWithItems(_unloadedContainer, _unloadedItemsContainer, settings.ShowStationUnloadedItems ? transfers : null, TransferDirection.unloading, GetEstimatatedNeededItems());
                bool incomplete = IncompleteTransfers;
                bool estimated = EstimatedTransfers;
                if (_lastIncomplete != incomplete || _lastEstimated != estimated)
                {
                    _lastIncomplete = incomplete;
                    _lastEstimated = estimated;
                    Locale locale = LazyManager<LocaleManager>.Current.Locale;
                    string additionText = (incomplete ? " (" + locale.GetString("schedule_stopwatch/partial").ToUpper() + ")" : "")
                        + (estimated ? " " + locale.GetString("schedule_stopwatch/inaccurate").ToUpper() : "");
                    _loadedContainerTitle.text = locale.GetString("schedule_stopwatch/monthly_loaded_items").ToUpper() + additionText;
                    _unloadedContainerTitle.text = locale.GetString("schedule_stopwatch/monthly_unloaded_items").ToUpper() + additionText;
                }
            } else
            {
                _loadedContainer.SetActive(false);
                _unloadedContainer.SetActive(false);
            }
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
                    TaskTransfers transfersSum = new TaskTransfers();
                    _incompleteTransfers = false;
                    _estimatedTransfers = false;

                    foreach (VehicleStation connStation in LazyManager<StationDemandManager>.Current.GetConnectedStationsEnum(StationWindow.Location.VehicleStation, true))
                    {
                        ImmutableList<Vehicle> vehicles = LazyManager<VehicleStationLocationManager>.Current.GetServicedVehicles(connStation.Location);
                        Manager<VehicleScheduleDataManager>.Current.GetStationTaskTransfersSum(vehicles, connStation.Location, out bool incompleteTransfers, out bool isEstimated, transfersSum);
                        _incompleteTransfers |= incompleteTransfers;
                        _estimatedTransfers |= isEstimated;
                    }
                    _lastTransfers = transfersSum.Transfers;
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

                return _incompleteTransfers != null && _incompleteTransfers.Value;
            }
        }

        private bool EstimatedTransfers
        {
            get
            {
                if (_estimatedTransfers == null)
                {
                    IReadOnlyDictionary<Item, TransferData> _ = LastTransfers;
                }
                return _estimatedTransfers != null && _estimatedTransfers.Value;
            }
        }

        private Transform _loadedContainer, _loadedItemsContainer;
        private Transform _unloadedContainer, _unloadedItemsContainer;
        private Text _loadedContainerTitle, _unloadedContainerTitle;
        private float _offset;
        private bool _lastIncomplete = false, _lastEstimated = false;
        private bool? _incompleteTransfers = null;
        private bool? _estimatedTransfers = null;
        private IReadOnlyDictionary<Item, TransferData> _lastTransfers;
        private Dictionary<Item, float> _lastNeededItems;
        private Dictionary<Item, Dictionary<Item, float>> _neededItemsPerItem;
        private Dictionary<Item, int> _lastDemands;

        private static Transform _actualTargetItemsContainer;

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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationWindowOverviewTab), "OnSelect")]
        private static void StationWindowOverviewTab_OnSelect_prf(StationWindowOverviewTab __instance)
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
