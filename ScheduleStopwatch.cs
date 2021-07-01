using HarmonyLib;
using System;
using VoxelTycoon.Modding;
using ModSettings;
using VoxelTycoon.Serialization;
using VoxelTycoon;
using VoxelTycoon.Localization;
//using ICSharpCode.SharpZipLib.Checksum;

namespace ScheduleStopwatch
{
    [SchemaVersion(3)]
    public class ScheduleStopwatch : Mod
    {
        public const int SAVE_VERSION = 3;
        private Harmony harmony;
        private const string harmonyID = "cz.xmnovotny.schedulestopwatch.patch";
        private static int? _readVersion = null;
        public static Logger logger = new Logger("ScheduleStopwatch");

        public static int GetSchemaVersion(Type type)
        {
            if (_readVersion != null)
            {
                return _readVersion.Value; //legacy version before using SchemaVersion()
            }
            return SaveSerializer.Current.SchemaVersions.Get(type);
        }

        protected override void Initialize()
        {
            Harmony.DEBUG = false;
            harmony = (Harmony)(object)new Harmony(harmonyID);
            //FileLog.Reset();
            if (XMNUtils.ModFunctions.IsModInstalled("AdvancedTransferTask"))
            {
                Manager<AdvancedTransferTaskAdapter>.Initialize();
            }
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
            if (SaveSerializer.Current.SchemaVersions.Get<ScheduleStopwatch>() < 3)
            {
                writer.WriteByte(SAVE_VERSION);
            }
            VehicleScheduleDataManager.Current.Write(writer);
            LazyManager<StationDemandManager>.Current.Write(writer);
        }

        protected override void Read(StateBinaryReader reader)
        {
            _readVersion = null;
            try
            {
                int antVersion = SchemaVersion<ScheduleStopwatch>.Get();
                if (antVersion < 3)
                {
                    _readVersion = reader.ReadByte();
                }
                VehicleScheduleDataManager.Current.Read(reader);
                LazyManager<StationDemandManager>.Current.Read(reader);
            }
            catch (Exception e)
            {
                logger.Log(UnityEngine.LogType.Error, "Error when loading ScheduleStopwatch data");
                logger.LogException(e);
            }
            _readVersion = null;
        }
    }
}
