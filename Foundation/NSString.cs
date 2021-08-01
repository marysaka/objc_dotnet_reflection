using System;
using System.Text;

namespace FoundationBindings
{
    [ObjectiveC.Class]
    public unsafe interface NSString
    {
        [ObjectiveC.Property("UTF8String")]
        UIntPtr RawUTF8String { get; }

        char CharacterAtIndex(nuint index);

        [ObjectiveC.Method("initWithCharacters:length")]
        UIntPtr InitWithCharacters(UIntPtr characters, nuint length);

        NSString Initialize(string str)
        {
            fixed (char *characters = str)
            {
                UIntPtr newInstance = InitWithCharacters((UIntPtr)characters, (nuint)str.Length);

                return ObjectiveC.ObjectiveC.CreateFromNativeInstance<NSString>(newInstance);
            }
        }

        string Value
        {
            get
            {
                byte *rawString = (byte*)RawUTF8String;

                int count = 0;

                while (rawString[count] != 0)
                {
                    count++;
                }

                return Encoding.UTF8.GetString(rawString, count);
            }
        }
    }
}
