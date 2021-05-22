using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon.Game.UI;
using VoxelTycoon.UI;
using VoxelTycoon.UI.Windows;

namespace ModSettings
{
    class ModSettingsWindow: RichWindow
    {
        public static ModSettingsWindow ShowFor<T>(string Title) where T : ModSettingsWindowPage
        {
            ModSettingsWindow modSettingsWindow = UIManager.Current.CreateFrame<ModSettingsWindow>(FrameAnchoring.Center);
            modSettingsWindow.Initialize<T>(Title);
            modSettingsWindow.Show();
            return modSettingsWindow;
        }

        protected override void InitializeFrame() {
            base.InitializeFrame();
            base.Draggable = false;
            base.Priority = 1001;
            this.Width = new float?((float)500);
        }

        protected override void Prepare()
        {
            base.Prepare();
            Overlay.ShowFor(this, false);
        }

        private T CreateContent<T>() where T : ModSettingsWindowPage
        {
            T t = new GameObject("ModSettingsTab").AddComponent<T>();
            t.gameObject.AddComponent<VerticalLayoutGroup>();
            t.transform.SetParent(base.ContentContainer);
            return t;
        }

        private void Initialize<T>(string Title) where T: ModSettingsWindowPage
        {
            base.Title = Title;
            CreateContent<T>().Initialize();
        }
    }
}
