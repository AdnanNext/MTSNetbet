using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.DataAccess.Repositories
{
    public interface IRepository<T>
    {
        //List<T> GetAll();
        long Add(T entity);
        long BulkAdd(List<T> listEntity);
        long Update(T entity);
    }
    public interface IRepositoryAsync<T>
    {
        Task<List<T>> GetAll();
        Task<long> AddAsync(T entity);
        Task<long> BulkAddAsync(List<T> listEntity);
        Task<long> Update(T entity);
    }
}
