using System;

namespace ObjectiveC
{
    [Flags]
    public enum InstanceCreationFlags : uint
    {
        Invalid = 0,
        Alloc = 1 << 0,
        Init = 1 << 1,
        New = 1 << 2
    }
}
