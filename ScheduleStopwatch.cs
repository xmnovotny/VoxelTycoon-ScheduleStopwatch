﻿using HarmonyLib;
using System;
using VoxelTycoon.Modding;
using ModSettings;
using VoxelTycoon.Serialization;
using VoxelTycoon;
using VoxelTycoon.Localization;

namespace ScheduleStopwatch
{
    [SchemaVersion(2)]
    public class ScheduleStopwatch : Mod
    {
        public const int SAVE_VERSION = 2;
        private Harmony harmony;
        private const string harmonyID = "cz.xmnovotny.schedulestopwatch.patch";

        protected override void Initialize()
        {
            Harmony.DEBUG = false;
            harmony = (Harmony)(object)new Harmony(harmonyID);
            FileLog.Reset();
            Manager<VehicleScheduleDataManager>.Initialize();
            harmony.PatchAll();
        }

        protected override void OnGameStarted()
        {
            ModSettingsWindowManager.Current.Register<SettingsWindowPage>(this.GetType().Name, LazyManager<LocaleManager>.Current.Locale.GetString("schedule_stopwatch/settings_window_title"));
            Manager<VehicleScheduleDataManager>.Current.StartMeasuring();
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
