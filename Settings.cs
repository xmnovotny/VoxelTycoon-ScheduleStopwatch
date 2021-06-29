using ModSettings;
using Newtonsoft.Json;

namespace ScheduleStopwatch
{
    [JsonObject(MemberSerialization.OptOut)]
    class Settings : ModSettings<Settings>
    {
        private bool _showIndividualTaskTimes = true;
        private bool _showScheduleTotalTime = true;
        private bool _showIndividualLoadingCapacity = true;
        private bool _showIndividualUnloadingCapacity = true;
        private bool _showTotalTransferCapacity = true;
        private bool _showStationLoadedItems = true;
        private bool _showStationUnloadedItems = true;
        private bool _calculateUnservicedDemands = false;

        public bool ShowIndividualTaskTimes { 
            get => _showIndividualTaskTimes;
            set =>  SetProperty<bool>(value, ref _showIndividualTaskTimes);
        }
        public bool ShowScheduleTotalTime
        {
            get => _showScheduleTotalTime; 
            set => SetProperty<bool>(value, ref _showScheduleTotalTime);
        }

        public bool ShowIndividualLoadingCapacity
        {
            get => _showIndividualLoadingCapacity;
            set => SetProperty<bool>(value, ref _showIndividualLoadingCapacity);
        }
        public bool ShowIndividualUnloadingCapacity
        {
            get => _showIndividualUnloadingCapacity;
            set => SetProperty<bool>(value, ref _showIndividualUnloadingCapacity);
        }
        public bool ShowTotalTransferCapacity
        {
            get => _showTotalTransferCapacity;
            set => SetProperty<bool>(value, ref _showTotalTransferCapacity);
        }
        public bool ShowStationLoadedItems
        {
            get => _showStationLoadedItems;
            set => SetProperty<bool>(value, ref _showStationLoadedItems);
        }
        public bool ShowStationUnloadedItems
        {
            get => _showStationUnloadedItems;
            set => SetProperty<bool>(value, ref _showStationUnloadedItems);
        }
        public bool CalculateUnservicedDemands
        {
            get => _calculateUnservicedDemands;
            set => SetProperty(value, ref _calculateUnservicedDemands);
        }
    }
}
