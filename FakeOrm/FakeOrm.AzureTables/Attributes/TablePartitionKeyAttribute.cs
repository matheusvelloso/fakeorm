using System;

namespace FakeOrm.AzureTables.Attributes
{
    public class TablePartitionKeyAttribute : Attribute
    {
        public string PartitionKey { get; set; }

        public TablePartitionKeyAttribute(string Key)
        {
            PartitionKey = Key;
        }
    }
}
