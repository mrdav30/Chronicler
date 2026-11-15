//=======================================================================
// InterpolatedStringHandlerArgumentAttribute.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if NETSTANDARD2_1
namespace System.Runtime.CompilerServices;

using System;

/// <summary>
/// Provides the compiler-recognized argument binding marker for target frameworks that do not define it publicly.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
{
    /// <summary>Binds one argument to a handler constructor.</summary>
    /// <param name="argument">The declared parameter name, or an empty string to bind the receiver.</param>
    public InterpolatedStringHandlerArgumentAttribute(string argument)
    {
        Arguments = new[] { argument };
    }

    /// <summary>Binds arguments to a handler constructor in order.</summary>
    /// <param name="arguments">The argument names in binding order. An empty name binds the receiver. The attribute retains the supplied array.</param>
    public InterpolatedStringHandlerArgumentAttribute(params string[] arguments)
    {
        Arguments = arguments;
    }

    /// <summary>Gets the names used to bind the handler constructor.</summary>
    /// <value>The underlying mutable array in binding order.</value>
    public string[] Arguments { get; }
}
#endif
