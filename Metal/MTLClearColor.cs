using System.Runtime.InteropServices;

namespace MetalBindings
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MTLClearColor
    {
        public double Red;
        public double Green;
        public double Blue;
        public double Alpha;
    }
}
