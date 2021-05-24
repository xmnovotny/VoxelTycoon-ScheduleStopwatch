using System;
using System.Collections.Generic;
using VoxelTycoon;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Tasks;

namespace ScheduleStopwatch
{
    public partial class VehicleScheduleCapacity
    {
        public class TransfersPerStationCont
        {
            private readonly Dictionary<VehicleStation, TaskTransfers> _transfPerSt;
            private readonly bool isReadonly = false;

            public TransfersPerStationCont() {
                _transfPerSt = new Dictionary<VehicleStation, TaskTransfers>();
            }
            private TransfersPerStationCont(TransfersPerStationCont asReadonly)
            {
                _transfPerSt = asReadonly._transfPerSt;
                isReadonly = true;
            }

            public TransfersPerStationCont(Dictionary<RootTask, TaskTransfers> transfersPerTask) : this()
            {
                foreach (KeyValuePair<RootTask, TaskTransfers> pair in transfersPerTask)
                {
                    Add(pair.Key.Destination.VehicleStationLocation.VehicleStation, pair.Value);    
                }
            }

            public TransfersPerStationCont AsReadonly()
            {
                return new TransfersPerStationCont(this);
            }

            public void Add(VehicleStation station, TaskTransfers transfers, float? multiplier = null)
            {
                if (isReadonly)
                {
                    throw new InvalidOperationException("TransfersPerStation is readonly");
                }
                if (_transfPerSt.TryGetValue(station, out TaskTransfers taskTransf))
                {
                    taskTransf.Add(transfers, multiplier);
                } else
                {
                    _transfPerSt.Add(station, new TaskTransfers(transfers, multiplier));
                }
            }

            public void Add(TransfersPerStationCont transfPerStation, float? mutliplier)
            {
                foreach (KeyValuePair<VehicleStation, TaskTransfers> pair in transfPerStation._transfPerSt)
                {
                    Add(pair.Key, pair.Value, mutliplier);
                }
            }

            public IReadOnlyDictionary<Item, int> this[VehicleStation idx]
            {
                get
                {
                    if (_transfPerSt.TryGetValue(idx, out TaskTransfers taskTransfers))
                    {
                        return taskTransfers.Transfers;
                    }
                    return null;
                }
            }

            public IEnumerator<KeyValuePair<VehicleStation, IReadOnlyDictionary<Item, int>>> GetEnumerator()
            {
                foreach (KeyValuePair<VehicleStation, TaskTransfers> taskTransfer in _transfPerSt)
                {
                    yield return new KeyValuePair<VehicleStation, IReadOnlyDictionary<Item, int>>(taskTransfer.Key, taskTransfer.Value.Transfers);
                }
                yield break;
            }

            public int Count
            {
                get
                {
                    return _transfPerSt.Count;
                }
            }
        }

    }
}
