using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceMachineIdUserNamePairSearchTerm : SearchTermBase<string>
    {
        /// <summary>
        /// Search by Login UserName and MachineUniqueId
        /// </summary>
        /// <param name="sourceMachineIdUserNamePairs">format: username@MachineUniqueId</param>
        public SourceMachineIdUserNamePairSearchTerm(IEnumerable<string> sourceMachineIdUserNamePairs)
            : base(sourceMachineIdUserNamePairs)
        { }

        public SourceMachineIdUserNamePairSearchTerm()
            : base()
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineIdUserNamePair;
    }
}
