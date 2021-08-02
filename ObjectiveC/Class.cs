using System;

namespace ObjectiveC
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ClassAttribute : Attribute
    {
        public readonly string CustomName;

        public ClassAttribute(string customName = null)
        {
            CustomName = customName;
        }
    }
}
