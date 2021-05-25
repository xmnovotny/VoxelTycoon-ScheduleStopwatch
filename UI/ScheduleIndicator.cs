using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Localization;
using VoxelTycoon.UI;
using VoxelTycoon.UI.Controls;

namespace ScheduleStopwatch.UI
{
    abstract class ScheduleIndicator: MonoBehaviour
    {

        private static Transform _baseTemplate;
        protected static Transform BaseTemplate
        {
            get
            {
                if (_baseTemplate == null)
                {
                    CreateBaseTemplate();
                }
                return _baseTemplate;
            }
        }

        private static void CreateBaseTemplate()
        {
            _baseTemplate = UnityEngine.Object.Instantiate<Transform>(R.Game.UI.VehicleWindow.ScheduleTab.VehicleWindowScheduleTabSeparatorView.transform.Find("AddStop"));
            _baseTemplate.name = "ScheduleIndicatorContainter";
            GameObject.DestroyImmediate(_baseTemplate.gameObject.GetComponent<ButtonEx>());
            GameObject.DestroyImmediate(_baseTemplate.gameObject.GetComponent<ClickableDecorator>());

            Transform tr = UnityEngine.GameObject.Instantiate<Transform>((new GameObject("TimeIndicator", typeof(RectTransform))).transform, _baseTemplate);
            tr.name = "TimeIndicator";
            HorizontalLayoutGroup group = _baseTemplate.gameObject.GetComponent<HorizontalLayoutGroup>();
            group.padding.top -= 3;
            group.padding.bottom -= 3;
            LayoutHelper.MakeLayoutGroup(tr, LayoutHelper.Orientation.Horizontal, new RectOffset(0, 0, 0, 0), 4, group.childAlignment, LayoutHelper.ChildSizing.ChildControlsSize);
            _baseTemplate.Find("Icon").SetParent(tr);
            _baseTemplate.Find("Text").SetParent(tr);
            _baseTemplate.SetActive(false);
        }

        protected bool TooltipTextForStation(IReadOnlyDictionary<Item, int> transfers, StringBuilder strBuilder, IReadOnlyDictionary<Item, int> routeTransfers, float monthMultiplier)
        {
            bool added = false;
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            foreach (KeyValuePair<Item, int> transfer in transfers)
            {
                string countStr = (Math.Abs(transfer.Value) * monthMultiplier).ToString("N0");
                if (routeTransfers != null && routeTransfers.TryGetValue(transfer.Key, out int routeCount))
                {
                    countStr += "/" + Math.Abs(routeCount).ToString();
                }
                if (transfer.Value > 0)
                {
                    strBuilder.AppendLine().Append(StringHelper.Colorify(StringHelper.Format(locale.GetString("schedule_stopwatch/loaded_items_count"), StringHelper.FormatCountString(transfer.Key.DisplayName, countStr)), Color.blue * 0.8f));
                    added = true;
                }
                else if (transfer.Value < 0)
                {
                    strBuilder.AppendLine().Append(StringHelper.Colorify(StringHelper.Format(locale.GetString("schedule_stopwatch/unloaded_items_count"), StringHelper.FormatCountString(transfer.Key.DisplayName, countStr)), Color.green * 0.9f));
                    added = true;
                }
            }

            return added;
        }

    }
}
