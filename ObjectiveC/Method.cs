using System;

namespace ObjectiveC
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodAttribute : Attribute
    {
        public readonly string CustomName;

        public MethodAttribute(string customName = null)
        {
            CustomName = customName;
        }
    }
}
