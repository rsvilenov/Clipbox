using System;

namespace Clipbox.Infrastructure.Factories
{
    public class RegularFactory : IFactory
    {
        public object GetInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        public T GetInstance<T>() where T : class, new()
        {
            return Activator.CreateInstance<T>();
        }
    }
}
