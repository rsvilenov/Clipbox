using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.SearchTerms
{
    [Serializable]
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

        protected SourceMachineIdUserNamePairSearchTerm(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineIdUserNamePair;
    }
}
