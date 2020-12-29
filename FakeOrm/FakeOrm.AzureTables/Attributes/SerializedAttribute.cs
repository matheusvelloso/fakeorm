using System;
using System.Collections.Generic;
using System.Text;

namespace FakeOrm.AzureTables.Attributes
{
    public class SerializedAttribute : Attribute
    {
        public Type ConvertToType;

        public SerializedAttribute()
        {

        }
        public SerializedAttribute(Type convertToType)
        {
            ConvertToType = convertToType;
        }
    }
}
