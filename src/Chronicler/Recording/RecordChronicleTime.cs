using System;
using Chronicler.Timing;

namespace Chronicler;

/// <summary>Records exact time values through a versioned, transport-neutral component schema.</summary>
public static class RecordChronicleTime
{
    /// <summary>Reads or writes a signed duration, assigning only after validation succeeds.</summary>
    /// <exception cref="InvalidOperationException">The required value schema is missing or unsupported.</exception>
    public static void Look(IChronicler chronicler, ref ChronicleDuration value, string name)
    {
        var record = new TimeRecord(value.WholeSeconds, value.FractionalSecond);
        RecordDeepStruct.Look(chronicler, ref record, name);
        record.ValidateSchema();
        value = new ChronicleDuration(record.WholeSeconds, record.FractionalSecond);
    }

    /// <summary>Reads or writes a timestamp, assigning only after validation succeeds.</summary>
    /// <exception cref="InvalidOperationException">The required value schema is missing or unsupported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The recorded timestamp precedes its origin.</exception>
    public static void Look(IChronicler chronicler, ref ChronicleTimestamp value, string name)
    {
        var record = new TimeRecord(value.WholeSeconds, value.FractionalSecond);
        RecordDeepStruct.Look(chronicler, ref record, name);
        record.ValidateSchema();
        value = new ChronicleTimestamp(record.WholeSeconds, record.FractionalSecond);
    }

    // Both transports first populate a struct against an empty reader. Validate
    // only after the actual deep read returns, not during that initialization.
    private struct TimeRecord : IRecordable
    {
        private int _schemaVersion;
        public long WholeSeconds;
        public uint FractionalSecond;

        public TimeRecord(long wholeSeconds, uint fractionalSecond)
        {
            _schemaVersion = 1;
            WholeSeconds = wholeSeconds;
            FractionalSecond = fractionalSecond;
        }

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref _schemaVersion, "SchemaVersion", 0);
            RecordValues.Look(chronicler, ref WholeSeconds, nameof(WholeSeconds), 0L);
            RecordValues.Look(chronicler, ref FractionalSecond, nameof(FractionalSecond), 0u);
        }

        public readonly void ValidateSchema()
        {
            if (_schemaVersion != 1)
                throw new InvalidOperationException("The time value schema is missing or unsupported.");
        }
    }
}
