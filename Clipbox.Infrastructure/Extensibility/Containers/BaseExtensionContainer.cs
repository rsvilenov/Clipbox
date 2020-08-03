using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public abstract class BaseExtensionContainer<TExtensionBaseType, TFactory> : IExtensionRegistrator
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

        public void RegisterExtension<TExtensionType>()
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

            if (!ClipboxExtensionAttribute.IsDefinedFor(type))
            {
                throw new ArgumentException($"{type.Name} is not decorated with {nameof(ClipboxExtensionAttribute)}");
            }

            if (!type.GetInterfaces().Contains(typeof(TExtensionBaseType)) && !type.IsSubclassOf(typeof(TExtensionBaseType)))
            {
                throw new InvalidCastException($"{type.Name} does not implement {typeof(TExtensionBaseType).FullName}");
            }
            
            IExtensionInfo extensionInfo = ClipboxExtensionAttribute.GetExtensionInfo(type);
            if (extensionInfo.MinHostVersion <= ProductConstants.PRODUCT_VERSION)
            {
                _registeredExtensions.TryAdd(type, extensionInfo);
            }
            else
            {
                throw new InvalidOperationException($"{type.Name} requires host version {extensionInfo.MinHostVersion} or higher. However, the current host version is {ProductConstants.PRODUCT_VERSION}");
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
    }
}
