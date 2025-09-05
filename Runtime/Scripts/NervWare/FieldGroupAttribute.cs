using System;

namespace NervWareSDK
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FieldGroupAttribute : Attribute
    {
        public FieldGroupAttribute(string groupName)
        {
            GroupName = groupName;

        }

        public string GroupName { get; }
    }
}