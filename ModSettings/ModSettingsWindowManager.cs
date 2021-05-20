using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;

namespace ModSettings
{
    [Harmony]
    class ModSettingsWindowManager
    {
        private static ModSettingsWindowManager _current;
        private static readonly Dictionary<string, ModData> registered = new Dictionary<string, ModData>();

        private struct ModData
        {
            public string Title;
            public UnityAction Show;
        }

        public static ModSettingsWindowManager Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new ModSettingsWindowManager();
                }
                return _current;
            }
        }

        private void ShowWindow<T>(string Title) where T : ModSettingsWindowPage
        {
            ModSettingsWindow.ShowFor<T>(Title);
        }

        public void Register<T>(string ModClassName, string Title) where T : ModSettingsWindowPage
        {
            ModData data = default;
            data.Title = Title;
            data.Show = delegate () { ShowWindow<T>(Title); };

            registered.Add(ModClassName, data);
        }

        public void Unregister(string ModClassName)
        {
            registered.Remove(ModClassName);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSettingsWindowPacksPage), "AddToggle")]
        static private void AddToggle_pof(SettingsWindowDropdownItem __result, GameSettingsWindowPacksPage __instance, GameSettingsWindowPacksPageItem item)
        {
            if (__instance is GameSettingsWindowInGamePacksPage)
            {
                if (registered.TryGetValue(item.Pack.Name, out var data))
                {
                    Transform hint = __result.transform.Find("Row1/NameContainer/Hint");
                    Transform settings = UnityEngine.Object.Instantiate<Transform>(hint, hint.parent);
                    Text text = settings.GetComponent<Text>();
                    text.text = "";
                    text.font = R.Fonts.Ketizoloto;
                    text.color = Color.black;

                    Button button = settings.GetComponent<Button>();
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(data.Show);
                    settings.gameObject.SetActive(true);
                }
            }
        }

    }
}
