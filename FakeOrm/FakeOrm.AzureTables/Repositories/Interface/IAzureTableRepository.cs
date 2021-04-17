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
        T GetByRowKey(Guid rowKey);

        Task<IList<T>> GetAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, IList<IncludePropertyCls<T>>>> include = null);

        Task<T> GetByRowKeyAsync(Guid rowKey);

        Task<T> CreateOrUpdateAsync(T entity);

        Task<IEnumerable<T>> CreateOrUpdateBatchAsync(IEnumerable<T> list);

        //Task<IEnumerable<T>> GetAsync(Expression<Func<T, IList<IncludePropertyCls<T>>>> expression = null);
    }
}
