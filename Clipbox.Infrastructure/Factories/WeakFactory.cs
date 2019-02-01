using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Clipbox.Infrastructure.Factories
{
    /// <summary>
    /// A factory that stores and reuses the created objects until they are referenced somewhere
    /// </summary>
    public class WeakFactory : IFactory
    {
        internal ConcurrentDictionary<Type, object> _typesAndWeakRefs =
            new ConcurrentDictionary<Type, object>();
        
        public T GetInstance<T>()
            where T : class, new()
        {
            Type type = typeof(T);

            if (type.GetInterfaces().Contains(typeof(IDisposable)))
            {
                // We cannot be sure that the IDisposables implement the full disposable pattern
                // and we have no way to know when to call Dispose(), so these types are not welcome
                throw new NotSupportedException("Types implementing IDisposable are not supported.");
            }

            T instance = default(T);
            
            if (_typesAndWeakRefs.ContainsKey(type))
            {
                WeakReference<T> reference = _typesAndWeakRefs[type] as WeakReference<T>;

                if (reference == null || !reference.TryGetTarget(out instance))
                {
                    instance = Activator.CreateInstance<T>();
                    _typesAndWeakRefs[type] = new WeakReference<T>(instance);
                }
            }
            else
            {
                instance = Activator.CreateInstance<T>();
                _typesAndWeakRefs.TryAdd(type, new WeakReference<T>(instance));
            }

            return instance;
        }

        public object GetInstance(Type type)
        {
            // TODO: refactor this
            MethodInfo createOrGet = typeof(WeakFactory).GetMethod(nameof(this.GetInstance), Type.EmptyTypes);
            MethodInfo generic = createOrGet.MakeGenericMethod(type);
            var instance = generic.Invoke(this, null);
            return instance;
        }
    }
}
