using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public interface IHaveFlowParameters
    {
        /// <summary>
        /// Gets all flow parameters, associates with this object.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<IFlowParameter> GetAllParameters();
    }
}
