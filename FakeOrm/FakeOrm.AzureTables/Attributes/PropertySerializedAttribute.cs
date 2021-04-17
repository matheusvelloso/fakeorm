using System;

namespace FakeOrm.AzureTables.Attributes
{
    public class PropertySerializedAttribute : Attribute
    {
        public Type ConvertToType;

        public PropertySerializedAttribute()
        {

        }
        public PropertySerializedAttribute(Type convertToType)
        {
            ConvertToType = convertToType;
        }
    }
}
