using FluentAssertions;
using System;
using System.Text;
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
        factory.CanConvert(null!).Should().BeFalse();
        factory.CanConvert(typeof(AbstractStateBackedCounter)).Should().BeFalse();
        factory.CanConvert(typeof(DisposableCounter)).Should().BeFalse();
    }

    [Fact]
    public void Factory_ShouldThrow_WhenStateBackedTypeHasNoStateConstructor()
    {
        var factory = new StateJsonConverterFactory();

        Action act = () => factory.CreateConverter(typeof(MissingStateConstructorCounter), new JsonSerializerOptions());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*constructor accepting*CounterState*");
    }

    [Fact]
    public void Factory_ShouldThrow_WhenTypeHasMultipleStateContracts()
    {
        var factory = new StateJsonConverterFactory();

        Action act = () => factory.CreateConverter(typeof(MultiStateCounter), new JsonSerializerOptions());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*only one IStateBacked<TState>*");
    }

    [Fact]
    public void Factory_ShouldThrow_WhenCreateConverterReceivesNonStateBackedType()
    {
        var factory = new StateJsonConverterFactory();

        Action act = () => factory.CreateConverter(typeof(ConventionOnlyCounter), new JsonSerializerOptions());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must implement IStateBacked<TState>*");
    }

    [Fact]
    public void Converter_ShouldRejectNullFactory()
    {
        Action act = () => new StateJsonConverter<StateBackedCounter, CounterState>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    [Theory]
    [InlineData("[]", "Expected JSON object")]
    [InlineData("{}", "Expected 'State' property")]
    [InlineData("""{"Other":{"Count":1}}""", "Expected 'State' property")]
    [InlineData("""{"State":null}""", "Unable to deserialize 'State'")]
    [InlineData("""{"State":{"Count":1},"Extra":2}""", "Expected end of JSON object")]
    public void Converter_ShouldRejectMalformedStateJson(string json, string message)
    {
        var options = CreateOptions();

        Action act = () => JsonSerializer.Deserialize<StateBackedCounter>(json, options);

        act.Should().Throw<JsonException>()
            .WithMessage($"*{message}*");
    }

    [Theory]
    [InlineData("{", "Expected 'State' property")]
    [InlineData("""{"State":""", "Expected 'State' value")]
    [InlineData("""{"State":{"Count":1,"Name":"alpha"}""", "Expected end of JSON object")]
    public void Converter_ShouldRejectPartialReaderInput(string json, string message)
    {
        byte[] utf8Json = Encoding.UTF8.GetBytes(json);
        Action act = () => ReadPartialStateJson(utf8Json);

        act.Should().Throw<JsonException>()
            .WithMessage($"*{message}*");
    }

    [Fact]
    public void Converter_ShouldWriteNullRecordAsJsonNull()
    {
        var converter = new StateJsonConverter<StateBackedCounter, CounterState>(state => new StateBackedCounter(state));
        using var buffer = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(buffer);

        converter.Write(writer, null!, new JsonSerializerOptions());
        writer.Flush();

        System.Text.Encoding.UTF8.GetString(buffer.ToArray()).Should().Be("null");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StateJsonConverterFactory());
        return options;
    }

    private static void ReadPartialStateJson(byte[] utf8Json)
    {
        var reader = new Utf8JsonReader(utf8Json, isFinalBlock: false, state: default);
        Assert.True(reader.Read());

        var converter = new StateJsonConverter<StateBackedCounter, CounterState>(state => new StateBackedCounter(state));
        converter.Read(ref reader, typeof(StateBackedCounter), new JsonSerializerOptions());
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

    private abstract class AbstractStateBackedCounter : IStateBacked<CounterState>
    {
        public abstract CounterState State { get; }
    }

    private sealed class ConventionOnlyCounter
    {
        public ConventionOnlyCounter(CounterState state)
        {
            State = state;
        }

        public CounterState State { get; }
    }

    private sealed class DisposableCounter : IDisposable
    {
        public void Dispose()
        {
        }
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

    private sealed class MultiStateCounter : IStateBacked<CounterState>, IStateBacked<AlternateCounterState>
    {
        public MultiStateCounter(CounterState state)
        {
            State = state;
        }

        public CounterState State { get; }

        AlternateCounterState IStateBacked<AlternateCounterState>.State => new();
    }

    private sealed class CounterState
    {
        public int Count { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class AlternateCounterState
    {
        public int Value { get; set; }
    }
}
