using System;

namespace Clipbox.Infrastructure.Factories
{
    public interface IFactory
    {
        T GetInstance<T>()
            where T : class, new();

        object GetInstance(Type type);
    }
}
