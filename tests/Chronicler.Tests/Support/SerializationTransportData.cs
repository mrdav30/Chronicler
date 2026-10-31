using System.Collections.Generic;

namespace Chronicler.Tests;

public static class SerializationTransportData
{
    public static IEnumerable<object[]> All
    {
        get
        {
            yield return new object[] { SerializationTransport.Json };
#if !CHRONICLER_DISABLE_MEMORYPACK
            yield return new object[] { SerializationTransport.MemoryPack };
#endif
        }
    }
}
