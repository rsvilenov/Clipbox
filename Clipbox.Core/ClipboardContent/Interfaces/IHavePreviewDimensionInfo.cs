namespace Clipbox.Core.ClipboardContent.Interfaces
{
    /// <summary>
    /// Used for storing preview dimentions so that we can know (or calculate) 
    /// how much space we will need to visualize the item.
    /// Consider moving this elsewhere so that this project stays clean of any UI related stuff.
    /// However these dimentions are very useful for the synchronization 
    /// (other machines will be able to know them before generating the preview),
    /// so they may stay here after all.
    /// </summary>
    public interface IHavePreviewDimensionInfo
    {
        PreviewDimensionInfo PreviewDimensionInfo { get; }
    }
}
