using FluentAssertions;
using System;
using System.Reflection;
using Xunit;

namespace Chronicler.Tests;

public sealed class ChronicleContextTests
{
    [Fact]
    public void QueueDeferredLink_ShouldRejectNullAssignmentCallback()
    {
        var context = new ChronicleContext();
        MethodInfo queueMethod = typeof(ChronicleContext)
            .GetMethod("QueueDeferredLink", BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(string));

        Action act = () => queueMethod.Invoke(
            context,
            new object?[] { "resource", "resource-id", null, null });

        act.Should().Throw<TargetInvocationException>()
            .Which.InnerException.Should().BeOfType<ArgumentNullException>()
            .Which.ParamName.Should().Be("assignLoadedValue");
    }
}
