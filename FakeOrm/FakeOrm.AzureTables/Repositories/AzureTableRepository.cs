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

        public T GetByRowKey(string rowKey)
        {
            //Todo: melhorar implementacao
            return _table.CreateQuery<T>().Where(x => x.RowKey == rowKey).FirstOrDefault();
        }

        public async Task<T> GetByRowKeyAsync(string rowKey)
        {
            //Todo: melhorar implementacao
            return await Task.Run(() => GetByRowKey(rowKey));
        }

        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate = null, Expression<Func<T, IList<IncludePropertyCls<T>>>> include = null)
        {
            //Todo: melhorar implementacao

            return await Task.Run(() =>
            {
                if (predicate == null) predicate = x => true;

                var list = _table.CreateQuery<T>().Where(predicate).ToList();

                foreach (var item in list)
                    ValidateInclude(include, item);

                return list;
            });
        }

        public async Task<T> FirstAsync(Expression<Func<T, bool>> predicate = null, Expression<Func<T, IList<IncludePropertyCls<T>>>> include = null)
        {
            var list = await GetAsync(predicate, include);

            return list.First();
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate = null, Expression<Func<T, IList<IncludePropertyCls<T>>>> include = null)
        {
            var list = await GetAsync(predicate, include);

            return list.FirstOrDefault();
        }

        #region [ Private Methods ]

        private void ValidateRowPartitionKey(T entity)
        {
            entity.RowKey = GetRowKey(entity);

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
                    var returnValue = method.Invoke(o, new object[] { fkValue.ToString() });
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

        private string GetRowKey(T entity)
        {
            var propertyRowKey = RowKeyValidation(typeof(T));

            if (propertyRowKey != null)
            {
                foreach (var item in entity.GetType().GetProperties())
                {
                    if (item.Name != propertyRowKey) continue;

                    var partition = entity.GetType().GetProperty(propertyRowKey)?.GetValue(entity);
                    return partition.ToString();
                }
            }

            if (!String.IsNullOrEmpty(entity.RowKey))
                return entity.RowKey;

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

        public static string RowKeyValidation(Type entity)
        {
            foreach (var item in entity.CustomAttributes)
            {
                if (item.AttributeType.Name != nameof(TableRowKeyAttribute))
                    continue;

                foreach (var i in item.ConstructorArguments)
                    return i.Value.ToString();
            }
            return null;
        }
    }
}
