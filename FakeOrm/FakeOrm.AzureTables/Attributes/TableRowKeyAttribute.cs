using System;

namespace FakeOrm.AzureTables.Attributes
{
    public class TableRowKeyAttribute : Attribute
    {
        public string RowKey { get; set; }

        public TableRowKeyAttribute(string Key)
        {
            RowKey = Key;
        }
    }
}
