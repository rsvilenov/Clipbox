namespace Clipbox.Core.Search.Enums
{
    /// <summary>
    /// Indicates whether the term must/may or must not exist in the search results
    /// </summary>
    public enum TermOccurrence
    {
        OBLIGATORY = 0, // default
        NON_OBLIGATORY,
        NON_PRESENT
    }
}
