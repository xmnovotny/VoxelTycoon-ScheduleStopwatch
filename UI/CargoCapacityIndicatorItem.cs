using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI.VehicleUnitPickerWindowViews;

namespace ScheduleStopwatch.UI
{
	public class CargoCapacityIndicatorItem : MonoBehaviour
	{
		
		private static CargoCapacityIndicatorItem _template;
		private Image _thumb;
		private Text _text;
		private Item _item;
		public Item Item
        {
			get
            {
				return _item;
            }
			set
            {
				if (value != _item)
                {
					_item = value;
					if (value != null)
					{
						_thumb.sprite = LazyManager<IconRenderer>.Current.GetItemIcon(value.AssetId);
						_thumb.transform.parent.gameObject.SetActive(true);
					}
					else
                    {
						_thumb.transform.parent.gameObject.SetActive(false);
                    }
				}
			}
        }

			// Token: 0x060030CA RID: 12490 RVA: 0x0009FEFC File Offset: 0x0009E0FC
		public void Initialize(Item item, float count, float? routeTotalCount = null)
		{
			_thumb = base.transform.GetComponentInChildren<Image>();
			_text = base.transform.GetComponentInChildren<Text>();
			UpdateItemData(item, count, routeTotalCount);
		}

		public void UpdateCount(float count, float? routeTotalCount = null)
        {
			_text.text = count.ToString("N0") + (routeTotalCount != null ? "/" + routeTotalCount.Value.ToString("N0") : "");
		}

		public void UpdateAsOverflowCount(int count)
		{
			Item = null;
			_text.text = "+" + count.ToString();
		}

		public void UpdateItemData(Item item, float count, float? routeTotalCount=null)
		{
			UpdateCount(count, routeTotalCount);
			Item = item;
		}

		public static CargoCapacityIndicatorItem GetInstance(Transform parent)
        {
			if (_template == null)
			{
				_template = CreateTemplate();
			}
			CargoCapacityIndicatorItem item = UnityEngine.Object.Instantiate<CargoCapacityIndicatorItem>(_template, parent);
			item.gameObject.name = "CargoCapacityItem";
			return item;
		}

		private static CargoCapacityIndicatorItem CreateTemplate()
        {
			DepotWindowVehicleListItemStoragesViewTooltipItem item = UnityEngine.Object.Instantiate<DepotWindowVehicleListItemStoragesViewTooltipItem>(R.Game.UI.DepotWindow.DepotWindowVehicleListItemStoragesViewTooltipItem);
			Transform itemTransform = item.transform;
			UnityEngine.Object.DestroyImmediate(item);

			CargoCapacityIndicatorItem result = itemTransform.gameObject.AddComponent<CargoCapacityIndicatorItem>();
			LayoutElement layout = itemTransform.GetComponent<LayoutElement>();
			layout.flexibleWidth = 0.00f;
			return result;
		}
	}
}
