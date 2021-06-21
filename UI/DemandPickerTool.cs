using System;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Audio;
using VoxelTycoon.Buildings;
using VoxelTycoon.Game;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Researches;
using VoxelTycoon.Theming;
using VoxelTycoon.UI;

namespace ScheduleStopwatch.UI
{
    class DemandPickerTool: ITool
    {
		public Action<Building> OnBuildingPicked { get; set; }

		public void Activate()
		{
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
			if (building != null)
			{
				if (this._building != building)
				{
					if (this._building != null)
					{
						this._building.SetTint(null);
					}
					this._building = building;
					this._tooltip.Background = new PanelColor(DemandPickerTool.ReadyToPickTooltipColor, 0f);
					this._tooltip.Text = "Add building for station demand";
				}
				UIManager.Current.SetCursor(Cursors.Pointer);
				if (InputHelper.WorldMouseDown)
				{
					
					this.OnBuildingPicked?.Invoke(building);
					Manager<SoundManager>.Current.PlayOnce(R.Audio.Click, null);
					if (!key)
					{
						return true;
					}
					this._deactivate = true;
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
				this._tooltip.Text = "Pick store or lab for station demand";
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

		private void ToggleDefaultTint(bool on)
		{
			ImmutableList<Store> allStores = LazyManager<BuildingManager>.Current.GetAll<Store>();
			for (int i = 0; i < allStores.Count; i++)
			{
				Store store = allStores[i];
				LazyManager<BuildingTintManager>.Current.SetDefaultColor(store, on ? new Color?(GameColors.WhiteModeCompanyTintColor) : null);
				store.SetTint(null);
			}

			ImmutableList<Lab> allLabs = LazyManager<BuildingManager>.Current.GetAll<Lab>();
			for (int i = 0; i < allLabs.Count; i++)
			{
				Lab store = allLabs[i];
				LazyManager<BuildingTintManager>.Current.SetDefaultColor(store, on ? new Color?(GameColors.WhiteModeCompanyTintColor) : null);
				store.SetTint(null);
			}
		}

		// Token: 0x04001211 RID: 4625
		private Tooltip _tooltip;
        private bool _deactivate;
		private Building _building;
    }
}
