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
			buttonCont.name = "AddButton";
			_addButton = buttonCont.Find<Button>("Button");
			_addButton.onClick.AddListener(delegate ()
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
		}

		public void OnBlur()
		{
		}

		// Token: 0x06002F84 RID: 12164 RVA: 0x0009B268 File Offset: 0x00099468
		public void OnDeselect()
		{
			LazyManager<StorageNetworkManager>.Current.NodeChanged -= this.OnNodeChanged;
			LazyManager<StationDemandManager>.Current.DemandChange -= this.OnNodeChanged;
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
			LazyManager<StorageNetworkManager>.Current.NodeChanged += this.OnNodeChanged;
			LazyManager<StationDemandManager>.Current.DemandChange += this.OnNodeChanged;
			//			LazyManager<WhiteMode>.Current.Request();
		}

		private void OnNodeChanged(IStorageNetworkNode node)
		{
			if (node == (IStorageNetworkNode)_station)
			{
				this.InvalidateDemandBuildings();
			}
		}
		private void InvalidateDemandBuildings()
		{
			ClearExceptButton(_demandBuildingNodesGrid, "AddButton");
			foreach (IStorageNetworkNode connectedNode in DemandHelper.GetStationDemandNodes(_station, false))
            {
				Instantiate<StationWindowLogisticTabDemandBuildingItem>(GetDemandBuildingsItemTemplate(), this._demandBuildingNodesGrid).Initialize(this._station, connectedNode, false);
			}
			foreach (IStorageNetworkNode connectedNode in LazyManager<StationDemandManager>.Current.GetAdditionalDemandsEnum(_station.Location))
			{
				Instantiate<StationWindowLogisticTabDemandBuildingItem>(GetDemandBuildingsItemTemplate(), this._demandBuildingNodesGrid).Initialize(this._station, connectedNode, true);
			}
			_addButton.transform.parent.SetAsLastSibling();
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
		private VehicleStation _station;
		private Button _addButton;

		private static StationWindowLogisticTabDemandBuildingItem _demandItemTemplate;
		private static StationWindowLogisticsTab _template;
		private static Transform _addButtonTemplate;

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
