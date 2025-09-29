using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Pharmacy.Core.Models;

namespace Pharmacy.Core.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        ValueTask<IReadOnlyList<T>> GetAllAsync();
        ValueTask<IEnumerable<T>> GetAllAsyncc();
        ValueTask<T> GetByIdAsync(Guid id);
        ValueTask<T> GetByIdAsync(string id);
        ValueTask<T> GetByIdAsync(int id);
        ValueTask<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes);

        IQueryable<T> GetAllQueryable(Expression<Func<T, bool>> predicate = null, Func<IQueryable<T>, IQueryable<T>> include = null);
        ValueTask<T> GetByIdWithIncludeAsync(Guid id, params Expression<Func<T, object>>[] includes);
        ValueTask AddAsync(T item);
        public ValueTask<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        ValueTask<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        ValueTask<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        void Delete(T item);
        void Update(T item);
        ValueTask<int> SaveChangesAsync();

        Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>> include = null);
        Task<IQueryable<T>> GetAllAsync(Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IQueryable<T>> include = null);
        ValueTask<PagedResult<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool descending = false, params Expression<Func<T, object>>[] includes);
        IQueryable<T> Query();

    }
}
