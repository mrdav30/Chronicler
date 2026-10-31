//=======================================================================
// InterpolatedStringHandlerAttribute.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if NETSTANDARD2_1
namespace System.Runtime.CompilerServices;

using System;

/// <summary>
/// Provides the compiler-recognized interpolated string handler marker for target frameworks that do not define it publicly.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class InterpolatedStringHandlerAttribute : Attribute { }
#endif
