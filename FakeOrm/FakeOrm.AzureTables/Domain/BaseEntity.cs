using FakeOrm.AzureTables.Utils;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;

namespace FakeOrm.AzureTables.Domain
{
    public class BaseEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string ETag { get; set; }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            EntityPropertyConvert.Deserialize(this, properties, operationContext);
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return EntityPropertyConvert.Serialize(this, operationContext);
        }
    }
}
