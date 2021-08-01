using System;
using System.Text;

namespace FoundationBindings
{
    [ObjectiveC.Class]
    public interface NSString
    {
        [ObjectiveC.Property("UTF8String")]
        UIntPtr UTF8String { get; }

        [ObjectiveC.Method("characterAtIndex")]
        ushort CharacterAtIndex(nuint index);

        unsafe string GetValue()
        {
            byte *rawString = (byte*)UTF8String;

            int count = 0;
            while (rawString[count] != 0)
            {
                count++;
            }

            return Encoding.UTF8.GetString(rawString, count);
        }
        // TODO
    }
}
