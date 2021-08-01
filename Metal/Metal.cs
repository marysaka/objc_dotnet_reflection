using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace MetalBindings
{
    public static class Metal
    {
        private static int _initialized = 0;

        public static void Initalize()
        {
            if (Interlocked.Exchange(ref _initialized, 1) == 0)
            {
                ObjectiveC.ObjectiveC.Initialize(Assembly.GetAssembly(typeof(Metal)), new string [] { "Metal" });
            }
        }
    }
}
