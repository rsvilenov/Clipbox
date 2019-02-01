using System;

namespace Clipbox.Core.Extensibility.Contracts
{
    public class FlowParameter<T> : IFlowParameter
    {
        public FlowParameter(string key, T value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public Type Type
        {
            get
            {
                return typeof(T);
            }
        }

        public T Value { get; }
    }

    public interface IFlowParameter
    {
        string Key { get; }

        Type Type { get; }
    }
}
