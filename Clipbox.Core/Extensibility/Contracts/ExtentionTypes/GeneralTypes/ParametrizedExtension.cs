using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public abstract class ParametrizedExtension : ConfigurableExtension, IHaveFlowParameters, IClipboxExtension
    {
        private readonly List<IFlowParameter> _parameters = new List<IFlowParameter>();
        private readonly Dictionary<string, object> _initialParameterValues = new Dictionary<string, object>();

        public event EventHandler<FlowParametersChangedEventArgs> ParametersChanged;

        public void SetParameter<T>(string key, T value)
        {
            IFlowParameter existingParameter = _parameters.FirstOrDefault(p => p.Key == key && p.Type == typeof(T));
            if (existingParameter != null)
            {
                T initialValue = default(T);
                if (_initialParameterValues.ContainsKey(existingParameter.Key))
                {
                    initialValue = (T)_initialParameterValues[existingParameter.Key];
                    if (initialValue.Equals(value))
                    {
                        _initialParameterValues.Remove(existingParameter.Key);
                    }
                }
                else 
                {
                    initialValue = ((FlowParameter<T>)existingParameter).Value;
                    if (!value.Equals(initialValue))
                    {
                        _initialParameterValues.Add(existingParameter.Key, initialValue);
                    }
                }
                
                _parameters.Remove(existingParameter);
            }

            FlowParameter<T> parameterToAdd = new FlowParameter<T>(key, value);
            _parameters.Add(parameterToAdd);

            bool areParamsSetToInitialState = !_initialParameterValues.Any();
            ParametersChanged?.Invoke(this, new FlowParametersChangedEventArgs(areParamsSetToInitialState));
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
            _initialParameterValues.Clear();
        }

        public void LoadParameters(IEnumerable<IFlowParameter> parameters)
        {
            _parameters.AddRange(parameters);
        }
    }
}
