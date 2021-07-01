﻿using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
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
using XMNUtils;
using static ScheduleStopwatch.VehicleScheduleCapacity;

namespace ScheduleStopwatch.UI
{
	[HarmonyPatch]
    internal class StationWindowLogisticsTab: MonoBehaviour, IWindowTabSelectHandler, IWindowTabDeselectHandler
    {
		public void Initialize(VehicleStation station)
		{
			this._station = station;
			Locale locale = LazyManager<LocaleManager>.Current.Locale;

			this._demandBuildingNodesGrid = base.transform.Find<ScrollRect>("Head/DemandBuildingNodes/ScrollView").content;
			Transform buttonCont = Instantiate<Transform>(GetAddButtonTemplate(), this._demandBuildingNodesGrid);
			buttonCont.name = "AddDemandButton";
			_addDemandButton = buttonCont.Find<Button>("Button");
			_addDemandButton.onClick.AddListener(delegate ()
			{
				UIManager.Current.SetTool(new DemandPickerTool()
				{
					OnBuildingPicked = OnDemandPicked,
					DisabledNodes = new HashSet<IStorageNetworkNode>(LazyManager<StationDemandManager>.Current.GetCombinedStationDemandNodesEnum(station, true))
				}, false);
			});
			Tooltip.For(_addDemandButton, locale.GetString("schedule_stopwatch/add_new_demand_tooltip"));

			this._connectedStationsGrid = base.transform.Find<ScrollRect>("Head/ConnectedStations/ScrollView").content;
			Transform stationButtonCont = Instantiate<Transform>(GetAddButtonTemplate(), this._connectedStationsGrid);
			stationButtonCont.name = "AddStationButton";
			_addStationButton = stationButtonCont.Find<Button>("Button");
			_addStationButton.onClick.AddListener(delegate ()
			{
				UIManager.Current.SetTool(new StationPickerTool()
				{
					OnStationPicked = OnStationPicked,
					DisabledStations = LazyManager<StationDemandManager>.Current.GetConnectedStationsHashset(station, true)
				}, false);
			});
			Tooltip.For(_addStationButton, locale.GetString("schedule_stopwatch/connect_station_tooltip"));

			Transform bodyContent = base.transform.Find("Body/WindowScrollView").GetComponent<ScrollRect>().content;
			_demandedContainer = bodyContent.Find("DemandedItems");
			Tooltip.For(_demandedContainer.Find("Label"), locale.GetString("schedule_stopwatch/demanded_items_tooltip"));
			_demandedItemsContainer = _demandedContainer.Find("Content");

			_productionNeededContainer = bodyContent.Find("NeededItems");
			Tooltip.For(_productionNeededContainer.Find("Label"), locale.GetString("schedule_stopwatch/needed_items_tooltip"));
			_productionNeededItemsContainer = _productionNeededContainer.Find("Content");

			_factoriesContainer = bodyContent.Find("NeededFactories");
			Tooltip.For(_factoriesContainer.Find("Label"), locale.GetString("schedule_stopwatch/needed_factories_tooltip"));
			_factoriesItemsContainer = _factoriesContainer.Find<ScrollRect>("ScrollView").content;
		}

		public void OnBlur()
		{
		}

		// Token: 0x06002F84 RID: 12164 RVA: 0x0009B268 File Offset: 0x00099468
		public void OnDeselect()
		{
			LazyManager<StationDemandManager>.Current.DemandChange -= this.OnNodeChanged;
			LazyManager<StationDemandManager>.Current.ConnectedStationChange -= this.OnConnectedStationChange;
			Settings.Current.Unsubscribe(OnSettingsChanged);
			//			LazyManager<WhiteMode>.Current.Release();
		}

		// Token: 0x06002F85 RID: 12165 RVA: 0x0009B2D1 File Offset: 0x000994D1
		public void OnFocus()
		{
		}

		// Token: 0x06002F86 RID: 12166 RVA: 0x0009B2F0 File Offset: 0x000994F0
		public void OnSelect()
		{
			this._offset = Time.unscaledTime;
			this.InvalidateDemandBuildings();
			this.InvalidateConnectedStations();
			this.Invalidate();
			LazyManager<StationDemandManager>.Current.DemandChange += this.OnNodeChanged;
			LazyManager<StationDemandManager>.Current.ConnectedStationChange += this.OnConnectedStationChange;
			Settings.Current.Subscribe(OnSettingsChanged);
			//			LazyManager<WhiteMode>.Current.Request();
		}

		protected void Update()
		{
			if (this._station is not {isActiveAndEnabled: true}) return;
			if (TimeHelper.OncePerUnscaledTime(1f, this._offset))
			{
				this.Invalidate();
			}
		}

		private void OnSettingsChanged()
		{
			Invalidate();
		}

		private void OnNodeChanged(IStorageNetworkNode node)
		{
			if (node == (IStorageNetworkNode)_station)
			{
				InvalidateDemandBuildings();
				Invalidate();
			}
		}

		private void OnConnectedStationChange(VehicleStationLocation station)
        {
			if (station == _station.Location)
            {
				InvalidateConnectedStations();
				InvalidateDemandBuildings();
				Invalidate();
			}
		}

		private void ClearCachedValues()
        {
			_neededItemsPerItem = null;
			_neededItems = null;
			_demands = null;
			_lastTransfers = null;
			_estimatedTransfers = null;
			_incompleteTransfers = null;
			_factoriesNeeded = null;
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void Invalidate()
        {
			ClearCachedValues();
			InvalidateNeededItems();
			InvalidateNeededFactories();
			LazyManager<StationWindowLogisticHelper>.Current.FillContainerWithNeededItems(_demandedContainer, _demandedItemsContainer, Demands);
		}

		private void InvalidateNeededItems()
        {
			Dictionary<Item, float> items = NeededItems;
            IReadOnlyDictionary<Item, TransferData> transfers = LastTransfers;
			Dictionary<Item, int> demands = Demands;
			StationWindowLogisticHelper helper = LazyManager<StationWindowLogisticHelper>.Current;

			List<ResourceView> resourceViews = new List<ResourceView>();
			_productionNeededItemsContainer.transform.GetComponentsInChildren<ResourceView>(resourceViews);
			int count = 0;
			foreach (KeyValuePair<Item, float> pair in items)
			{
				int value = pair.Value.RoundToInt();
				if (transfers != null && transfers.TryGetValue(pair.Key, out TransferData transfData) && transfData.load > 0 && transfData.unload > 0)
                {
					value -= transfData.load;
                }
				if (demands != null && demands.TryGetValue(pair.Key, out int demand))
                {
					value -= demand;
                }

				if (value <= 0) continue;
				
				helper.AddOneItemToContainer(resourceViews, count, _productionNeededItemsContainer, pair.Key, value, null, null);
				count++;
			}
			_productionNeededContainer.gameObject.SetActive(count > 0);
			helper.DeactivateResourceViews(resourceViews, count);
		}

		private void InvalidateNeededFactories()
        {
			List<StationWindowLogisticTabBuildingCountItem> items = new();
			_factoriesItemsContainer.GetComponentsInChildren(true, items);
			int count = 0;
			
			//mines
			IReadOnlyDictionary<Item, TransferData> transfers = LastTransfers;
			RecipeHelper helper = LazyManager<RecipeHelper>.Current;
			foreach (KeyValuePair<Item, float> itemAmount in NeededItems)
			{
				if (itemAmount.Value < 0.1 || (transfers.TryGetValue(itemAmount.Key, out TransferData data) && data.unload > 0))
				{
					continue;
				}
				(int? itemPerMonth, Mine mine) = helper.GetMinedItemsPerMineAndMonth(itemAmount.Key);
				if (itemPerMonth.HasValue)
				{
					StationWindowLogisticTabBuildingCountItem item = GetBuildingCountItem(items, count, _factoriesItemsContainer);
					item.Initialize(_station, mine, null, itemAmount.Value / itemPerMonth.Value, itemAmount.Key);
					item.gameObject.SetActive(true);
					count++;
				}
			}

			Dictionary<(Device device, Recipe recipe), float> factoriesNeeded = FactoriesNeeded;
			//devices
			foreach (KeyValuePair<(Device device, Recipe recipe), float> deviceAmount in (from f in factoriesNeeded orderby f.Key.device.DisplayName.ToString(), f.Key.recipe.DisplayName.ToString() select f))
			{
				if (deviceAmount.Value > 0.01)
                {
                    StationWindowLogisticTabBuildingCountItem item = GetBuildingCountItem(items, count, _factoriesItemsContainer);
					item.Initialize(_station, deviceAmount.Key.device, deviceAmount.Key.recipe, deviceAmount.Value);
					item.gameObject.SetActive(true);
					count++;
				}
			}

			_factoriesContainer.gameObject.SetActive(count > 0);
            for (; count < items.Count; count++)
            {
				items[count].gameObject.SetActive(false);
            }
        }

		private void InvalidateDemandBuildings()
		{
			ClearExceptButton(_demandBuildingNodesGrid, "AddDemandButton");
			foreach (IStorageNetworkNode connectedNode in LazyManager<StationDemandManager>.Current.GetCombinedStationDemandNodesEnum(_station, false))
            {
				Instantiate(GetDemandBuildingsItemTemplate(), _demandBuildingNodesGrid).Initialize(_station, connectedNode, false);
			}
			foreach (IStorageNetworkNode connectedNode in LazyManager<StationDemandManager>.Current.GetAdditionalDemandsEnum(_station))
			{
				Instantiate(GetDemandBuildingsItemTemplate(), _demandBuildingNodesGrid).Initialize(_station, connectedNode, true);
			}
			_addDemandButton.transform.parent.SetAsLastSibling();
		}

		private void InvalidateConnectedStations()
        {
			ClearExceptButton(_connectedStationsGrid, "AddStationButton");
			foreach (VehicleStation connectedStation in LazyManager<StationDemandManager>.Current.GetConnectedStationsEnum(_station))
			{
				Instantiate(GetConnectedStationItemTemplate(), this._connectedStationsGrid).Initialize(this._station, connectedStation);
			}
			_addStationButton.transform.parent.SetAsLastSibling();
		}

		private void ClearExceptButton(Transform trans, string buttonName)
        {
			foreach (object obj in trans)
			{
				Transform transf = (Transform)obj;
				if (transf.name != buttonName)
				{
					Destroy(((Transform)obj).gameObject);
				}
			}
		}
		
		private void OnDemandPicked(Building building)
        {
			StationDemandManager manager = LazyManager<StationDemandManager>.Current;
			if (building is IStorageNetworkNode storageNode)
			{
				manager.AddDemand(_station.Location, storageNode);
			}
        }
		private void OnStationPicked(VehicleStation station)
		{
			StationDemandManager manager = LazyManager<StationDemandManager>.Current;
			manager.AddConnectedStation(_station, station);
		}

		private StationWindowLogisticTabBuildingCountItem GetBuildingCountItem(List<StationWindowLogisticTabBuildingCountItem> items, int index, Transform parent)
		{
			if (index < items.Count)
			{
				return items[index];
			}
			if (_buildingCountItemsTemplate == null)
			{
				CreateBuildingCountItemTemplate();
			}
			StationWindowLogisticTabBuildingCountItem result = Instantiate(_buildingCountItemsTemplate, parent);
			return result;
		}

        private void CreateBuildingCountItemTemplate()
        {
			StorageNetworkTabConnectedNodeItem nodeItem = Instantiate<StorageNetworkTabConnectedNodeItem>(R.Game.UI.StorageNetworking.StorageNetworkTabConnectedNodeItem);
			Transform nodeItemTransf = nodeItem.transform;
			DestroyImmediate(nodeItem);
			DestroyImmediate(nodeItemTransf.gameObject.GetComponent<Button>());
			DestroyImmediate(nodeItemTransf.gameObject.GetComponent<ClickableDecorator>());
			Instantiate<Transform>(R.Game.UI.ResourceView.transform.Find("ValueContainer"), nodeItemTransf).name = "ValueContainer";

			Instantiate<Transform>(R.Game.UI.ResourceView.transform.Find("Background"), nodeItemTransf);
			Transform trans;
			(trans = Instantiate<Transform>(R.Game.UI.ResourceView.transform.Find("Image"), nodeItemTransf)).name = "ItemImage";
			trans.GetComponent<RectTransform>().anchoredPosition = new Vector2(-15, 15);
			_buildingCountItemsTemplate = nodeItemTransf.gameObject.AddComponent<StationWindowLogisticTabBuildingCountItem>();
		}

		private static StationWindowLogisticTabDemandBuildingItem GetDemandBuildingsItemTemplate()
        {
			if (_demandItemTemplate == null)
			{
				StorageNetworkTabConnectedNodeItem nodeItem = Instantiate<StorageNetworkTabConnectedNodeItem>(R.Game.UI.StorageNetworking.StorageNetworkTabConnectedNodeItem);
				Transform nodeItemTransf = nodeItem.transform;
				DestroyImmediate(nodeItem);
				_demandItemTemplate = nodeItemTransf.gameObject.AddComponent<StationWindowLogisticTabDemandBuildingItem>();
			}
			return _demandItemTemplate;
		}
		private static StationWindowLogisticTabConnectedStationItem GetConnectedStationItemTemplate()
		{
			if (_connectedStationItemTemplate == null)
			{
				StorageNetworkTabConnectedNodeItem nodeItem = Instantiate<StorageNetworkTabConnectedNodeItem>(R.Game.UI.StorageNetworking.StorageNetworkTabConnectedNodeItem);
				Transform nodeItemTransf = nodeItem.transform;
				DestroyImmediate(nodeItem);
				_connectedStationItemTemplate = nodeItemTransf.gameObject.AddComponent<StationWindowLogisticTabConnectedStationItem>();
			}
			return _connectedStationItemTemplate;
		}

		private static Transform GetStorageNetworkTab()
		{
			return Instantiate(R.Game.UI.StorageNetworking.StorageNetworkTab).transform;
		}
		
		private static StationWindowLogisticsTab GetTemplate()
        {
	        if (_template != null) return _template;
	        Locale locale = LazyManager<LocaleManager>.Current.Locale;
	        
	        Transform tabTransf = GetStorageNetworkTab();

	        _template = tabTransf.gameObject.AddComponent<StationWindowLogisticsTab>();
			tabTransf.Find("Placeholder").DestroyGameObject();
			Transform connectedNodes = tabTransf.Find("Head/ConnectedNodes");
			connectedNodes.name = "DemandBuildingNodes";
			connectedNodes.Find<Text>("Label").text = locale.GetString("schedule_stopwatch/buildings_with_demand").ToUpper();

			AddBuildingItemsContainer(connectedNodes.parent, locale.GetString("schedule_stopwatch/connected_stations").ToUpper(), "ConnectedStations");
			Transform body = tabTransf.Find("Body");
			body.DestroyGameObject();

			body = Instantiate<Transform>(R.Game.UI.StationWindow.StationWindowOverviewTab.transform.Find("Body"), tabTransf);
			body.name = "Body";
			body.Find("Placeholder")?.DestroyGameObject();
			Transform bodyContent = body.Find("WindowScrollView").GetComponent<ScrollRect>().content;
			bodyContent.Clear();

			Transform demandedContainer = LazyManager<StationWindowLogisticHelper>.Current.CreateItemsContainer(bodyContent, locale.GetString("schedule_stopwatch/demanded_items").ToUpper(), "DemandedItems");
			demandedContainer.SetActive(true);

			Transform productionNeededContainer = LazyManager<StationWindowLogisticHelper>.Current.CreateItemsContainer(bodyContent, locale.GetString("schedule_stopwatch/needed_items").ToUpper(), "NeededItems");
			productionNeededContainer.SetActive(true);

			Transform factNeededCont = AddBuildingItemsContainer(bodyContent, locale.GetString("schedule_stopwatch/factories_needed").ToUpper(), "NeededFactories");
			GridLayoutGroup group = factNeededCont.Find<GridLayoutGroup>("ScrollView/Viewport/Content");

			RectOffset padding = group.padding;
			padding.top = 0;
			group.padding = padding;

			Vector2 spacing = group.spacing;
			spacing.y = 16;
			group.spacing = spacing;
			return _template;
		}

		private static Transform AddBuildingItemsContainer(Transform parent, string title, string name = null)
        {
			if (_buildingItemsContainerTemplate == null)
            {
				_buildingItemsContainerTemplate = Instantiate<Transform>(GetStorageNetworkTab().Find("Head/ConnectedNodes"));
			}
			Transform result = Instantiate<Transform>(_buildingItemsContainerTemplate, parent);
			result.Find<Text>("Label").text = title;
			if (name != null)
            {
				result.name = name;
            }
			return result;
        }

		private static Transform GetAddButtonTemplate()
        {
			if (_addButtonTemplate == null)
			{
				_addButtonTemplate = Instantiate<Transform>(R.Game.UI.VehicleEditorWindow.Content.transform.Find("ScrollView/Viewport/Content/AddButton"));
				RectTransform transf = _addButtonTemplate.Find<RectTransform>("Button");
				Vector2 pos = transf.anchoredPosition;
				pos.y = 0;
				transf.anchoredPosition = pos;
			}
			return _addButtonTemplate;
		}

		private void CalculateEstimatedData()
        {
			Dictionary<Recipe, float> recipesNeeded = new();
			_neededItems = LazyManager<StationWindowLogisticHelper>.Current.GetEstimatatedNeededItems(_station, LastTransfers, out _neededItemsPerItem, out _demands, recipesNeeded);
			_factoriesNeeded = LazyManager<RecipeHelper>.Current.GetNeededDevicesPerMonth(recipesNeeded);
        }

		private void CalculateTransfers()
        {
			(_lastTransfers, _incompleteTransfers, _estimatedTransfers) = LazyManager<StationWindowLogisticHelper>.Current.GetTransfers(_station);
        }

		private Dictionary<Item, int> Demands
        {
			get
            {
				if (_demands == null)
                {
					CalculateEstimatedData();
                }
				return _demands;
            }
        }

		private IReadOnlyDictionary<Item, TransferData> LastTransfers
        {
			get
            {
				if (_lastTransfers == null)
                {
					CalculateTransfers();
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
					CalculateTransfers();
                }
				return _incompleteTransfers.Value;
            }
        }

        public Dictionary<Item, float> NeededItems
		{
			get
			{
				if (_neededItems == null)
                {
					CalculateEstimatedData();
                }
				return _neededItems;
			}
		}

        public Dictionary<Item, Dictionary<Item, float>> NeededItemsPerItem
		{
			get
			{
				if (_neededItemsPerItem == null)
                {
					CalculateEstimatedData();
                }
				return _neededItemsPerItem;
			}
		}

		public Dictionary<(Device device, Recipe recipe), float> FactoriesNeeded
        {
			get
            {
				if (_factoriesNeeded == null)
                {
					CalculateEstimatedData();
                }
				return _factoriesNeeded;
            }
        }

		private float _offset;
		private Transform _demandBuildingNodesGrid;
		private Transform _connectedStationsGrid;
		private VehicleStation _station;
		private Button _addDemandButton;
		private Button _addStationButton;
		private Transform _demandedContainer, _demandedItemsContainer;
		private Transform _productionNeededContainer, _productionNeededItemsContainer;
		private Transform _factoriesContainer, _factoriesItemsContainer;

		private static StationWindowLogisticTabDemandBuildingItem _demandItemTemplate;
		private static StationWindowLogisticsTab _template;
		private static Transform _addButtonTemplate;
		private static StationWindowLogisticTabConnectedStationItem _connectedStationItemTemplate;
		private static Transform _buildingItemsContainerTemplate;
		private static StationWindowLogisticTabBuildingCountItem _buildingCountItemsTemplate;

		private Dictionary<Item, float> _neededItems;
		private Dictionary<Item, Dictionary<Item, float>> _neededItemsPerItem;
		private Dictionary<Item, int> _demands;
		private IReadOnlyDictionary<Item, TransferData> _lastTransfers;
		private Dictionary<(Device device, Recipe recipe), float> _factoriesNeeded;
		private bool? _incompleteTransfers = null;
		private bool? _estimatedTransfers = null;

        #region HARMONY
        [HarmonyPostfix]
		[HarmonyPatch(typeof(StationWindow), "Prepare")]
		private static void StationWindow_Prepare_pof(StationWindow __instance)
		{
			if (!__instance.Location.IsDead)
			{
				StationWindowLogisticsTab tab = Instantiate<StationWindowLogisticsTab>(GetTemplate());
				tab.Initialize(__instance.Location.VehicleStation);
				__instance.AddTab(LocaleManager.Current.Locale.GetString("schedule_stopwatch/logistics_2"), tab.transform, UIColors.Solid.HeroBackground, TabContentHideMode.Deactivate);
			}
		}

		#endregion
	}
}
