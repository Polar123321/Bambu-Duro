using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ConsoleApp4.Data;
using ConsoleApp4.Repositories.Interfaces;

namespace ConsoleApp4.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly BotDbContext Db;

    public Repository(BotDbContext db)
    {
        Db = db;
    }

    public Task<T?> GetAsync(Expression<Func<T, bool>> predicate)
    {
        return Db.Set<T>().FirstOrDefaultAsync(predicate);
    }

    public Task AddAsync(T entity)
    {
        Db.Set<T>().Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return Db.SaveChangesAsync();
    }
}
