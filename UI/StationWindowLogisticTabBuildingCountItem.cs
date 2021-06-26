using VoxelTycoon;

namespace ScheduleStopwatch.UI
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using VoxelTycoon.Buildings;
    using VoxelTycoon.Recipes;
    using VoxelTycoon.Tracks;
    using VoxelTycoon.UI;

    /// <summary>
    /// Defines the <see cref="StationWindowLogisticTabBuildingCountItem" />.
    /// </summary>
    class StationWindowLogisticTabBuildingCountItem : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
    {
        /// <summary>
        /// The Initialize.
        /// </summary>
        /// <param name="station">The station<see cref="VehicleStation"/>.</param>
        /// <param name="building">The building<see cref="Building"/>.</param>
        /// <param name="recipe">The recipe<see cref="Recipe"/>.</param>
        /// <param name="count">The count<see cref="float"/>.</param>
        public void Initialize(VehicleStation station, Building building, Recipe recipe, float count)
        {
            this._station = station;
            this._building = building;
            this._count = Mathf.Ceil(count * 10) / 10;
            this._recipe = recipe;
            base.transform.Find<Image>("Thumb").sprite = this.GetThumbIcon();
            Image image = base.transform.Find<Image>("ItemImage");

            if (this._recipe !=  null)
            {
                image.sprite = GetItemIcon();
                image.gameObject.SetActive(true);
            } else
            {
                image.gameObject.SetActive(false);
            }
            this._settingsCog = base.transform.Find<Button>("ConnectionCog");
            this._valueText = base.transform.Find<Text>("ValueContainer/Value");
            this._valueText.text = _count.ToString("N1");
            this._valueText.transform.parent.gameObject.SetActive(true);
            //			VoxelTycoon.UI.ContextMenu.For(this._settingsCog, PickerBehavior.OverlayToRight, new Action<VoxelTycoon.UI.ContextMenu>(this.SetupContextMenu));
            this.SetSettingsCogVisibility(false);
            Tooltip.For(this, null, BuildingHelper.GetBuildingName(_building), GetTooltipText, 0);
        }

        /// <summary>
        /// The OnPointerEnter.
        /// </summary>
        /// <param name="eventData">The eventData<see cref="PointerEventData"/>.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        /// <summary>
        /// The OnPointerExit.
        /// </summary>
        /// <param name="eventData">The eventData<see cref="PointerEventData"/>.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
        }

        /// <summary>
        /// The SetupContextMenu.
        /// </summary>
        /// <param name="menu">The menu<see cref="VoxelTycoon.UI.ContextMenu"/>.</param>
        private void SetupContextMenu(VoxelTycoon.UI.ContextMenu menu)
        {
        }

        /// <summary>
        /// The GetTooltipText.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        private string GetTooltipText()
        {
            return (_recipe != null ? _recipe.DisplayName : "") + " (" + _count.ToString("N1") + ")";
        }

        /// <summary>
        /// The GetThumbIcon.
        /// </summary>
        /// <returns>The <see cref="Sprite"/>.</returns>
        private Sprite GetThumbIcon()
        {
            int assetId = this._building.SharedData.AssetId;
            BuildingRecipe recipe;
            if (LazyManager<BuildingRecipeManager>.Current.TryGet(assetId, out recipe))
            {
                return LazyManager<IconRenderer>.Current.GetBuildingRecipeIcon(recipe);
            }
            return LazyManager<IconRenderer>.Current.GetBuildingIcon(assetId, "default");
        }

        private Sprite GetItemIcon()
        {
            int assetId = this._recipe.OutputItems[0].Item.AssetId;
            return LazyManager<IconRenderer>.Current.GetItemIcon(assetId);
        }

        /// <summary>
        /// The SetSettingsCogVisibility.
        /// </summary>
        /// <param name="isVisible">The isVisible<see cref="bool"/>.</param>
        private void SetSettingsCogVisibility(bool isVisible)
        {
            this._settingsCog.transform.localScale = Vector3.one * (float)(isVisible ? 1 : 0);
        }

        private Building _building;

        private float _count;

        private Recipe _recipe;

        private Button _settingsCog;

        private VehicleStation _station;

        private Text _valueText;
    }
}
