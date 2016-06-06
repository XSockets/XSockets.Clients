using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace XSockets.Utility.Storage
{
    /// <summary>
    /// A non-static generic repository for in-memory storage
    /// </summary>
    /// <typeparam name="TK">Key Type</typeparam>
    /// <typeparam name="T">Value Type</typeparam>
    public class RepositoryInstance<TK, T>
    {
        private ConcurrentDictionary<TK, T> Container { get; set; }

        public RepositoryInstance()
        {
            this.Container = new ConcurrentDictionary<TK, T>();
        }

        public T AddOrUpdate(TK key, T entity)
        {
            return this.Container.AddOrUpdate(key, entity, (k, arg2) => entity);
        }

        public bool Remove(TK key)
        {
            T entity;
            return Container.TryRemove(key, out entity);
        }

        public int Remove(Func<T, bool> f)
        {
            return FindWithKeys(f).Count(o => Remove((TK) o.Key));
        }

        public void RemoveAll()
        {
            Container = new ConcurrentDictionary<TK, T>();
        }

        public IEnumerable<T> Find(Func<T, bool> f)
        {
            return Container.Values.Where(f);
        }

        public IDictionary<TK, T> FindWithKeys(Func<T, bool> f)
        {
            var y = from x in Container
                where f.Invoke(x.Value)
                select x;
            return y.ToDictionary(x => x.Key, x => x.Value);
        }

        public IDictionary<TK, T> GetAllWithKeys()
        {
            return Container;
        }

        public IEnumerable<T> GetAll()
        {
            return Container.Values;
        }

        public T GetById(TK key)
        {
            return Container.ContainsKey(key) ? Container[key] : default(T);
        }

        public KeyValuePair<TK, T> GetByIdWithKey(TK key)
        {
            return Container.ContainsKey(key) ? new KeyValuePair<TK, T>(key, Container[key]) : new KeyValuePair<TK, T>(key, default(T));
        }

        public bool ContainsKey(TK key)
        {
            return Container.ContainsKey(key);
        }
    }
}