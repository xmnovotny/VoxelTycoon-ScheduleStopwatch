using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Audio;
using VoxelTycoon.Buildings;
using VoxelTycoon.Cities;
using VoxelTycoon.Game;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Localization;
using VoxelTycoon.Researches;
using VoxelTycoon.Theming;
using VoxelTycoon.UI;

namespace ScheduleStopwatch.UI
{
	[HarmonyPatch]
    class DemandPickerTool: ITool
    {
		public Action<Building> OnBuildingPicked { get; set; }
		public HashSet<IStorageNetworkNode> DisabledNodes { get; set; }

		public void Activate()
		{
			_activatedCount++;
			this.ToggleDefaultTint(true);
			this._tooltip = UIManager.Current.CreateFrame<Tooltip>(Input.mousePosition);
			this._tooltip.GetComponent<Canvas>().overridePixelPerfect = false;
			this._tooltip.Show();
			LazyManager<WhiteMode>.Current.Request();
			LazyManager<HotkeyPanel>.Current.Add(S.HoldToPickMany).AddKey(LazyManager<VoxelTycoon.Settings>.Current.MultipleModeKey);
		}

		// Token: 0x06002CF4 RID: 11508 RVA: 0x0008F988 File Offset: 0x0008DB88
		public bool Deactivate(bool soft)
		{
			_activatedCount--;
			this._tooltip.Close();
			this.ToggleDefaultTint(false);
/*			Action onDeactivated = this.OnDeactivated;
			if (onDeactivated != null)
			{
				onDeactivated();
			}*/
			LazyManager<WhiteMode>.Current.Release();
			LazyManager<HotkeyPanel>.Current.Clear();
			return true;
		}

		public bool OnUpdate()
		{
			bool key = LazyManager<InputManager>.Current.GetKey(LazyManager<VoxelTycoon.Settings>.Current.MultipleModeKey);
			if (this._deactivate && !key)
			{
				return true;
			}
			Building building = null;
			if (!InputHelper.OverUI)
			{
				building = ObjectRaycaster.Get<Building>((Component comp) => comp is Store || comp is Lab);
			}
			else
			{
				GameObject gameObject = InputHelper.GetUIGameObjectUnderPointer();
				if (gameObject != null)
				{
					DemandIndicator demandInd;
					if ((demandInd = gameObject.GetComponent<DemandIndicator>()) != null)
					{
						CityDemand demand = Traverse.Create(demandInd).Field<CityDemand>("_demand").Value;
						if (demand != null && demand.Store)
						{
							building = demand.Store;
						}
					}
				}
			}
			if (building != null)
			{
				if (this._building != building)
				{
					if (this._building != null)
					{
						this._building.SetTint(null);
					}
					this._building = building;
					this._canPick = this.CanPick(building);
					if (_canPick)
                    {
						this._tooltip.Background = new PanelColor(DemandPickerTool.ReadyToPickTooltipColor, 0f);
						this._tooltip.Text = LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/add_building_for_demand");
					}
					else
                    {
						this._tooltip.Background = new PanelColor(DemandPickerTool.WrongReadyToPickHighlightColor, 0f);
						this._tooltip.Text = LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/already_added");
						building.SetTint(new Color?(WrongReadyToPickHighlightColor));
					}
				}
				if (_canPick)
				{
					UIManager.Current.SetCursor(Cursors.Pointer);
					if (InputHelper.MouseDown)
					{

						this.OnBuildingPicked?.Invoke(building);
						Manager<SoundManager>.Current.PlayOnce(R.Audio.Click, null);
						if (_indicatorEntered)
						{
							_disableIndicatorUntilExit = true;
						}
						if (!key)
						{
							return true;
						}
						DisabledNodes?.Add(building as IStorageNetworkNode);
						LazyManager<BuildingTintManager>.Current.SetDefaultColor(building, null);
						building.SetTint(null);
						this._deactivate = true;
					}
				}
			}
			else
			{
				if (this._building != null)
				{
					this._building.SetTint(null);
					this._building = null;
				}
				this._tooltip.Background = new PanelColor(DemandPickerTool.PickingTooltipColor, 0f);
				this._tooltip.Text = LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/pick_store_or_lab");
			}
			this._tooltip.RectTransform.anchoredPosition = (new Vector2(Input.mousePosition.x, Input.mousePosition.y) + new Vector2(-10f, -16f)) / UIManager.Current.Scale;
			this._tooltip.ClampPositionToScreen();
			return InputHelper.MouseDown && !key;
		}

		private static Color ReadyToPickTooltipColor
		{
			get
			{
				return LazyManager<ThemeManager>.Current.Theme.GetColor("PickerToolReadyToPickTooltip");
			}
		}

		private static Color WrongReadyToPickHighlightColor
		{
			get
			{
				return LazyManager<ThemeManager>.Current.Theme.GetColor("WhiteModeWrongReadyToPickTint");
			}
		}

		private static Color PickingTooltipColor
		{
			get
			{
				return LazyManager<ThemeManager>.Current.Theme.GetColor("VehicleDestinationPickerToolPickingTooltip");
			}
		}

		private bool CanPick(Building building)
        {
			if (building is IStorageNetworkNode node)
            {
				return DisabledNodes?.Contains(node) == false;
            }
			return false;
        }

		private void ToggleDefaultTint(bool on)
		{
			ImmutableList<Store> allStores = LazyManager<BuildingManager>.Current.GetAll<Store>();
			for (int i = 0; i < allStores.Count; i++)
			{
				Store store = allStores[i];
				if (on && DisabledNodes?.Contains(store) == true)
                {
					continue;
                }
				LazyManager<BuildingTintManager>.Current.SetDefaultColor(store, on ? new Color?(GameColors.WhiteModeCompanyTintColor) : null);
				store.SetTint(null);
			}

			ImmutableList<Lab> allLabs = LazyManager<BuildingManager>.Current.GetAll<Lab>();
			for (int i = 0; i < allLabs.Count; i++)
			{
				Lab lab = allLabs[i];
				if (on && DisabledNodes?.Contains(lab) == true)
				{
					continue;
				}
				LazyManager<BuildingTintManager>.Current.SetDefaultColor(lab, on ? new Color?(GameColors.WhiteModeCompanyTintColor) : null);
				lab.SetTint(null);
			}
		}

		// Token: 0x04001211 RID: 4625
		private Tooltip _tooltip;
        private bool _deactivate;
		private bool _canPick;
		private Building _building;

		static private int _activatedCount = 0;
		static private bool _indicatorEntered = false;
		static private bool _disableIndicatorUntilExit = false;

        #region HARMONY
        [HarmonyPrefix]
		[HarmonyPatch(typeof(DemandIndicator), "OnPointerClick")]
		static private bool DemandIndicator_OnPointerClick_prf()
		{
			//disable click on demand indicator when adding new demand
			return _activatedCount == 0 && !_disableIndicatorUntilExit;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(DemandIndicator), "OnPointerEnter")]
		static private void DemandIndicator_OnPointerEnter_pof()
		{
			//disable click on demand indicator when adding new demand
			_indicatorEntered = true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(DemandIndicator), "OnPointerExit")]
		static private void DemandIndicator_OnPointerExit_pof()
		{
			//disable click on demand indicator when adding new demand
			_indicatorEntered = false;
			_disableIndicatorUntilExit = false;
		}
        #endregion
    }
}
