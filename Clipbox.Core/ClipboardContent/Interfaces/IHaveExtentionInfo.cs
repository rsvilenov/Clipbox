namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IHaveExtentionInfo
    {
        string FormatExtentionName { get; set; }

        double FormatExtentionVersion { get; set; }
    }
}
