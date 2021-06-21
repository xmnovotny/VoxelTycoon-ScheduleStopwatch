using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Buildings;
using VoxelTycoon.Researches;
using VoxelTycoon.Tracks;
using VoxelTycoon.UI;

namespace ScheduleStopwatch.UI
{
	class StationWindowLogisticTabConnectedStationItem : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		public void Initialize(VehicleStation station, VehicleStationLocation connectedStation)
		{
			this._station = station;
			this._connectedStation = connectedStation;
			base.transform.Find<Image>("Thumb").sprite = this.GetThumbIcon();
			base.transform.GetComponent<Button>().onClick.AddListener(delegate ()
			{
				GameCameraViewHelper.TryGoTo(this._connectedStation.VehicleStation, 70f);
			});
			this._settingsCog = base.transform.Find<Button>("ConnectionCog");
			VoxelTycoon.UI.ContextMenu.For(this._settingsCog, PickerBehavior.OverlayToRight, new Action<VoxelTycoon.UI.ContextMenu>(this.SetupContextMenu));
			this.SetSettingsCogVisibility(true);
			Tooltip.For(this, null, BuildingHelper.GetBuildingName(connectedStation.VehicleStation), "", 0);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
		}

		// Token: 0x06002F91 RID: 12177 RVA: 0x0009B6C2 File Offset: 0x000998C2
		public void OnPointerExit(PointerEventData eventData)
		{
		}

		private void SetupContextMenu(VoxelTycoon.UI.ContextMenu menu)
		{
			menu.AddItem("Remove", () => RemoveItem());
		}

		private void RemoveItem()
		{
			LazyManager<StationDemandManager>.Current.RemoveConnectedStation(_station.Location, _connectedStation);
		}

		private Sprite GetThumbIcon()
		{
			int assetId = this._connectedStation.VehicleStation.SharedData.AssetId;
			BuildingRecipe recipe;
			if (LazyManager<BuildingRecipeManager>.Current.TryGet(assetId, out recipe))
			{
				return LazyManager<IconRenderer>.Current.GetBuildingRecipeIcon(recipe);
			}
			return LazyManager<IconRenderer>.Current.GetBuildingIcon(assetId, "default");
		}

		private void SetSettingsCogVisibility(bool isVisible)
		{
			this._settingsCog.transform.localScale = Vector3.one * (float)(isVisible ? 1 : 0);
		}

		private VehicleStationLocation _connectedStation;
		private Button _settingsCog;
		private VehicleStation _station;

	}
}
