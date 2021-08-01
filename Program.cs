using System;
using System.Reflection;
using FoundationBindings;

namespace ObjectiveC
{
    class Program
    {
        static void Main(string[] args)
        {
            // We are on the same assembly for now, hackaround here
            //Foundation.Initalize();

            ObjectiveC.Initialize(Assembly.GetAssembly(typeof(Program)),
                                  new string [] { "Foundation" });

            // Testing time!
            NSString testString = ObjectiveC.CreateInstance<NSString>(InstanceCreationFlags.Alloc);
            testString = testString.Initialize("nyahahahahahah!");

            Console.WriteLine(testString);
            Console.WriteLine(testString.Value);
            Console.WriteLine(testString.CharacterAtIndex(0));
        }
    }
}
