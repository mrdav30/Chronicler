//=======================================================================
// WarningDiagnosticLevel.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;
/// <summary>
/// Provides the warning diagnostic level for fixed-level interpolated logging.
/// </summary>
public readonly struct WarningDiagnosticLevel : IDiagnosticLevelProvider
{
    /// <inheritdoc />
    public DiagnosticLevel Level => DiagnosticLevel.Warning;
}
