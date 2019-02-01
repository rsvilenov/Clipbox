using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Infrastructure.Factories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Infrastructure.Extensibility.Containers
{
    public abstract class BaseExtentionContainer<TExtentionBaseType, TFactory>
        where TExtentionBaseType : class
        where TFactory : class, IFactory, new()
    {
        IFactory _factory;
        private ConcurrentDictionary<Type, IExtentionInfo> _registeredExtentions;

        protected IReadOnlyCollection<Type> RegisteredTypes
        {
            get
            {
                return _registeredExtentions.
                    Select(ext => ext.Key).
                    ToList().
                    AsReadOnly();
            }
        }

        protected BaseExtentionContainer()
        {
            _factory = Activator.CreateInstance<TFactory>();
            _registeredExtentions = new ConcurrentDictionary<Type, IExtentionInfo>();
        }

        public virtual void RegisterExtention<TExtentionType>()
            where TExtentionType : class, TExtentionBaseType, new()
        {
            RegisterExtention(typeof(TExtentionBaseType));
        }

        public virtual void RegisterExtention(Type type)
        {
            if (_registeredExtentions.ContainsKey(type))
            {
                throw new ArgumentException($"Type {type.Name} has already been added.");
            }
            
            if (!type.GetInterfaces().Contains(typeof(TExtentionBaseType)) && !type.IsSubclassOf(typeof(TExtentionBaseType)))
            {
                throw new InvalidCastException($"{type.Name} does not implement {typeof(TExtentionBaseType).FullName}");
            }

            if (ClipboxExtentionAttribute.IsDefinedFor(type))
            {
                IExtentionInfo extentionInfo = ClipboxExtentionAttribute.GetExtentionInfo(type);
                _registeredExtentions.TryAdd(type, extentionInfo);
            }
        }
        
        protected TExtentionType Resolve<TExtentionType>()
            where TExtentionType : class, TExtentionBaseType, new()
        {
            if (!_registeredExtentions.Any(t => t.Key == typeof(TExtentionType)))
            {
                throw new InvalidOperationException("The type has not been registered.");
            }

            return _factory.GetInstance<TExtentionType>();
        }


        protected TExtentionBaseType Resolve(Type type)
        {
            if (!_registeredExtentions.Any(t => t.Key == type))
            {
                throw new InvalidOperationException("The type has not been registered.");
            }

            return (TExtentionBaseType)_factory.GetInstance(type);
        }

        public IExtentionInfo GetExtentionInfo(IClipboxExtention extention)
        {
            IExtentionInfo extInfo = null;
            Type type = extention.GetType();

            if (!_registeredExtentions.TryGetValue(type, out extInfo))
            {
                throw new KeyNotFoundException($"There is no registered extention of type {type.Name}");
            }

            return extInfo;
        }

        
    }
}
