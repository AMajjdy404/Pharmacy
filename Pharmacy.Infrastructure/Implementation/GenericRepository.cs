using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Interfaces;
using Pharmacy.Core.Models;
using Pharmacy.Infrastructure.Data;

namespace Pharmacy.Infrastructure.Implementation
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly PharmacyDbContext _context;



        public GenericRepository(PharmacyDbContext context)
        {
            _context = context;
        }
        public async ValueTask<IReadOnlyList<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public async ValueTask<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);
            return await query.Where(predicate).ToListAsync();
        }
        public async Task<IEnumerable<T>> GetAllAsync(
       Expression<Func<T, bool>>? filter = null,
       Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
       params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();

            if (filter != null)
                query = query.Where(filter);

            if (includes != null)
            {
                foreach (var include in includes)
                    query = query.Include(include);
            }

            if (orderBy != null)
                query = orderBy(query);

            return await query.ToListAsync();
        }


        public IQueryable<T> GetAllQueryable(Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IQueryable<T>> include = null)
        {
            IQueryable<T> query = _context.Set<T>();
            if (include != null)
            {
                query = include(query);
            }
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            return query;
        }

        public async ValueTask<T> GetByIdAsync(Guid id)
         => await _context.Set<T>().FindAsync(id);
        public async ValueTask<T> GetByIdAsync(string id)
        => await _context.Set<T>().FindAsync(id);

        public async ValueTask<T> GetByIdAsync(int id)
        => await _context.Set<T>().FindAsync(id);


        public async ValueTask<T> GetByIdWithIncludeAsync(Guid id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);

            return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
        }

        public async ValueTask<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(predicate);
        }
        public async ValueTask<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.FirstOrDefaultAsync(predicate);
        }



        public async ValueTask<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().AnyAsync(predicate);
        }
        public async ValueTask AddAsync(T item)
        {
            Console.WriteLine($"[Repo] Adding {typeof(T).Name}");
            await _context.Set<T>().AddAsync(item);

            var state = _context.Entry(item).State;
            Console.WriteLine($"[Repo] State after AddAsync = {state}");
        }


        public void Delete(T item)
        => _context.Set<T>().Remove(item);

        public void Update(T item)
        => _context.Set<T>().Update(item);
        public async ValueTask<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

        public async Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>> include = null)
        {
            IQueryable<T> query = _context.Set<T>();
            if (include != null)
            {
                query = include(query);
            }
            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<IQueryable<T>> GetAllAsync(Expression<Func<T, bool>> predicate = null,
         Func<IQueryable<T>, IQueryable<T>> include = null)
        {
            IQueryable<T> query = _context.Set<T>();
            if (include != null)
            {
                query = include(query);
            }
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            return query; // لا حاجة لـ await هنا لأننا نرجع IQueryable
        }
        public async ValueTask<PagedResult<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool descending = false, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);
            query = query.Where(predicate);

            var totalItems = await query.CountAsync();

            query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            query = query.Skip((page - 1) * pageSize).Take(pageSize);

            var items = await query.ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                TotalItems = totalItems
            };
        }


        public IQueryable<T> Query()
        {
            return _context.Set<T>().AsQueryable();
        }

        public async ValueTask<IEnumerable<T>> GetAllAsyncc()
        {
            return await _context.Set<T>().ToListAsync();
        }
    }
}
