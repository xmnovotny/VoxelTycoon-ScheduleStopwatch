using System;
using System.Collections.Generic;
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
		public HashSet<VehicleStation> DisabledStations { get; set; }

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
				if (this._station != station)
				{
					if (this._station != null)
					{
						this._station.SetTint(null);
					}
					this._station = station;
					this._canPick = this.CanPick(station);
					if (_canPick)
					{
						this._tooltip.Background = new PanelColor(ReadyToPickTooltipColor, 0f);
						this._tooltip.Text = "Add connected station";
					}
					else
					{
						this._tooltip.Background = new PanelColor(WrongReadyToPickHighlightColor, 0f);
						this._tooltip.Text = "Already added";
						station.SetTint(new Color?(WrongReadyToPickHighlightColor));
					}
				}
				if (_canPick)
				{
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
			}
			else
			{
				if (this._station != null)
				{
					this._station.SetTint(null);
					this._station = null;
				}
				this._tooltip.Background = new PanelColor(PickingTooltipColor, 0f);
				this._tooltip.Text = "Pick station";
			}
			this._tooltip.RectTransform.anchoredPosition = (new Vector2(Input.mousePosition.x, Input.mousePosition.y) + new Vector2(-10f, -16f)) / UIManager.Current.Scale;
			this._tooltip.ClampPositionToScreen();
			return InputHelper.MouseDown && !key;
		}

		private bool CanPick(VehicleStation station)
		{
			return DisabledStations?.Contains(station) == false;
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
				VehicleStation station = allStations[i];
				if (DisabledStations?.Contains(station) == true)
                {
					continue;
                }
				LazyManager<BuildingTintManager>.Current.SetDefaultColor(station, on ? new Color?(GameColors.WhiteModeCompanyTintColor) : null);
				station.SetTint(null);
			}
		}

		// Token: 0x04001211 RID: 4625
		private Tooltip _tooltip;
		private bool _deactivate;
		private bool _canPick;
		private Building _station;
	}
}
