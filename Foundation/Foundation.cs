using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace FoundationBindings
{
    public static class Foundation
    {
        private static int _initialized = 0;

        public static void Initalize()
        {
            if (Interlocked.Exchange(ref _initialized, 1) == 0)
            {
                ObjectiveC.ObjectiveC.Initialize(Assembly.GetAssembly(typeof(Foundation)), new string [] { "Foundation" });
            }
        }
    }
}
