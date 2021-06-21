using System;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Audio;
using VoxelTycoon.Buildings;
using VoxelTycoon.Game;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Researches;
using VoxelTycoon.Theming;
using VoxelTycoon.Tracks;
using VoxelTycoon.UI;

namespace ScheduleStopwatch.UI
{
	class StationPickerTool : ITool
	{
		public Action<VehicleStation> OnStationPicked { get; set; }

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
			VehicleStation station = null;
			if (!InputHelper.OverUI)
			{
				station = ObjectRaycaster.Get<VehicleStation>();
			}
			if (station != null)
			{
				if (this._building != station)
				{
					if (this._building != null)
					{
						this._building.SetTint(null);
					}
					this._building = station;
					this._tooltip.Background = new PanelColor(ReadyToPickTooltipColor, 0f);
					this._tooltip.Text = "Add connected station";
				}
				UIManager.Current.SetCursor(Cursors.Pointer);
				if (InputHelper.WorldMouseDown)
				{

					this.OnStationPicked?.Invoke(station);
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
				this._tooltip.Background = new PanelColor(PickingTooltipColor, 0f);
				this._tooltip.Text = "Pick station";
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
			ImmutableList<VehicleStation> allStations = LazyManager<BuildingManager>.Current.GetAll<VehicleStation>();
			for (int i = 0; i < allStations.Count; i++)
			{
				VehicleStation store = allStations[i];
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
