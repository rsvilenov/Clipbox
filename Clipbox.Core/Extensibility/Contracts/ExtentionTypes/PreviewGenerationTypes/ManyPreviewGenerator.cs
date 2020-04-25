using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes
{
    public abstract class ManyPreviewGenerator<TIntermediateFormat, TPreviewData> : DisposableObject, IManyPreviewGenerator
        where TIntermediateFormat : IIntermediateFormat
        where TPreviewData : IPreviewData
    {
        public Type PreviewDataType
        {
            get
            {
                return typeof(TPreviewData);
            }
        }

        private TIntermediateFormat _intermediateFormatObj;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="previewWidth"></param>
        /// <param name="previewHeight"></param>
        /// <param name="maxNumberOfPreviewsToGenerate">0 means no limit</param>
        /// <returns></returns>
        public IEnumerable<IPreviewData> GetPreview(int previewWidth, int previewHeight, int maxNumberOfPreviewsToGenerate)
        {
            return GetPreview(_intermediateFormatObj, previewWidth, previewHeight, maxNumberOfPreviewsToGenerate)
                .Cast<IPreviewData>();
        }
        
        protected abstract IEnumerable<TPreviewData> GetPreview(TIntermediateFormat intermediateFormatObj, int previewWidth, int previewHeight, int maxNumberOfPreviewsToGenerate);

        public void SetData(IIntermediateFormat intermediateFormatObj)
        {
            if (!(intermediateFormatObj is TIntermediateFormat))
            {
                throw new InvalidCastException($"Expected type {typeof(TIntermediateFormat).Name}");
            }

            _intermediateFormatObj = (TIntermediateFormat)intermediateFormatObj;
        }

        protected abstract int GetItemsTotalCount(TIntermediateFormat intermediateFormatObj);

        public int GetItemsTotalCount()
        {
            return GetItemsTotalCount(_intermediateFormatObj);
        }
    }

    public interface IManyPreviewGenerator : IPreviewGenerator
    {
        void SetData(IIntermediateFormat clipboardData);

        int GetItemsTotalCount();

        IEnumerable<IPreviewData> GetPreview(int previewWidth, int previewHeight, int maxNumberOfPreviewsToGenerate);
    }
}
