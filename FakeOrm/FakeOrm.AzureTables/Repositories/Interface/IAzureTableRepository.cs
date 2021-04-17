using FakeOrm.AzureTables.Extensions;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FakeOrm.AzureTables.Repositories.Interface
{
    public interface IAzureTableRepository<T> where T : ITableEntity
    {
        Task<T> CreateOrUpdateAsync(T entity);
        
        Task<IEnumerable<T>> CreateOrUpdateBatchAsync(IEnumerable<T> list);
        
        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, IList<IncludePropertyCls<T>>>> include = null);
        
        T GetByRowKey(Guid rowKey);

        Task<T> GetByRowKeyAsync(Guid rowKey);

        IEnumerable<T> GetAll();

        Task<IEnumerable<T>> GetAllAsync();
    }
}
