using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;

namespace ScheduleStopwatch.UI
{
    public class CargoCapacityIndicatorItem: MonoBehaviour
    {
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
					_thumb.sprite = LazyManager<IconRenderer>.Current.GetItemIcon(value.AssetId);
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

		public void UpdateItemData(Item item, float count, float? routeTotalCount=null)
		{
			UpdateCount(count, routeTotalCount);
			Item = item;
		}
	}
}
