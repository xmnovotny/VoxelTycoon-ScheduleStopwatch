using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Buildings;
using VoxelTycoon.Game;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Game.UI.StorageNetworking;
using VoxelTycoon.Tracks;
using VoxelTycoon.UI;
using VoxelTycoon.UI.Controls;

namespace ScheduleStopwatch.UI
{
	[HarmonyPatch]
    class StationWindowLogisticsTab: MonoBehaviour, IWindowTabSelectHandler, IWindowTabDeselectHandler
    {
		public void Initialize(VehicleStation station)
		{
			this._station = station;

			this._demandBuildingNodesGrid = base.transform.Find<ScrollRect>("Head/DemandBuildingNodes/ScrollView").content;
			Transform buttonCont = Instantiate<Transform>(GetAddButtonTemplate(), this._demandBuildingNodesGrid);
			buttonCont.name = "AddDemandButton";
			_addDemandButton = buttonCont.Find<Button>("Button");
			_addDemandButton.onClick.AddListener(delegate ()
			{
				UIManager.Current.SetTool(new DemandPickerTool()
				{
					OnBuildingPicked = delegate (Building building)
					{
						FileLog.Log("Building picked " + building.DisplayName);
						OnDemandPicked(building);
					}
				}, false);
			});
			Tooltip.For(_addDemandButton, "Add a new building with demand (Lab or Store)");

			this._connectedStationsGrid = base.transform.Find<ScrollRect>("Head/ConnectedStations/ScrollView").content;
			Transform stationButtonCont = Instantiate<Transform>(GetAddButtonTemplate(), this._connectedStationsGrid);
			stationButtonCont.name = "AddStationButton";
			_addStationButton = stationButtonCont.Find<Button>("Button");
			_addStationButton.onClick.AddListener(delegate ()
			{
				UIManager.Current.SetTool(new StationPickerTool()
				{
					OnStationPicked = delegate (VehicleStation stationToAdd)
					{
						FileLog.Log("Station picked " + stationToAdd.name);
						OnStationPicked(stationToAdd);
					}
				}, false);
			});
		}

		public void OnBlur()
		{
		}

		// Token: 0x06002F84 RID: 12164 RVA: 0x0009B268 File Offset: 0x00099468
		public void OnDeselect()
		{
			LazyManager<StorageNetworkManager>.Current.NodeChanged -= this.OnNodeChanged;
			LazyManager<StationDemandManager>.Current.DemandChange -= this.OnNodeChanged;
			LazyManager<StationDemandManager>.Current.ConnectedStationChange -= this.OnConnectedStationChange;
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
			LazyManager<StorageNetworkManager>.Current.NodeChanged += this.OnNodeChanged;
			LazyManager<StationDemandManager>.Current.DemandChange += this.OnNodeChanged;
			LazyManager<StationDemandManager>.Current.ConnectedStationChange += this.OnConnectedStationChange;
			//			LazyManager<WhiteMode>.Current.Request();
		}

		private void OnNodeChanged(IStorageNetworkNode node)
		{
			if (node == (IStorageNetworkNode)_station)
			{
				InvalidateDemandBuildings();
			}
		}

		private void OnConnectedStationChange(VehicleStationLocation station)
        {
			if (station == _station.Location)
            {
				InvalidateConnectedStations();
            }
        }

		private void InvalidateDemandBuildings()
		{
			ClearExceptButton(_demandBuildingNodesGrid, "AddDemandButton");
			foreach (IStorageNetworkNode connectedNode in DemandHelper.GetStationDemandNodes(_station, false))
            {
				Instantiate<StationWindowLogisticTabDemandBuildingItem>(GetDemandBuildingsItemTemplate(), this._demandBuildingNodesGrid).Initialize(this._station, connectedNode, false);
			}
			foreach (IStorageNetworkNode connectedNode in LazyManager<StationDemandManager>.Current.GetAdditionalDemandsEnum(_station.Location))
			{
				Instantiate<StationWindowLogisticTabDemandBuildingItem>(GetDemandBuildingsItemTemplate(), this._demandBuildingNodesGrid).Initialize(this._station, connectedNode, true);
			}
			_addDemandButton.transform.parent.SetAsLastSibling();
		}

		private void InvalidateConnectedStations()
        {
			ClearExceptButton(_connectedStationsGrid, "AddStationButton");
			foreach (VehicleStationLocation connectedStation in LazyManager<StationDemandManager>.Current.GetConnectedStationsEnum(_station.Location))
			{
				Instantiate<StationWindowLogisticTabConnectedStationItem>(GetConnectedStationItemTemplate(), this._connectedStationsGrid).Initialize(this._station, connectedStation);
			}
			_addStationButton.transform.parent.SetAsLastSibling();
		}

		private void ClearExceptButton(Transform transform, string buttonName)
        {
			foreach (object obj in transform)
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
			manager.AddConnectedStation(_station.Location, station.Location);
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

		private static StationWindowLogisticsTab GetTemplate()
        {
			if (_template == null)
			{
				StorageNetworkTab tab = Instantiate<StorageNetworkTab>(R.Game.UI.StorageNetworking.StorageNetworkTab);
				Transform tabTransf = tab.transform;
				DestroyImmediate(tab);
				_template = tabTransf.gameObject.AddComponent<StationWindowLogisticsTab>();
				tabTransf.Find("Placeholder").SetActive(false);
				Transform connectedNodes = tabTransf.Find("Head/ConnectedNodes");
				connectedNodes.name = "DemandBuildingNodes";
				connectedNodes.Find<Text>("Label").text = "Buildings with demand".ToUpper();

				Transform connectedStations = Instantiate<Transform>(connectedNodes, connectedNodes.parent);
				connectedStations.name = "ConnectedStations";
				connectedStations.Find<Text>("Label").text = "Connected stations".ToUpper();
			}
			return _template;
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

		private float _offset;
		private Transform _demandBuildingNodesGrid;
		private Transform _connectedStationsGrid;
		private VehicleStation _station;
		private Button _addDemandButton;
		private Button _addStationButton;

		private static StationWindowLogisticTabDemandBuildingItem _demandItemTemplate;
		private static StationWindowLogisticsTab _template;
		private static Transform _addButtonTemplate;
		private static StationWindowLogisticTabConnectedStationItem _connectedStationItemTemplate;

		#region HARMONY
		[HarmonyPostfix]
		[HarmonyPatch(typeof(StationWindow), "Prepare")]
		private static void StationWindow_Prepare_pof(StationWindow __instance)
		{
			if (!__instance.Location.IsDead)
			{
				StationWindowLogisticsTab tab = Instantiate<StationWindowLogisticsTab>(GetTemplate());
				tab.Initialize(__instance.Location.VehicleStation);
				__instance.AddTab("Logistics II", tab.transform, UIColors.Solid.HeroBackground, TabContentHideMode.Deactivate);
			}
		}

		#endregion
	}
}
