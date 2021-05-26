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
        private bool _toOverride;

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

        /** marks task data for overwrite with next new data (all old data will be discarded when new data are added) */
        public void MarkForOverwrite()
        {
            _toOverride = true;
        }

        public void Add(TimeSpan duration)
        {
            if (_toOverride)
            {
                Clear();
            } else
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
            foreach (TimeSpan duration in _durationData)
            {
                writer.WriteLong(duration.Ticks);
            }
            writer.WriteInt(_totalCount);
            writer.WriteLong(_totalSum.Ticks);
        }

        internal static DurationDataSet Read(StateBinaryReader reader, byte version)
        {
            DurationDataSet result = new DurationDataSet();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                TimeSpan span = new TimeSpan(reader.ReadLong());
                result._durationData.Put(span);
                result._runningSum += span;
            }

            result._totalCount = reader.ReadInt();
            result._totalSum = new TimeSpan(reader.ReadLong());

            return result;
        }

    }
}
