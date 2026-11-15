//=======================================================================
// DiagnosticSink.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;
/// <summary>Receives a diagnostic synchronously from a channel.</summary>
/// <param name="diagnostic">A read-only reference to the event for the duration of the callback. Copy it to retain it.</param>
public delegate void DiagnosticSink(in DiagnosticEvent diagnostic);
