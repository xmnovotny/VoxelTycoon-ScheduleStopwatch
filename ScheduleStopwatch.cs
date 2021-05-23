using HarmonyLib;
using System;
using VoxelTycoon.Modding;
using ModSettings;
using VoxelTycoon.Serialization;
using VoxelTycoon.Game.UI;
using System.Reflection;
using VoxelTycoon;
using VoxelTycoon.Localization;

namespace ScheduleStopwatch
{
    [SchemaVersion(1)]
    public class ScheduleStopwatch : Mod
    {
        public const int SAVE_VERSION = 1;
        private Harmony harmony;
        private const string harmonyID = "cz.xmnovotny.schedulestopwatch.patch";

        protected override void Initialize()
        {
            harmony = (Harmony)(object)new Harmony(harmonyID);
            Harmony.DEBUG = false;
            FileLog.Reset();
            Manager<VehicleScheduleDataManager>.Initialize();
            harmony.PatchAll();
        }

        protected override void OnGameStarted()
        {
            ModSettingsWindowManager.Current.Register<SettingsWindowPage>(this.GetType().Name, LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/settings_window_title"));
        }

        protected override void Deinitialize()
        {
            harmony.UnpatchAll(harmonyID);
            harmony = null;
        }

        protected override void Write(StateBinaryWriter writer)
        {
            writer.WriteByte(SAVE_VERSION);
            VehicleScheduleDataManager.Current.Write(writer);
        }

        protected override void Read(StateBinaryReader reader)
        {
            try
            {
                byte version = reader.ReadByte();
                VehicleScheduleDataManager.Current.Read(reader, version);
            }
            catch (Exception e)
            {
                FileLog.Log(e.Message);
                FileLog.Log(e.StackTrace.ToString());
            }
        }
    }
}
