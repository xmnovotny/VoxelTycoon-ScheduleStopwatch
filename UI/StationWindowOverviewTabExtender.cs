using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Localization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;
using VoxelTycoon.UI;

namespace ScheduleStopwatch.UI
{
    [Harmony]
    public class StationWindowOverviewTabExtender : MonoBehaviour
    {

        public StationWindowOverviewTab OverviewTab { get; private set; }
        public StationWindow StationWindow { get; private set; }

        public void Initialize(StationWindow window)
        {
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            StationWindow = window;
            OverviewTab = gameObject.GetComponent<StationWindowOverviewTab>();
            if (OverviewTab == null)
            {
                throw new NullReferenceException("Component StationWindowOverviewTab not found");
            }
            Transform content = base.transform.Find("Body/WindowScrollView").GetComponent<ScrollRect>().content;
            _unloadedContainer = CreateContainer(content, locale.GetString("schedule_stopwatch/monthly_unloaded_items").ToUpper());
            _unloadedContainer.SetSiblingIndex(2);
            _unloadedItemsContainer = _unloadedContainer.Find("Content");
            _unloadedContainerTitle = _unloadedContainer.Find("Label").gameObject.GetComponent<Text>();
            _loadedContainer = CreateContainer(content,locale.GetString("schedule_stopwatch/monthly_loaded_items").ToUpper());
            _loadedContainer.SetSiblingIndex(3);
            _loadedItemsContainer = _loadedContainer.Find("Content");
            _loadedContainerTitle = _loadedContainer.Find("Label").gameObject.GetComponent<Text>();
            this._offset = Time.unscaledTime;
        }

        protected void Update()
        {
            if (!this.StationWindow.Location.IsDead)
            {
                if (TimeHelper.OncePerUnscaledTime(1f, this._offset))
                {
                    this.Invalidate();
                }
            }
        }

        private void OnSelect()
        {
            this._offset = Time.unscaledTime;
            Invalidate();
        }

        private void Invalidate()
        {
            Settings settings = Settings.Current;
            if (settings.ShowStationLoadedItems || settings.ShowStationUnloadedItems)
            {
                ImmutableList<Vehicle> vehicles = LazyManager<VehicleStationLocationManager>.Current.GetServicedVehicles(StationWindow.Location);
                IReadOnlyDictionary<Item, int> transfers = Manager<VehicleScheduleDataManager>.Current.GetStationTaskTransfersSum(vehicles, StationWindow.Location.VehicleStation, out bool incomplete);
                FillContainerWithItems(_loadedContainer, _loadedItemsContainer, settings.ShowStationLoadedItems ? transfers : null, Direction.load);
                FillContainerWithItems(_unloadedContainer, _unloadedItemsContainer, settings.ShowStationUnloadedItems ? transfers : null, Direction.unload);
                if (_lastIncomplete != incomplete)
                {
                    _lastIncomplete = incomplete;
                    Locale locale = LazyManager<LocaleManager>.Current.Locale;
                    _loadedContainerTitle.text = locale.GetString("schedule_stopwatch/monthly_loaded_items").ToUpper() + (incomplete ? " (" + locale.GetString("schedule_stopwatch/partial").ToUpper() + ")" : "");
                    _unloadedContainerTitle.text = locale.GetString("schedule_stopwatch/monthly_unloaded_items").ToUpper() + (incomplete ? " (" + locale.GetString("schedule_stopwatch/partial").ToUpper() + ")" : "");
                }
            } else
            {
                _loadedContainer.SetActive(false);
                _unloadedContainer.SetActive(false);
            }
        }

        private void FillContainerWithItems(Transform container, Transform itemContainer, IReadOnlyDictionary<Item, int> transfers, Direction direction)
        {
            int count = 0;
            if (transfers == null)
            {
                container.gameObject.SetActive(false);
                return;
            }
            List<ResourceView> resourceViews = new List<ResourceView>();
            itemContainer.transform.GetComponentsInChildren<ResourceView>(resourceViews);
            foreach (KeyValuePair<Item, int> pair in transfers)
            {
                if ((pair.Value>0 && direction == Direction.load) || (pair.Value<0 && direction == Direction.unload))
                {
                    this.GetResourceView(resourceViews, count, itemContainer).ShowItem(pair.Key, null, Math.Abs(pair.Value));
                    count++;
                }
            }
            container.gameObject.SetActive(count > 0);
            while (count < resourceViews.Count)
            {
                resourceViews[count].gameObject.SetActive(false);
                count++;
            }
        }

        private ResourceView GetResourceView(List<ResourceView> resourceViews, int i, Transform parent)
        {
            if (i < resourceViews.Count)
            {
                return resourceViews[i];
            }
            return UnityEngine.Object.Instantiate<ResourceView>(R.Game.UI.ResourceView, parent);
        }

        private void CreateTemplate()
        {
            _template = UnityEngine.Object.Instantiate<Transform>(R.Game.UI.StationWindow.StationWindowOverviewTab.transform.Find("Body/WindowScrollView").GetComponent<ScrollRect>().content.Find("Sources"));
            _template.gameObject.SetActive(false);
        }

        private Transform CreateContainer(Transform parent, string title)
        {
            if (!_template)
            {
                CreateTemplate();
            }
            Transform result = GameObject.Instantiate<Transform>(_template, parent);
            result.Find("Label").gameObject.GetComponent<Text>().text = title;
            return result;
        }

        private void OnSettingsChanged()
        {
            Invalidate();
        }

        protected void OnEnable()
        {
            Settings.Current.Subscribe(OnSettingsChanged);
        }

        protected void OnDisable()
        {
            Settings.Current.Unsubscribe(OnSettingsChanged);
        }

        private Transform _loadedContainer, _loadedItemsContainer;
        private Transform _unloadedContainer, _unloadedItemsContainer;
        private Text _loadedContainerTitle, _unloadedContainerTitle;
        private Transform _template;
        private float _offset;
        private bool _lastIncomplete = false;
        private enum Direction {unload, load};

        #region HARMONY
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationWindowOverviewTab), "Initialize")]
        private static void VehicleWindowScheduleTab_Initialize_prf(VehicleWindowScheduleTab __instance, StationWindow window)
        {
            StationWindowOverviewTabExtender tabExt = __instance.gameObject.AddComponent<StationWindowOverviewTabExtender>();
            tabExt.Initialize(window);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationWindowOverviewTab), "OnSelect")]
        private static void StationWindowOverviewTab_OnSelect_pof(StationWindowOverviewTab __instance)
        {
            StationWindowOverviewTabExtender tabExt = __instance.gameObject.GetComponent<StationWindowOverviewTabExtender>();
            if (tabExt != null)
            {
                tabExt.OnSelect();
            }
        }
        #endregion
    }
}
