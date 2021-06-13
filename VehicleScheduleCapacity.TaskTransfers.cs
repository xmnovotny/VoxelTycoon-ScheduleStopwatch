using System.Collections.Generic;
using VoxelTycoon;

namespace ScheduleStopwatch
{
    public partial class VehicleScheduleCapacity
    {
        public class TaskTransfers
        {
            //transfer per Item ( >0 = loading, <0 = unloading)
            private readonly Dictionary<Item, int> _transfers = new Dictionary<Item, int>();

            public TaskTransfers() { }
            public TaskTransfers(TaskTransfers taskTransfers, float? multiplier = null)
            {
                Add(taskTransfers._transfers, multiplier);
            }

            public IReadOnlyDictionary<Item, int> Transfers
            {
                get
                {
                    return _transfers;
                }
            }

/*            public IReadOnlyList<Dictionary<Item, int>> TransfersPerUnit
            {
                get
                {
                    return _transfersPerUnit;
                }
            }*/

            public void Add(Item item, int count)
            {
                if (_transfers.ContainsKey(item))
                {
                    _transfers[item] += count;
                }
                else
                {
                    _transfers.Add(item, count);
                }
            }

            public void Add(TaskTransfers transfers, float? multiplier = null)
            {
                Add(transfers._transfers, multiplier);
            }

            public void Add(IReadOnlyDictionary<Item, int> transfers, float? multiplier=null)
            {
                foreach (KeyValuePair<Item, int> transfer in transfers)
                {
                    int value = multiplier != null ? (transfer.Value * multiplier.Value).RoundToInt() : transfer.Value;
                    this.Add(transfer.Key, value);
                }
            }
        }

    }
}
