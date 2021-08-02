using System.Runtime.InteropServices;

namespace ObjectiveC
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Id<T>
    {
        public nuint NativePointer;

        public T ToInstance()
        {
            return ObjectiveC.CreateFromNativeInstance<T>(NativePointer);
        }
    }
}
