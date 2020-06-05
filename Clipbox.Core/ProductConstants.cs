using System.Collections.Generic;

namespace Clipbox.Core
{
    public static class ProductConstants
    {
        public const string PRODUCT_NAME = "Clipbox";

        public const double PRODUCT_VERSION = 0.1;

        public const string CLIENT_EXE_NAME = "Clipbox.exe";

        public const string GUARD_EXE_NAME = "ClipboxGuard.exe";

        public const string SERVICE_EXE_NAME = "ClipboxSvc.exe";

        public const string STARTED_FROM_SERVICE_ARG = "svc";

        public const string STARTED_FROM_SCHEDULER_ARG = "sched";

        public const string STARTED_FROM_GUARD_ARG = "guard";

        public const string SERVICE_GUARD_NAMED_PIPE_PREFIX = "ClipboxPipe";

        public const string SEARCH_SERVICE_NAMED_PIPE_PREFIX = "ClipboxSearchPipe";

        public const string SHELL_EXTENSION_NAMED_PIPE_PREFIX = "ClipboxShExPipe";
        
        public static readonly IReadOnlyList<byte> CLIENT_EXE_HASH = new byte[] { 0 };
    }
}
