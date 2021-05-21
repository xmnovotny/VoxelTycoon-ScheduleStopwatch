using HarmonyLib;
using System;
using VoxelTycoon.Modding;
using ModSettings;
using VoxelTycoon.Serialization;
using VoxelTycoon.Game.UI;
using System.Reflection;

namespace ScheduleStopwatch
{
    [SchemaVersion(1)]
    public class ScheduleStopwatch : Mod
    {
        public const int SAVE_VERSION = 1;
        private static Harmony harmony;
        private const string harmonyID = "cz.xmnovotny.schedulestopwatch.patch";

        protected override void Initialize()
        {
            harmony = (Harmony)(object)new Harmony(harmonyID);
            Harmony.DEBUG = false;
            FileLog.Reset();
            var interf = typeof(VehicleWindowScheduleTabRefitPropertyView).GetInterface("IFrameCloseHandler");
            var interfaceMapping = typeof(VehicleWindowScheduleTabRefitPropertyView).GetInterfaceMap(interf);
            foreach (MethodInfo info in AccessTools.GetDeclaredMethods(typeof(VehicleWindowScheduleTabRefitPropertyView)))
            {
                FileLog.Log(info.Name);
            }
            foreach (MethodInfo info in interfaceMapping.TargetMethods)
            {
                FileLog.Log(info.Name);
            }
            harmony.PatchAll();
            VehicleScheduleDataManager.Current.Initialize();
        }

        protected override void OnGameStarted()
        {
            ModSettingsWindowManager.Current.Register<SettingsWindowPage>(this.GetType().Name, "Automatic schedule stopwatch settings");
        }

        protected override void Deinitialize()
        {
            VehicleScheduleDataManager.Current.Deinitialize();
            ModSettingsWindowManager.Current.Unregister(this.GetType().Name);
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
