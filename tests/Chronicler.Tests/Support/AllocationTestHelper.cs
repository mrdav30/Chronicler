using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Chronicler.Tests;

internal static class AllocationTestHelper
{
    public static long MeasureAfterWarmup(Action warmup, Action measured)
    {
        long allocated = -1;
        ExceptionDispatchInfo? capturedException = null;

        var thread = new Thread(() =>
        {
            try
            {
                warmup();
                long before = GC.GetAllocatedBytesForCurrentThread();
                measured();
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            }
            catch (Exception ex)
            {
                capturedException = ExceptionDispatchInfo.Capture(ex);
            }
        })
        {
            IsBackground = true
        };
        
        thread.Start();
        thread.Join();

        capturedException?.Throw();
        return allocated;
    }
}
