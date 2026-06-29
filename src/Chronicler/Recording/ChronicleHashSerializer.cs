using System;

namespace Chronicler;

/// <summary>
/// Computes deterministic record hashes by traversing <see cref="IRecordable.RecordData(IChronicler)"/>.
/// </summary>
public static class ChronicleHashSerializer
{
    /// <summary>
    /// Computes a deterministic hash for a recordable state graph using an empty context.
    /// </summary>
    public static ChronicleHash Compute(IRecordable target)
    {
        return Compute(target, new ChronicleContext());
    }

    /// <summary>
    /// Computes a deterministic hash for a recordable state graph using the supplied context.
    /// </summary>
    public static ChronicleHash Compute(IRecordable target, ChronicleContext context)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var writer = new ChronicleHashWriter();
        writer.WriteSection("chronicler.hash", 1);
        Contribute(target, context, ref writer);
        return writer.ToHash();
    }

    /// <summary>
    /// Contributes a recordable state graph to an existing caller-owned hash writer using an empty context.
    /// </summary>
    public static void Contribute(IRecordable target, ref ChronicleHashWriter writer)
    {
        Contribute(target, new ChronicleContext(), ref writer);
    }

    /// <summary>
    /// Contributes a recordable state graph to an existing caller-owned hash writer using the supplied context.
    /// </summary>
    public static void Contribute(IRecordable target, ChronicleContext context, ref ChronicleHashWriter writer)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        ChronicleHashChronicler.Contribute(target, context, ref writer);
    }
}
