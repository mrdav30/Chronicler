using System;
using System.Collections.Generic;
using Chronicler.Diagnostics;
using Xunit;

namespace Chronicler.Diagnostics.Tests;

public class ThrowHelperTests
{
    [Fact]
    public void IndexAndKeyGuards_PreserveBoundariesAndExceptionDetails()
    {
        ThrowHelper.ThrowIfArrayIndexInvalid(0, 0);
        ThrowHelper.ThrowIfArrayIndexInvalid(3, 3);
        foreach (int index in new[] { -1, 4 })
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThrowHelper.ThrowIfArrayIndexInvalid(index, 3));
            Assert.Equal("index", exception.ParamName);
            Assert.Equal(index, exception.ActualValue);
        }

        ThrowHelper.ThrowIfListIndexInvalid(0, 1);
        foreach (int index in new[] { -1, 1 })
            Assert.Equal($"Index out of range: {index}", Assert.Throws<IndexOutOfRangeException>(() =>
                ThrowHelper.ThrowIfListIndexInvalid(index, 1)).Message);

        ThrowHelper.ThrowIfKeyInvalid(0, "present");
        Assert.Equal("Key was not found.", Assert.Throws<KeyNotFoundException>(() =>
            ThrowHelper.ThrowIfKeyInvalid(-1)).Message);
        Assert.Equal("Key not found: missing", Assert.Throws<KeyNotFoundException>(() =>
            ThrowHelper.ThrowIfKeyInvalid(-1, "missing")).Message);
    }

    [Fact]
    public void StandardGuards_AcceptValidInputsAndHonorCustomMessages()
    {
        ThrowHelper.ThrowIfNull(new object());
        ThrowHelper.ThrowIfNegative(0);
        ThrowHelper.ThrowIfNegativeOrZero(1);
        ThrowHelper.ThrowIfArgument(false);
        ThrowHelper.ThrowIfArgumentOutOfRange(false, 0);
        ThrowHelper.ThrowIfTrue(false);
        ThrowHelper.ThrowIfDisposed(false);
        ThrowHelper.ThrowIfKeyNotFound(false);
        Assert.Equal("custom", Assert.Throws<InvalidOperationException>(() =>
            ThrowHelper.ThrowIfTrue(true, message: "custom")).Message);
        Assert.Equal("Pool", Assert.Throws<ObjectDisposedException>(() =>
            ThrowHelper.ThrowIfDisposed(true, "Pool")).ObjectName);
        Assert.Equal("Key not found: missing", Assert.Throws<KeyNotFoundException>(() =>
            ThrowHelper.ThrowIfKeyNotFound(true, "missing")).Message);
    }

    [Fact]
    public void NamedRangeGuards_PreserveParameterValueAndDefaultMessage()
    {
        var nonnegative = Assert.Throws<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfNegative(-1, "count"));
        var positive = Assert.Throws<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfNegativeOrZero(0, "capacity"));
        var range = Assert.Throws<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfArgumentOutOfRange(true, 5, "index"));
        Assert.Equal(new ArgumentOutOfRangeException("count", -1, "count must be non-negative.").Message, nonnegative.Message);
        Assert.Equal(new ArgumentOutOfRangeException("capacity", 0, "capacity must be greater than zero.").Message, positive.Message);
        Assert.Equal(new ArgumentOutOfRangeException("index", 5, "index is out of range.").Message, range.Message);
    }

    [Fact]
    public void ThrowIfNull_WithoutExplicitParamName_UsesCallerArgumentExpression()
    {
        object value = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            ThrowHelper.ThrowIfNull(value));

        Assert.Equal(nameof(value), exception.ParamName);
    }

    [Fact]
    public void ThrowIfNullGeneric_PreservesReferenceAndNullableValueContracts()
    {
        string reference = null;
        int? nullableValue = null;

        Assert.Throws<ArgumentNullException>(() => ThrowHelper.ThrowIfNullGeneric(reference));
        Assert.Throws<ArgumentNullException>(() => ThrowHelper.ThrowIfNullGeneric(nullableValue));
        ThrowHelper.ThrowIfNullGeneric(0);
    }

    [Fact]
    public void ThrowIfNullAndNullsAreIllegal_OnlyThrowsForNonNullableDefaults()
    {
        ThrowHelper.ThrowIfNullAndNullsAreIllegal("value", default(int));
        ThrowHelper.ThrowIfNullAndNullsAreIllegal(null, default(string));
        ThrowHelper.ThrowIfNullAndNullsAreIllegal(null, default(int?));

        object value = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            ThrowHelper.ThrowIfNullAndNullsAreIllegal(value, default(int)));

        Assert.Equal(nameof(value), exception.ParamName);
    }

    [Fact]
    public void ThrowIfNegative_WithoutParamName_UsesGenericDefaultMessage()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ThrowHelper.ThrowIfNegative(-1));

        Assert.Contains("Value must be non-negative.", exception.Message);
        Assert.False(exception.Message.StartsWith(" must be non-negative.", StringComparison.Ordinal));
    }

    [Fact]
    public void StandardOverloads_PreserveDefaultAndCustomExceptionContracts()
    {
        ArgumentNullException nullException = Assert.Throws<ArgumentNullException>(() =>
            ThrowHelper.ThrowIfNull(null, null));
        Assert.Null(nullException.ParamName);
        Assert.Equal(
            new ArgumentNullException(null, "Value cannot be null.").Message,
            nullException.Message);

        ArgumentOutOfRangeException positiveException = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ThrowHelper.ThrowIfNegativeOrZero(0, null));
        Assert.Null(positiveException.ParamName);
        Assert.Equal(0, positiveException.ActualValue);
        Assert.Equal(
            new ArgumentOutOfRangeException(null, 0, "Value must be greater than zero.").Message,
            positiveException.Message);

        ArgumentOutOfRangeException rangeException = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ThrowHelper.ThrowIfArgumentOutOfRange(true, 5, null, null));
        Assert.Null(rangeException.ParamName);
        Assert.Equal(5, rangeException.ActualValue);
        Assert.Equal(
            new ArgumentOutOfRangeException(null, 5, "Specified argument was out of range.").Message,
            rangeException.Message);

        ArgumentException unnamedArgumentException = Assert.Throws<ArgumentException>(() =>
            ThrowHelper.ThrowIfArgument(true));
        Assert.Null(unnamedArgumentException.ParamName);
        Assert.Equal(
            new ArgumentException("The argument is invalid.", (string)null).Message,
            unnamedArgumentException.Message);

        ArgumentException namedArgumentException = Assert.Throws<ArgumentException>(() =>
            ThrowHelper.ThrowIfArgument(true, "value"));
        Assert.Equal("value", namedArgumentException.ParamName);
        Assert.Equal(
            new ArgumentException("value is invalid.", "value").Message,
            namedArgumentException.Message);

        InvalidOperationException unnamedStateException = Assert.Throws<InvalidOperationException>(() =>
            ThrowHelper.ThrowIfTrue(true));
        Assert.Equal("Operation is not valid in the current state.", unnamedStateException.Message);

        InvalidOperationException namedStateException = Assert.Throws<InvalidOperationException>(() =>
            ThrowHelper.ThrowIfTrue(true, "Pool"));
        Assert.Equal("Object 'Pool' is in an invalid state.", namedStateException.Message);

        ObjectDisposedException unnamedDisposedException = Assert.Throws<ObjectDisposedException>(() =>
            ThrowHelper.ThrowIfDisposed(true));
        Assert.Equal(string.Empty, unnamedDisposedException.ObjectName);
        Assert.Equal(
            new ObjectDisposedException(null, "Object has been disposed.").Message,
            unnamedDisposedException.Message);

        ObjectDisposedException customDisposedException = Assert.Throws<ObjectDisposedException>(() =>
            ThrowHelper.ThrowIfDisposed(true, "Pool", "Custom disposed message."));
        Assert.Equal("Pool", customDisposedException.ObjectName);
        Assert.Equal(
            new ObjectDisposedException("Pool", "Custom disposed message.").Message,
            customDisposedException.Message);

        KeyNotFoundException unnamedKeyException = Assert.Throws<KeyNotFoundException>(() =>
            ThrowHelper.ThrowIfKeyNotFound(true));
        Assert.Equal("Key was not found.", unnamedKeyException.Message);

        KeyNotFoundException customKeyException = Assert.Throws<KeyNotFoundException>(() =>
            ThrowHelper.ThrowIfKeyNotFound(true, "key", "Custom key message."));
        Assert.Equal("Custom key message.", customKeyException.Message);
    }

    [Fact]
    public void InterpolatedOverloads_WhenFalse_SkipFormatting()
    {
        int evaluations = 0;

        string SideEffect()
        {
            evaluations++;
            return "evaluated";
        }

        ThrowHelper.ThrowIfArgumentOutOfRange(false, 5, $"Hidden {SideEffect()}.");
        ThrowHelper.ThrowIfArgumentOutOfRange(false, 5, "value", $"Hidden {SideEffect()}.");
        ThrowHelper.ThrowIfArgument(false, $"Hidden {SideEffect()}.");
        ThrowHelper.ThrowIfArgument(false, "value", $"Hidden {SideEffect()}.");
        ThrowHelper.ThrowIfTrue(false, $"Hidden {SideEffect()}.");
        ThrowHelper.ThrowIfTrue(false, "Pool", $"Hidden {SideEffect()}.");
        ThrowHelper.ThrowIfDisposed(false, $"Hidden {SideEffect()}.");
        ThrowHelper.ThrowIfDisposed(false, "Pool", $"Hidden {SideEffect()}.");
        ThrowHelper.ThrowIfKeyNotFound(false, $"Hidden {SideEffect()}.");
        ThrowHelper.ThrowIfKeyNotFound(false, "key", $"Hidden {SideEffect()}.");

        Assert.Equal(0, evaluations);
    }

    [Fact]
    public void InterpolatedOverloads_WhenTrue_PreserveExceptionContractsAndFormatting()
    {
        ArgumentOutOfRangeException unnamedRangeException = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ThrowHelper.ThrowIfArgumentOutOfRange(true, 5, $"Out of range: {5}."));
        Assert.Null(unnamedRangeException.ParamName);
        Assert.Equal(5, unnamedRangeException.ActualValue);
        Assert.Equal(
            new ArgumentOutOfRangeException(null, 5, "Out of range: 5.").Message,
            unnamedRangeException.Message);

        ArgumentOutOfRangeException namedRangeException = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ThrowHelper.ThrowIfArgumentOutOfRange(true, 6, "value", $"Out of range: {6}."));
        Assert.Equal("value", namedRangeException.ParamName);
        Assert.Equal(6, namedRangeException.ActualValue);
        Assert.Equal(
            new ArgumentOutOfRangeException("value", 6, "Out of range: 6.").Message,
            namedRangeException.Message);

        ArgumentException unnamedArgumentException = Assert.Throws<ArgumentException>(() =>
            ThrowHelper.ThrowIfArgument(true, $"Invalid value: {7}."));
        Assert.Null(unnamedArgumentException.ParamName);
        Assert.Equal(
            new ArgumentException("Invalid value: 7.", (string)null).Message,
            unnamedArgumentException.Message);

        string text = "ok";
        const string formattedMessage = "g 0F|a   7|b F   |s ok|sa   ok|sb ok  |p go|pa   hi|pb z  ";
        ArgumentException namedArgumentException = Assert.Throws<ArgumentException>(() =>
            ThrowHelper.ThrowIfArgument(
                true,
                "value",
                $"g {15:X2}|a {7,3}|b {15,-4:X}|s {text}|sa {text,4}|sb {text,-4:ignored}|p {"go".AsSpan()}|pa {"hi".AsSpan(),4}|pb {"z".AsSpan(),-3:ignored}"));
        Assert.Equal("value", namedArgumentException.ParamName);
        Assert.Equal(
            new ArgumentException(formattedMessage, "value").Message,
            namedArgumentException.Message);

        InvalidOperationException unnamedStateException = Assert.Throws<InvalidOperationException>(() =>
            ThrowHelper.ThrowIfTrue(true, $"Invalid state: {1}."));
        Assert.Equal("Invalid state: 1.", unnamedStateException.Message);

        InvalidOperationException namedStateException = Assert.Throws<InvalidOperationException>(() =>
            ThrowHelper.ThrowIfTrue(true, "Pool", $"Invalid state: {2}."));
        Assert.Equal("Invalid state: 2.", namedStateException.Message);

        ObjectDisposedException unnamedDisposedException = Assert.Throws<ObjectDisposedException>(() =>
            ThrowHelper.ThrowIfDisposed(true, $"Disposed: {1}."));
        Assert.Equal(string.Empty, unnamedDisposedException.ObjectName);
        Assert.Equal(
            new ObjectDisposedException(null, "Disposed: 1.").Message,
            unnamedDisposedException.Message);

        ObjectDisposedException namedDisposedException = Assert.Throws<ObjectDisposedException>(() =>
            ThrowHelper.ThrowIfDisposed(true, "Pool", $"Disposed: {2}."));
        Assert.Equal("Pool", namedDisposedException.ObjectName);
        Assert.Equal(
            new ObjectDisposedException("Pool", "Disposed: 2.").Message,
            namedDisposedException.Message);

        KeyNotFoundException unnamedKeyException = Assert.Throws<KeyNotFoundException>(() =>
            ThrowHelper.ThrowIfKeyNotFound(true, $"Missing key: {1}."));
        Assert.Equal("Missing key: 1.", unnamedKeyException.Message);

        KeyNotFoundException namedKeyException = Assert.Throws<KeyNotFoundException>(() =>
            ThrowHelper.ThrowIfKeyNotFound(true, "key", $"Missing key: {2}."));
        Assert.Equal("Missing key: 2.", namedKeyException.Message);
    }

    [Fact]
    public void ThrowInterpolatedStringHandler_IsEnabledReflectsCondition()
    {
        var enabledHandler = new ThrowInterpolatedStringHandler(0, 0, true, out bool enabled);
        var disabledHandler = new ThrowInterpolatedStringHandler(0, 0, false, out bool disabled);

        Assert.True(enabled);
        Assert.True(enabledHandler.IsEnabled);
        Assert.False(disabled);
        Assert.False(disabledHandler.IsEnabled);
    }
}
