using ModSettings;

namespace ScheduleStopwatch
{
    class Settings: ModSettings.ModSettings
    {
        private bool _showIndividualTaskTimes = true;
        private bool _showScheduleTotalTime = true;

        private static Settings _current;

        private Settings()
        {
        }

        public static Settings Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new Settings();
                }
                return _current;
            }
        }

        public bool ShowIndividualTaskTimes { 
            get => _showIndividualTaskTimes;
            set
            {
                if (_showIndividualTaskTimes != value)
                {
                    _showIndividualTaskTimes = value;
                    OnChange();
                }
            }
        }
        public bool ShowScheduleTotalTime
        {
            get => _showScheduleTotalTime; 
            set
            {
                if (_showScheduleTotalTime != value) 
                {
                    _showScheduleTotalTime = value;
                    OnChange();
                }
            }
        }

    }
}
