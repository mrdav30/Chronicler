//=======================================================================
// IDiagnosticLevelProvider.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;
/// <summary>
/// Describes a fixed diagnostic level for an interpolated diagnostic message handler.
/// </summary>
public interface IDiagnosticLevelProvider
{
    /// <summary>
    /// Gets the diagnostic level for this provider.
    /// </summary>
    DiagnosticLevel Level { get; }
}
