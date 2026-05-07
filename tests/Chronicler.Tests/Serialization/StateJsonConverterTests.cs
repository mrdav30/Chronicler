using FluentAssertions;
using System;
using System.Text.Json;
using Xunit;

namespace Chronicler.Tests;

public class StateJsonConverterTests
{
    [Fact]
    public void Factory_ShouldRoundTripStateBackedTypeThroughState()
    {
        var options = CreateOptions();
        var source = new StateBackedCounter(42, "alpha");

        string json = JsonSerializer.Serialize(source, options);
        StateBackedCounter? target = JsonSerializer.Deserialize<StateBackedCounter>(json, options);

        json.Should().Be("""{"State":{"Count":42,"Name":"alpha"}}""");
        target.Should().NotBeNull();
        target!.Count.Should().Be(42);
        target.Name.Should().Be("alpha");
    }

    [Fact]
    public void Factory_ShouldIgnoreTypesThatOnlyFollowStateNamingConvention()
    {
        var factory = new StateJsonConverterFactory();

        factory.CanConvert(typeof(ConventionOnlyCounter)).Should().BeFalse();
    }

    [Fact]
    public void Factory_ShouldThrow_WhenStateBackedTypeHasNoStateConstructor()
    {
        var factory = new StateJsonConverterFactory();

        Action act = () => factory.CreateConverter(typeof(MissingStateConstructorCounter), new JsonSerializerOptions());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*constructor accepting*CounterState*");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StateJsonConverterFactory());
        return options;
    }

    private sealed class StateBackedCounter : IStateBacked<CounterState>
    {
        public StateBackedCounter(int count, string name)
        {
            Count = count;
            Name = name;
        }

        public StateBackedCounter(CounterState state)
        {
            Count = state.Count;
            Name = state.Name;
        }

        public int Count { get; }

        public string Name { get; }

        public CounterState State => new()
        {
            Count = Count,
            Name = Name
        };
    }

    private sealed class ConventionOnlyCounter
    {
        public ConventionOnlyCounter(CounterState state)
        {
            State = state;
        }

        public CounterState State { get; }
    }

    private sealed class MissingStateConstructorCounter : IStateBacked<CounterState>
    {
        public MissingStateConstructorCounter(int count)
        {
            Count = count;
        }

        public int Count { get; }

        public CounterState State => new()
        {
            Count = Count
        };
    }

    private sealed class CounterState
    {
        public int Count { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
