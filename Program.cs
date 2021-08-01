using System;
using System.Reflection;
using System.Text;
using FoundationBindings;

namespace ObjectiveC
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize
            // We are on the same assembly for now, hackaround here
            //Foundation.Initalize();
            ObjectiveC.Initialize(Assembly.GetAssembly(typeof(Program)),
                                  new string [] { "Foundation" });

            // Testing time!
            var testString = ObjectiveC.CreateInstance<NSString>(InstanceCreationFlags.Alloc);
            testString = testString.Initialize("nyahahahahahah!");

            Console.WriteLine(testString.Value);

            StringBuilder builder = new StringBuilder();

            builder.Append(testString.CharacterAtIndex(0));
            builder.Append(testString.CharacterAtIndex(1));
            builder.Append(testString.CharacterAtIndex(2));

            Console.WriteLine(builder.ToString());
        }
    }
}
