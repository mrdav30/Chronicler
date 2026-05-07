using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicler;

/// <summary>
/// Creates JSON converters for types that explicitly implement <see cref="IStateBacked{TState}"/>.
/// </summary>
public sealed class StateJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert != null
            && typeToConvert.IsClass
            && !typeToConvert.IsAbstract
            && HasStateBackedContract(typeToConvert);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type stateType = GetSingleStateType(typeToConvert);

        ConstructorInfo constructor = typeToConvert.GetConstructor(new[] { stateType })
            ?? throw new InvalidOperationException(
                $"Type '{typeToConvert}' must define a public constructor accepting '{stateType}'.");

        Type converterType = typeof(StateJsonConverter<,>).MakeGenericType(typeToConvert, stateType);
        Delegate factory = CreateFactory(typeToConvert, stateType, constructor);

        return (JsonConverter)(Activator.CreateInstance(converterType, factory)
            ?? throw new InvalidOperationException($"Failed to create converter instance for type '{converterType}'."));
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
