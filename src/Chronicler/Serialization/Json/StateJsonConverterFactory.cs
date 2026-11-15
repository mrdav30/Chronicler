//=======================================================================
// StateJsonConverterFactory.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicler.Serialization;

/// <summary>
/// Creates JSON converters for types that explicitly implement <see cref="IStateBacked{TState}"/>.
/// </summary>
public sealed class StateJsonConverterFactory : JsonConverterFactory
{
    /// <summary>Checks whether the type is a candidate for state-backed conversion.</summary>
    /// <param name="typeToConvert">The type to inspect. Null is ineligible.</param>
    /// <returns>True for a concrete class that implements a state-backed interface. Constructor and contract validation occur when creating the converter.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert != null
            && typeToConvert.IsClass
            && !typeToConvert.IsAbstract
            && HasStateBackedContract(typeToConvert);
    }

    /// <summary>Builds a state converter that invokes the record's state constructor.</summary>
    /// <param name="typeToConvert">A concrete class with exactly one state-backed interface and a public constructor accepting that interface's state type.</param>
    /// <param name="options">The calling serializer's configuration. Constructor selection depends on the declared record type.</param>
    /// <returns>A converter whose constructor delegate is bound to the record's public state constructor.</returns>
    /// <remarks>Discovers the interface and constructor via reflection and compiles a constructor delegate.</remarks>
    /// <exception cref="InvalidOperationException">The type has zero or multiple state-backed interfaces, or lacks the matching public constructor.</exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type stateType = GetSingleStateType(typeToConvert);

        ConstructorInfo constructor = typeToConvert.GetConstructor(new[] { stateType })
            ?? throw new InvalidOperationException(
                $"Type '{typeToConvert}' must define a public constructor accepting '{stateType}'.");

        Type converterType = typeof(StateJsonConverter<,>).MakeGenericType(typeToConvert, stateType);
        Delegate factory = CreateFactory(typeToConvert, stateType, constructor);

        return (JsonConverter)Activator.CreateInstance(converterType, factory)!;
    }

    private static bool HasStateBackedContract(Type type)
    {
        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (IsStateBackedInterface(interfaceType))
                return true;
        }

        return false;
    }

    private static Type GetSingleStateType(Type type)
    {
        Type? stateType = null;

        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (!IsStateBackedInterface(interfaceType))
                continue;

            Type currentStateType = interfaceType.GetGenericArguments()[0];
            if (stateType != null)
            {
                throw new InvalidOperationException(
                    $"Type '{type}' must implement only one IStateBacked<TState> contract.");
            }

            stateType = currentStateType;
        }

        return stateType
            ?? throw new InvalidOperationException(
                $"Type '{type}' must implement IStateBacked<TState>.");
    }

    private static bool IsStateBackedInterface(Type type)
    {
        return type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(IStateBacked<>);
    }

    private static Delegate CreateFactory(Type recordType, Type stateType, ConstructorInfo constructor)
    {
        ParameterExpression stateParameter = Expression.Parameter(stateType, "state");
        NewExpression createRecord = Expression.New(constructor, stateParameter);
        Type factoryType = typeof(Func<,>).MakeGenericType(stateType, recordType);

        return Expression.Lambda(factoryType, createRecord, stateParameter).Compile();
    }
}
