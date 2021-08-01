using System;

namespace ObjectiveC
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyAttribute : Attribute
    {
        public readonly string CustomReadName;
        public readonly string CustomWriteName;

        public PropertyAttribute(string customReadName = null, string customWriteName = null)
        {
            CustomReadName = customReadName;
            CustomWriteName = customWriteName;
        }
    }
}
