//=======================================================================
// DynamicDiagnosticLevel.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;
/// <summary>
/// Provides a placeholder level for dynamic-level interpolated logging.
/// </summary>
public readonly struct DynamicDiagnosticLevel : IDiagnosticLevelProvider
{
    /// <summary>Gets the placeholder used when no dynamic level is supplied.</summary>
    /// <value><see cref="DiagnosticLevel.None"/>. Pass an explicit level to enable dynamic logging.</value>
    public DiagnosticLevel Level => DiagnosticLevel.None;
}
