
namespace XSockets.Utility.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A static generic repository for in-memory storage
    /// </summary>
    /// <typeparam name="TK">Key Type</typeparam>
    /// <typeparam name="T">Value Type</typeparam>
    public class Repository<TK, T>
    {
        private static object _locker = new object();
        private static ConcurrentDictionary<TK, T> Container { get; set; }

        static Repository()
        {
            Container = new ConcurrentDictionary<TK, T>();
        }

        public static T AddOrUpdate(TK key, T entity)
        {
            lock (_locker)
            {                
                if (!Container.ContainsKey(key))
                    Container.TryAdd(key, entity);
                else
                {
                    Container[key] = entity;
                }
            }
            return entity;
        }

        public static bool Remove(TK key)
        {
            lock (_locker)
            {
                T entity;
                return Container.TryRemove(key, out entity);
            }
        }

        public static int Remove(Func<T, bool> f)
        {
            lock (_locker)
            {
                return FindWithKeys(f).Count(o => Remove((TK) o.Key));
            }
        }

        public static void RemoveAll()
        {
            lock (_locker)
            {
                Container = new ConcurrentDictionary<TK, T>();
            }
        }

        public static IEnumerable<T> Find(Func<T, bool> f)
        {
            return Container.Values.Where(f);
        }

        public static IDictionary<TK, T> FindWithKeys(Func<T, bool> f)
        {
            var y = from x in Container
                    where f.Invoke(x.Value)
                    select x;
            return y.ToDictionary(x => x.Key, x => x.Value);
        }

        public static IDictionary<TK, T> GetAllWithKeys()
        {
            return Container;
        }

        public static IEnumerable<T> GetAll()
        {
            return Container.Values;
        }

        public static T GetById(TK key)
        {
            return Container.ContainsKey(key) ? Container[key] : default(T);
        }

        public static KeyValuePair<TK, T> GetByIdWithKey(TK key)
        {
            return Container.ContainsKey(key) ? new KeyValuePair<TK, T>(key, Container[key]) : new KeyValuePair<TK, T>(key, default(T));
        }

        public static bool ContainsKey(TK key)
        {
            return Container.ContainsKey(key);
        }
    }
}