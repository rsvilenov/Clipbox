using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public abstract class BaseExtensionContainer<TExtensionBaseType, TFactory>
        where TExtensionBaseType : class
        where TFactory : class, IFactory, new()
    {
        IFactory _factory;
        private ConcurrentDictionary<Type, IExtensionInfo> _registeredExtensions;

        protected IReadOnlyCollection<Type> RegisteredTypes
        {
            get
            {
                return _registeredExtensions.
                    Select(ext => ext.Key).
                    ToList().
                    AsReadOnly();
            }
        }

        protected BaseExtensionContainer()
        {
            _factory = Activator.CreateInstance<TFactory>();
            _registeredExtensions = new ConcurrentDictionary<Type, IExtensionInfo>();
        }

        public virtual void RegisterExtension<TExtensionType>()
            where TExtensionType : class, TExtensionBaseType, new()
        {
            RegisterExtension(typeof(TExtensionBaseType));
        }

        public virtual void RegisterExtension(Type type)
        {
            if (_registeredExtensions.ContainsKey(type))
            {
                throw new ArgumentException($"Type {type.Name} has already been added.");
            }
            
            if (!type.GetInterfaces().Contains(typeof(TExtensionBaseType)) && !type.IsSubclassOf(typeof(TExtensionBaseType)))
            {
                throw new InvalidCastException($"{type.Name} does not implement {typeof(TExtensionBaseType).FullName}");
            }

            if (ClipboxExtensionAttribute.IsDefinedFor(type))
            {
                IExtensionInfo extensionInfo = ClipboxExtensionAttribute.GetExtensionInfo(type);
                _registeredExtensions.TryAdd(type, extensionInfo);
            }
        }
        
        protected TExtensionType Resolve<TExtensionType>()
            where TExtensionType : class, TExtensionBaseType, new()
        {
            if (!_registeredExtensions.Any(t => t.Key == typeof(TExtensionType)))
            {
                throw new InvalidOperationException("The type has not been registered.");
            }

            return _factory.GetInstance<TExtensionType>();
        }


        protected TExtensionBaseType Resolve(Type type)
        {
            if (!_registeredExtensions.Any(t => t.Key == type))
            {
                throw new InvalidOperationException("The type has not been registered.");
            }

            return (TExtensionBaseType)_factory.GetInstance(type);
        }

        public IExtensionInfo GetExtensionInfo(IClipboxExtension extension)
        {
            IExtensionInfo extInfo = null;
            Type type = extension.GetType();

            if (!_registeredExtensions.TryGetValue(type, out extInfo))
            {
                throw new KeyNotFoundException($"There is no registered extension of type {type.Name}");
            }

            return extInfo;
        }

        
    }
}
