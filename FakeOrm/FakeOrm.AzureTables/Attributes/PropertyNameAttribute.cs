using System;
using System.Collections.Generic;
using System.Text;

namespace FakeOrm.AzureTables.Attributes
{
    public class PropertyNameAttribute : Attribute
    {
        public string PropertyName { get; set; }

        public PropertyNameAttribute(string name)
        {

            PropertyName = name;
        }
    }
}
