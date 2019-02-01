using System;
using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public abstract class ManyPreviewHandler<TPreviewData> : DisposableObject, IManyPreviewHandler
        where TPreviewData : IPreviewData
    {
        public Type PreviewDataType
        {
            get
            {
                return typeof(TPreviewData);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="previewWidth"></param>
        /// <param name="previewHeight"></param>
        /// <param name="maxNumberOfPreviewsToGenerate">0 means no limit</param>
        /// <returns></returns>
        public abstract IEnumerable<TPreviewData> GetPreview(int previewWidth, int previewHeight, int maxNumberOfPreviewsToGenerate);

        public abstract void SetData(object clipboardData);

        public abstract int GetItemsTotalCount();
    }

    public interface IManyPreviewHandler : IPreviewHandler
    {
        void SetData(object clipboardData);

        int GetItemsTotalCount();
    }
}
