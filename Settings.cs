using ModSettings;
using VoxelTycoon;

namespace ScheduleStopwatch
{
    class Settings: ModSettings<Settings>
    {
        private bool _showIndividualTaskTimes = true;
        private bool _showScheduleTotalTime = true;

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
