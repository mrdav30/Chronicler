//=======================================================================
// ChronicleHashSerializer.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace Chronicler.Hashing;

/// <summary>
/// Computes deterministic record hashes by traversing <see cref="IRecordable.RecordData(IChronicler)"/>.
/// </summary>
/// <remarks>
/// The hash includes the recorded types, field names, defaults, values, and stable link IDs in schema order.
/// Supported leaf types are Boolean, fixed-width integers up to 64 bits, characters, strings, and enums.
/// </remarks>
public static class ChronicleHashSerializer
{
    /// <summary>Computes a record hash with an empty link registry.</summary>
    /// <param name="target">The graph whose schema is traversed in saving mode and in recording-call order.</param>
    /// <returns>The version-one hash of the declared state.</returns>
    /// <exception cref="ArgumentNullException">The target is null.</exception>
    /// <exception cref="InvalidOperationException">A non-null link needs a stable ID. Use the overload with a populated context.</exception>
    /// <exception cref="NotSupportedException">The schema records a leaf type unsupported by the hash backend.</exception>
    public static ChronicleHash Compute(IRecordable target)
    {
        return Compute(target, new ChronicleContext());
    }

    /// <summary>Computes a deterministic hash of the declared state.</summary>
    /// <param name="target">The graph whose schema is traversed in saving mode and in recording-call order.</param>
    /// <param name="context">The registry context used to look up stable IDs for links.</param>
    /// <returns>The hash of the version-one record stream, including its section header.</returns>
    /// <exception cref="ArgumentNullException">The target or context is null.</exception>
    /// <exception cref="InvalidOperationException">A non-null link has no stable ID in the context.</exception>
    /// <exception cref="NotSupportedException">The schema records a leaf type unsupported by the hash backend.</exception>
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

    /// <summary>Appends a record hash contribution with an empty link registry.</summary>
    /// <param name="target">The graph whose schema is traversed in saving mode and in recording-call order.</param>
    /// <param name="writer">The caller's running writer. Appends the record on success and preserves the writer if recording fails.</param>
    /// <exception cref="ArgumentNullException">The target is null.</exception>
    /// <exception cref="InvalidOperationException">A non-null link needs a stable ID. Use the overload with a populated context.</exception>
    /// <exception cref="NotSupportedException">The schema records a leaf type unsupported by the hash backend.</exception>
    public static void Contribute(IRecordable target, ref ChronicleHashWriter writer)
    {
        Contribute(target, new ChronicleContext(), ref writer);
    }

    /// <summary>Appends a record to a running hash.</summary>
    /// <param name="target">The graph whose schema is traversed in saving mode and in recording-call order.</param>
    /// <param name="context">The registry context used to look up stable IDs for links.</param>
    /// <param name="writer">The caller's running writer. Appends the record on success and preserves the writer if recording fails.</param>
    /// <remarks>Use the same contribution order in every caller that needs matching hashes.</remarks>
    /// <exception cref="ArgumentNullException">The target or context is null.</exception>
    /// <exception cref="InvalidOperationException">A non-null link has no stable ID in the context.</exception>
    /// <exception cref="NotSupportedException">The schema records a leaf type unsupported by the hash backend.</exception>
    public static void Contribute(IRecordable target, ChronicleContext context, ref ChronicleHashWriter writer)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        ChronicleHashChronicler.Contribute(target, context, ref writer);
    }
}
