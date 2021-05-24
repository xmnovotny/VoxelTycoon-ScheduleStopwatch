using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
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
        }

    }
}
