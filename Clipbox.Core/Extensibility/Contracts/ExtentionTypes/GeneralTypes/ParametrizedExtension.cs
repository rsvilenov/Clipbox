using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public abstract class ParametrizedExtension : ConfigurableExtension, IClipboxExtension
    {
        private readonly List<IFlowParameter> _parameters = new List<IFlowParameter>();
        
        public void SetParameter<T>(string key, T value)
        {
            IFlowParameter existingParameter = _parameters.FirstOrDefault(p => p.Key == key && p.Type == typeof(T));
            if (existingParameter != null)
            {
                _parameters.Remove(existingParameter);
            }

            FlowParameter<T> parameterToAdd = new FlowParameter<T>(key, value);
            _parameters.Add(parameterToAdd);
        }

        protected T GetParameter<T>(string key)
        {
            IFlowParameter existingParameter = _parameters.FirstOrDefault(p => p.Key == key && p.Type == typeof(T));
            if (existingParameter != null)
            {
                return (existingParameter as FlowParameter<T>).Value;
            }

            return default(T);
        }

        protected bool IsParameterSet<T>(string key, T value)
        {
            IFlowParameter existingParameter = _parameters.FirstOrDefault(p => p.Key == key && p.Type == typeof(T));
            if (existingParameter != null)
            {
                return (existingParameter as FlowParameter<T>).Value.Equals(value);
            }

            return false;
        }

        public IReadOnlyList<IFlowParameter> GetAllParameters()
        {
            return _parameters.AsReadOnly();
        }

        public void ClearParameters()
        {
            _parameters.Clear();
        }

        public void LoadParameters(IEnumerable<IFlowParameter> parameters)
        {
            _parameters.AddRange(parameters);
        }
    }
}
