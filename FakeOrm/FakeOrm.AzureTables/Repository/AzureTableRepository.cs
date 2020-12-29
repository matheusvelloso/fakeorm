using FakeOrm.AzureTables.Attributes;
using FakeOrm.AzureTables.Configurations;
using FakeOrm.AzureTables.Extensions;
using FakeOrm.AzureTables.Repository.Interface;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace FakeOrm.AzureTables.Repository
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
            if (String.IsNullOrEmpty(entity.RowKey))
                entity.RowKey = Guid.NewGuid().ToString().ToLower();

            var key = PartitionKeyValidation(typeof(T));

            if (key != null)
            {
                foreach (var item in entity.GetType().GetProperties())
                {
                    if (item.Name != key) continue;

                    var partition = entity.GetType().GetProperty(key)?.GetValue(entity);
                    entity.PartitionKey = partition.ToString();
                }
            }
            else
                entity.PartitionKey = Guid.NewGuid().ToString().ToLower();

            var operation = TableOperation.InsertOrReplace(entity);
            var result = await _table.ExecuteAsync(operation);

            return (T)result.Result;
        }

        public T GetByRowKey(Guid rowKey)
        {
            //Todo: melhorar implementacao
            return _table.CreateQuery<T>().Where(x => x.RowKey == rowKey.ToString()).FirstOrDefault();
        }

        public async Task<T> GetByRowKeyAsync(Guid rowKey)
        {
            //Todo: melhorar implementacao
            return _table.CreateQuery<T>().Where(x => x.RowKey == rowKey.ToString()).FirstOrDefault();
        }

        public async Task<IList<T>> GetAsync(Expression<Func<T, IList<IncludePropertyCls<T>>>> include = null)
        {
            //Todo: melhorar implementacao
            var list = _table.CreateQuery<T>().Where(x => 1 == 1).ToList();

            foreach (var item in list)
            {
                ValidateInclude(include, item);
            }

            return list;
        }

        private void ValidateInclude(Expression<Func<T, IList<IncludePropertyCls<T>>>> expression, T entity)
        {
            var listProperties = expression.Compile().Invoke(entity);

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


            //var carro = new Carro();



            ////verificar se a propriedade possue o attr ForeignKey 
            ////obter o valor da property parametro do FK Attr
            //var marca = carro.Marca;
            //var pk = carro.MarcaId;
            ////
            //carro.Marca = new RepositoryAbstraction<Marca>().GetByRowKey(pk);
        }
    }

    public abstract class BaseAzureTableRepository
    {
        protected string TableNameValidation(Type entity)
        {

            foreach (var item in entity.CustomAttributes)
            {
                if (item.AttributeType.Name != nameof(TableNameAttribute)) continue;
                foreach (var i in item.ConstructorArguments)
                {

                    return i.Value.ToString();
                }
            }

            return entity.Name;
        }

        public static string PartitionKeyValidation(Type entity)
        {

            foreach (var item in entity.CustomAttributes)
            {
                if (item.AttributeType.Name != nameof(TablePartitionKeyAttribute)) continue;
                foreach (var i in item.ConstructorArguments)
                {

                    return i.Value.ToString();
                }
            }
            return null;
        }
    }
}
