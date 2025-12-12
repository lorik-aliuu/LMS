using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.ServiceInterfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;

        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

        Task RemoveAsync(string key);

        Task<bool> ExistsAsync(string key);

        Task<long> IncrementAsync(string key, TimeSpan? expiration = null);

        Task RemoveByPatternAsync(string pattern);
        Task<long> GetCounterAsync(string key);


    }
}
