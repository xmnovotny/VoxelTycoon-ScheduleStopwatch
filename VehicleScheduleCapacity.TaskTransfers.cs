using System.Collections.Generic;
using VoxelTycoon;

namespace ScheduleStopwatch
{
    public partial class VehicleScheduleCapacity
    {
        public enum TransferDirection { both, loading, unloading };
        public struct TransferData
        {
            public int load, unload;
            public int Total
            {
                get
                {
                    return load - unload;
                }
            }

            public int Get(TransferDirection direction)
            {
                switch (direction)
                {
                    case TransferDirection.loading:
                        return load;
                    case TransferDirection.unloading:
                        return unload;
                    case TransferDirection.both:
                        return Total;
                }
                return 0;
            }
        }

        public class TaskTransfers
        {
            //transfer per Item ( >0 = loading, <0 = unloading)
            private readonly Dictionary<Item, TransferData> _transfers = new Dictionary<Item, TransferData>();

            public TaskTransfers() { }
            public TaskTransfers(TaskTransfers taskTransfers, float? multiplier = null)
            {
                Add(taskTransfers._transfers, multiplier);
            }

            public IReadOnlyDictionary<Item, TransferData> Transfers
            {
                get
                {
                    return _transfers;
                }
            }

            public void Add(Item item, int count)
            {
                if (count > 0)
                {
                    Add(item, count, 0);
                } else
                {
                    Add(item, 0, -count);
                }
            }

            public void Add(Item item, int load, int unload)
            {
                if (!_transfers.TryGetValue(item, out TransferData data))
                {
                    data = default;
                }
                data.load += load;
                data.unload += unload;
                _transfers[item] = data;
            }

            public void Add(TaskTransfers transfers, float? multiplier = null)
            {
                Add(transfers._transfers, multiplier);
            }

            public void Add(IReadOnlyDictionary<Item, TransferData> transfers, float? multiplier=null)
            {
                foreach (KeyValuePair<Item, TransferData> transfer in transfers)
                {
                    int load = multiplier != null ? (transfer.Value.load * multiplier.Value).RoundToInt() : transfer.Value.load;
                    int unload = multiplier != null ? (transfer.Value.unload * multiplier.Value).RoundToInt() : transfer.Value.unload;
                    this.Add(transfer.Key, load, unload);
                }
            }
        }

    }
}
