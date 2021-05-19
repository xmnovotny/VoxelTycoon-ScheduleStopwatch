using System;
using System.Collections.Generic;
using System.Text;
using Cyotek.Collections.Generic;
using VoxelTycoon.Serialization;

namespace ScheduleStopwatch
{
    public class DurationDataSet
    {
        public const int DEFAULT_BUFFER_SIZE = 10;
        private TimeSpan? _average;

        private TimeSpan _totalSum;
        private TimeSpan _runningSum;
        private int _totalCount;

        private CircularBuffer<TimeSpan> _durationData;
        public DurationDataSet(int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            _durationData = new CircularBuffer<TimeSpan>(bufferSize);
        }

        public void MarkDirty()
        {
            _average = null;
        }

        public void Clear()
        {
            _durationData.Clear();
            _totalSum = default;
            _totalCount = 0;
            _runningSum = default;
            MarkDirty();
        }

        public void Add(TimeSpan duration)
        {
            if (_durationData.IsFull)
            {
                _runningSum -= _durationData.Get();
            }
            _durationData.Put(duration);
            _totalCount++;
            _totalSum += duration;
            _runningSum += duration;
            MarkDirty();
        }

        public TimeSpan? Average
        {
            get
            {
                if (_average == null)
                {
                    if (_durationData.Size == 0)
                    {
                        return null;
                    }

                    /*                    TimeSpan resultSum = default;
                                        foreach (TimeSpan duration in _durationData)
                                        {
                                            resultSum += duration;
                                        }
                    */
                    _average = new TimeSpan(_runningSum.Ticks / _durationData.Size);
                }
                return _average;
            }
        }

        public TimeSpan? TotalAverage
        {
            get
            {
                return (_totalCount > 0) ? new TimeSpan?(new TimeSpan(_totalSum.Ticks / _totalCount)) : null;
            }
        }

        public void Write(StateBinaryWriter writer)
        {
            writer.WriteInt(_durationData.Size);
            foreach (var duration in _durationData)
            {
                writer.WriteLong(duration.Ticks);
            }
            writer.WriteInt(_totalCount);
            writer.WriteLong(_totalSum.Ticks);
        }

        internal static DurationDataSet Read(StateBinaryReader reader, byte version)
        {
            var result = new DurationDataSet();
            var count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                var span = new TimeSpan(reader.ReadLong());
                result._durationData.Put(span);
                result._runningSum += span;
            }

            result._totalCount = reader.ReadInt();
            result._totalSum = new TimeSpan(reader.ReadLong());

            return result;
        }

    }
}
