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
            private readonly Dictionary<VehicleStationLocation, TaskTransfers> _transfPerSt;
            private readonly bool isReadonly = false;

            public TransfersPerStationCont() {
                _transfPerSt = new Dictionary<VehicleStationLocation, TaskTransfers>();
            }
            private TransfersPerStationCont(TransfersPerStationCont asReadonly)
            {
                _transfPerSt = asReadonly._transfPerSt;
                isReadonly = true;
            }

            public TransfersPerStationCont(Dictionary<RootTask, TaskTransfers> transfersPerTask) : this()
            {
                if (transfersPerTask != null)
                {
                    foreach (KeyValuePair<RootTask, TaskTransfers> pair in transfersPerTask)
                    {
                        Add(pair.Key.Destination.VehicleStationLocation, pair.Value);
                    }
                }
            }

            public TransfersPerStationCont AsReadonly()
            {
                return new TransfersPerStationCont(this);
            }

            public void Add(VehicleStationLocation station, TaskTransfers transfers, float? multiplier = null, bool estimated = false)
            {
                if (isReadonly)
                {
                    throw new InvalidOperationException("TransfersPerStation is readonly");
                }
                if (_transfPerSt.TryGetValue(station, out TaskTransfers taskTransf))
                {
                    taskTransf.Add(transfers, multiplier, estimated);
                } else
                {
                    _transfPerSt.Add(station, new TaskTransfers(transfers, multiplier, estimated));
                }
            }

            public void Add(TransfersPerStationCont transfPerStation, float? mutliplier, bool estimated = false)
            {
                foreach (KeyValuePair<VehicleStationLocation, TaskTransfers> pair in transfPerStation._transfPerSt)
                {
                    Add(pair.Key, pair.Value, mutliplier, estimated);
                }
            }

            public IReadOnlyDictionary<Item, TransferData> this[VehicleStationLocation idx]
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

            public IEnumerator<KeyValuePair<VehicleStationLocation, IReadOnlyDictionary<Item, TransferData>>> GetEnumerator()
            {
                foreach (KeyValuePair<VehicleStationLocation, TaskTransfers> taskTransfer in _transfPerSt)
                {
                    yield return new KeyValuePair<VehicleStationLocation, IReadOnlyDictionary<Item, TransferData>>(taskTransfer.Key, taskTransfer.Value.Transfers);
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
