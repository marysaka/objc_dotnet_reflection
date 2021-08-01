using System;
using System.Reflection;
using FoundationBindings;
using MetalBindings;

namespace ObjectiveC
{
    class Program
    {
        private static void TestMetal()
        {
            MTLRenderPassColorAttachmentDescriptor descriptor = ObjectiveC.CreateInstance<MTLRenderPassColorAttachmentDescriptor>(InstanceCreationFlags.New);

            Console.WriteLine("Testing Metal");

            MTLClearColor clearColor = new MTLClearColor
            {
                Red = 1.0,
                Green = 2.0,
                Blue = 3.0,
                Alpha = 4.0,
            };

            descriptor.ClearColor = clearColor; 

            Console.WriteLine($"descriptor.ClearColor.Red = {descriptor.ClearColor.Red}");
            Console.WriteLine($"descriptor.ClearColor.Green = {descriptor.ClearColor.Green}");
            Console.WriteLine($"descriptor.ClearColor.Blue = {descriptor.ClearColor.Blue}");
            Console.WriteLine($"descriptor.ClearColor.Alpha = {descriptor.ClearColor.Alpha}");
        }

        private static void TestFoundation()
        {
            NSString testString = ObjectiveC.CreateInstance<NSString>(InstanceCreationFlags.Alloc | InstanceCreationFlags.Init);

            Console.WriteLine(testString);
            Console.WriteLine(testString.GetValue());
        }

        static void Main(string[] args)
        {
            // We are on the same assembly, hackaround here
            //Foundation.Initalize();
            //Metal.Initalize();

            ObjectiveC.Initialize(Assembly.GetAssembly(typeof(Program)), new string [] { "Foundation", "Metal" });

            // Testing time!
            TestMetal();
            TestFoundation();
        }
    }
}
