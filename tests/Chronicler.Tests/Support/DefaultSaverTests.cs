using System.Collections.Generic;
using Xunit;

namespace Chronicler.Tests;

public sealed class DefaultSaverTests
{
    [Fact]
    public void LifecycleMethods_InvokeMatchingProtectedHooksInOrder()
    {
        var saver = new RecordingSaver();

        saver.Save();
        saver.EarlyApply();
        saver.Apply();
        saver.LateApply();

        Assert.Equal(
            new[] { "save", "early-apply", "apply", "late-apply" },
            saver.Calls);
    }

    private sealed class RecordingSaver : DefaultSaver
    {
        public readonly List<string> Calls = new List<string>();

        protected override void OnSave() => Calls.Add("save");

        protected override void OnEarlyApply() => Calls.Add("early-apply");

        protected override void OnApply() => Calls.Add("apply");

        protected override void OnLateApply() => Calls.Add("late-apply");
    }
}
