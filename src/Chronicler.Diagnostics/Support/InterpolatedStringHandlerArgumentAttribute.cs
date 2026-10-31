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
    /// <summary>
    /// Initializes the attribute with one constructor argument name.
    /// </summary>
    /// <param name="argument">The method parameter name to pass to the interpolated string handler constructor.</param>
    public InterpolatedStringHandlerArgumentAttribute(string argument)
    {
        Arguments = new[] { argument };
    }

    /// <summary>
    /// Initializes the attribute with multiple constructor argument names.
    /// </summary>
    /// <param name="arguments">The method parameter names to pass to the interpolated string handler constructor.</param>
    public InterpolatedStringHandlerArgumentAttribute(params string[] arguments)
    {
        Arguments = arguments;
    }

    /// <summary>
    /// Gets the method parameter names passed to the interpolated string handler constructor.
    /// </summary>
    public string[] Arguments { get; }
}
#endif
