using System;
using System.Text;

using ObjectiveC;

namespace FoundationBindings
{
    [Class]
    public unsafe interface NSString
    {
        [Property("UTF8String")]
        UIntPtr RawUTF8String { get; }

        char CharacterAtIndex(nuint index);

        NSString InitWithCharacters(UIntPtr characters, nuint length);

        NSString Initialize(string str)
        {
            fixed (char *characters = str)
            {
                return InitWithCharacters((UIntPtr)characters, (nuint)str.Length);
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
