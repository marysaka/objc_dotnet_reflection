using System;

namespace ObjectiveC
{
    [Flags]
    public enum InstanceCreationFlags : uint
    {
        Invalid = 0,
        Alloc,
        Init,
        New
    }
}
