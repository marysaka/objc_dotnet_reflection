using System;
using System.Text;

namespace FoundationBindings
{
    [ObjectiveC.Class]
    public interface NSString
    {
        [ObjectiveC.Property("UTF8String")]
        UIntPtr UTF8String { get; }

        char CharacterAtIndex(nuint index);
        [ObjectiveC.Method("initWithCharacters:length")]
        unsafe UIntPtr InitWithCharacters(char *characters, nuint length);

        NSString Initialize(string str)
        {
            unsafe
            {
                fixed (char *characters = str)
                {
                    return ObjectiveC.ObjectiveC.CreateFromNativeInstance<NSString>(InitWithCharacters(characters, (nuint)str.Length));
                }
            }
        }

        string Value
        {
            get
            {
                unsafe
                {
                    byte *rawString = (byte*)UTF8String;

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
}
