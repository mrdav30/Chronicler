//=======================================================================
// DiagnosticSink.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;
/// <summary>
/// Receives diagnostic events emitted by a <see cref="DiagnosticChannel"/>.
/// </summary>
/// <param name="diagnostic">The emitted diagnostic event.</param>
public delegate void DiagnosticSink(in DiagnosticEvent diagnostic);
