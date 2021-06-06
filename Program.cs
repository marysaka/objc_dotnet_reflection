using System;
using MetalBindings;

namespace ObjectiveC
{
    class Program
    {
        static void Main(string[] args)
        {
            MTLRenderPassColorAttachmentDescriptor descriptor = ObjectiveC.CreateInstance<MTLRenderPassColorAttachmentDescriptor>();

            Console.WriteLine("Testing objective C");

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
    }
}
