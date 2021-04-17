using FakeOrm.AzureTables.Attributes;
using FakeOrm.AzureTables.Configurations;
using FakeOrm.AzureTables.Extensions;
using FakeOrm.AzureTables.Repositories.Interface;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace FakeOrm.AzureTables.Repositories
{
    public class AzureTableRepository<T> : BaseAzureTableRepository, IAzureTableRepository<T> where T : ITableEntity, new()
    {
        private readonly CloudTable _table;
        private readonly ConnectionStrings _connectionString;

        public AzureTableRepository(ConnectionStrings connectionString)
        {
            _connectionString = connectionString;
            var storageAccount = CloudStorageAccount.Parse(connectionString.AzureTableConnection);
            var cloudTableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());

            var tableName = TableNameValidation(typeof(T));

            tableName = tableName.Underscored();
            _table = cloudTableClient.GetTableReference(tableName);

            if (!_table.Exists() && !_table.CreateIfNotExists())
                throw new Exception($"Table '{tableName}' isn't created.");
        }

        public async Task<T> CreateOrUpdateAsync(T entity)
        {
            ValidateRowPartitionKey(entity);

            var operation = TableOperation.InsertOrReplace(entity);
            var result = await _table.ExecuteAsync(operation);

            return (T)result.Result;
        }

        public async Task<IEnumerable<T>> CreateOrUpdateBatchAsync(IEnumerable<T> list)
        {
            var groupList = list.Select((x, i) => new
            {
                Index = i,
                Value = x
            }).GroupBy(x => x.Index / 100).Select(x => x.Select(v => v.Value).ToList()).ToList();

            var listResult = new List<T>();

            foreach (var l in groupList)
            {
                var batchOperationObj = new TableBatchOperation();

                foreach (var item in l)
                {
                    ValidateRowPartitionKey(item);
                    batchOperationObj.InsertOrReplace(item);
                }

                var result = await _table.ExecuteBatchAsync(batchOperationObj);
                listResult.AddRange(result.Select(x => (T)x.Result));
            }

            return listResult;
        }

        public T GetByRowKey(Guid rowKey)
        {
            //Todo: melhorar implementacao
            return _table.CreateQuery<T>().Where(x => x.RowKey == rowKey.ToString()).FirstOrDefault();
        }

        public async Task<T> GetByRowKeyAsync(Guid rowKey)
        {
            //Todo: melhorar implementacao
            return await Task.Run(() => _table.CreateQuery<T>().Where(x => x.RowKey == rowKey.ToString()).FirstOrDefault());
        }

        public async Task<IList<T>> GetAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, IList<IncludePropertyCls<T>>>> include = null)
        {
            //Todo: melhorar implementacao

            return await Task.Run(() =>
            {

                var list = _table.CreateQuery<T>().Where(predicate).ToList();

                foreach (var item in list)
                {
                    ValidateInclude(include, item);
                }

                return list;
            });
        }

        #region [ Private Methods ]

        private void ValidateRowPartitionKey(T entity)
        {
            if (String.IsNullOrEmpty(entity.RowKey))
                entity.RowKey = Guid.NewGuid().ToString().ToLower();

            entity.PartitionKey = GetPartitionKey(entity);
        }

        private void ValidateInclude(Expression<Func<T, IList<IncludePropertyCls<T>>>> expression, T entity)
        {
            var listProperties = expression?.Compile().Invoke(entity);

            if (listProperties == null)
                return;

            foreach (var p in listProperties)
            {
                var property = entity.GetType().GetProperty(p.PropertyName);
                var fkAttr = (ForeignKeyAttribute)Attribute.GetCustomAttribute(property, typeof(ForeignKeyAttribute));
                if (fkAttr != null)
                {
                    var fkValue = entity.GetType().GetProperty(fkAttr.Name)?.GetValue(entity);

                    Type d1 = typeof(AzureTableRepository<>);
                    Type[] typeArgs = { property.PropertyType };
                    Type makeme = d1.MakeGenericType(typeArgs);
                    object o = Activator.CreateInstance(makeme, new object[] { _connectionString });

                    MethodInfo method = makeme.GetMethod(nameof(GetByRowKey));
                    var returnValue = method.Invoke(o, new object[] { fkValue });
                    property.SetValue(entity, returnValue);
                }
            }
        }

        private string GetPartitionKey(T entity)
        {
            var propertyPartitionKey = PartitionKeyValidation(typeof(T));

            if (propertyPartitionKey != null)
            {
                foreach (var item in entity.GetType().GetProperties())
                {
                    if (item.Name != propertyPartitionKey) continue;

                    var partition = entity.GetType().GetProperty(propertyPartitionKey)?.GetValue(entity);
                    return partition.ToString();
                }
            }

            if (!String.IsNullOrEmpty(entity.PartitionKey))
                return entity.PartitionKey;

            return Guid.NewGuid().ToString().ToLower();
        }

        #endregion
    }

    public abstract class BaseAzureTableRepository
    {
        protected string TableNameValidation(Type entity)
        {
            foreach (var item in entity.CustomAttributes)
            {
                if (item.AttributeType.Name != nameof(TableNameAttribute))
                    continue;

                foreach (var i in item.ConstructorArguments)
                    return i.Value.ToString();
            }

            return entity.Name;
        }

        public static string PartitionKeyValidation(Type entity)
        {
            foreach (var item in entity.CustomAttributes)
            {
                if (item.AttributeType.Name != nameof(TablePartitionKeyAttribute))
                    continue;

                foreach (var i in item.ConstructorArguments)
                    return i.Value.ToString();
            }
            return null;
        }
    }
}
