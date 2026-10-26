using FluentAssertions;
using System;
using System.Reflection;
using Xunit;

namespace Chronicler.Tests;

public class ChronicleLinkRegistryTests
{
    [Fact]
    public void RegisterResolver_ShouldThrow_WhenResolverIsNull()
    {
        var registry = new ChronicleLinkRegistry();

        Action act = () => registry.RegisterResolver<string>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("resolver");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RegisterInstance_ShouldThrow_WhenIdIsBlank(string? id)
    {
        var registry = new ChronicleLinkRegistry();

        Action act = () => registry.RegisterInstance(id!, 1);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("id");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UnregisterInstance_ShouldReturnFalse_WhenIdIsBlank(string? id)
    {
        var registry = new ChronicleLinkRegistry();

        registry.UnregisterInstance<int>(id!).Should().BeFalse();
    }

    [Fact]
    public void UnregisterInstance_ShouldReturnFalse_WhenNoTableExists()
    {
        var registry = new ChronicleLinkRegistry();

        registry.UnregisterInstance<int>("missing").Should().BeFalse();
    }

    [Fact]
    public void UnregisterInstance_ShouldReturnTrue_WhenEntryExists_AndFalseAfterRemoval()
    {
        var registry = new ChronicleLinkRegistry();

        registry.RegisterInstance("first", 1);
        registry.RegisterInstance("second", 2);

        registry.UnregisterInstance<int>("first").Should().BeTrue();
        registry.UnregisterInstance<int>("first").Should().BeFalse();
        registry.TryResolve("second", out int remaining).Should().BeTrue();
        remaining.Should().Be(2);
    }

    [Fact]
    public void TryGetReferenceId_ShouldUseValueEquality_ForValueTypes()
    {
        var registry = new ChronicleLinkRegistry();
        registry.RegisterInstance("one", 1);

        registry.TryGetReferenceId(1, out string? id).Should().BeTrue();
        id.Should().Be("one");
    }

    [Fact]
    public void TryGetReferenceId_ShouldReturnFalse_WhenRegisteredValueTypeDoesNotMatch()
    {
        var registry = new ChronicleLinkRegistry();
        registry.RegisterInstance("one", 1);

        registry.TryGetReferenceId(2, out string? id).Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public void TryResolve_ShouldReturnFalse_WhenEntryDoesNotExist()
    {
        var registry = new ChronicleLinkRegistry();

        registry.TryResolve("missing", out int value).Should().BeFalse();
        value.Should().Be(default);
    }

    [Fact]
    public void ChronicleLinkKey_EqualsObject_ShouldHandleMatchingAndNonMatchingObjects()
    {
        Type keyType = typeof(ChronicleLinkRegistry).GetNestedType(
            "ChronicleLinkKey",
            BindingFlags.NonPublic)!;

        object first = Activator.CreateInstance(
            keyType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { typeof(int), null },
            culture: null)!;

        object second = Activator.CreateInstance(
            keyType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { typeof(int), null },
            culture: null)!;

        MethodInfo equalsMethod = keyType.GetMethod("Equals", new[] { typeof(object) })!;

        ((bool)equalsMethod.Invoke(first, new[] { second })!).Should().BeTrue();
        ((bool)equalsMethod.Invoke(first, new object?[] { "not-a-key" })!).Should().BeFalse();
    }

    [Fact]
    public void ChronicleLinkKey_ShouldRejectNullType()
    {
        Type keyType = typeof(ChronicleLinkRegistry).GetNestedType(
            "ChronicleLinkKey",
            BindingFlags.NonPublic)!;

        Action act = () => Activator.CreateInstance(
            keyType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { null, null },
            culture: null);

        act.Should().Throw<TargetInvocationException>()
            .Which.InnerException.Should().BeOfType<ArgumentNullException>()
            .Which.ParamName.Should().Be("type");
    }

    [Fact]
    public void ChronicleLinkKey_EqualsTyped_ShouldReturnFalseForDifferentKeys()
    {
        Type keyType = typeof(ChronicleLinkRegistry).GetNestedType(
            "ChronicleLinkKey",
            BindingFlags.NonPublic)!;

        object first = Activator.CreateInstance(
            keyType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { typeof(int), null },
            culture: null)!;

        object differentType = Activator.CreateInstance(
            keyType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { typeof(string), null },
            culture: null)!;

        object differentSlot = Activator.CreateInstance(
            keyType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { typeof(int), "alternate" },
            culture: null)!;

        MethodInfo equalsMethod = keyType.GetMethod("Equals", new[] { keyType })!;

        ((bool)equalsMethod.Invoke(first, new[] { differentType })!).Should().BeFalse();
        ((bool)equalsMethod.Invoke(first, new[] { differentSlot })!).Should().BeFalse();
    }
}
