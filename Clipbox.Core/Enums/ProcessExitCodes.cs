namespace Clipbox.Core.Enums
{
    /// <summary>
    /// Exit codes for our processes
    /// </summary>
    public enum ProcessExitCodes
    {
        /// <summary>
        /// We will redefine the default '0'
        /// from OK to Undefined, so that we know that if our process
        /// exits with '0' but still has an error then something is not right.
        /// </summary>
        Undefined = 0,
        Ok = 500,
        Error = 501,
        Special = 502
    }
}
