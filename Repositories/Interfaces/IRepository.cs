using System.Linq.Expressions;

namespace ConsoleApp4.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task SaveChangesAsync();
}
