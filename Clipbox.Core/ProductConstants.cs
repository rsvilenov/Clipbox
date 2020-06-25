using System.Collections.Generic;

namespace Clipbox.Core
{
    public static class ProductConstants
    {
        public const string PRODUCT_NAME = "Clipbox";

        public const double PRODUCT_VERSION = 0.1;

        public const string MAIN_PROCESS_EXE_NAME = "Clipbox.exe";

        public const string GUARD_EXE_NAME = "ClipboxGuard.exe";

        public const string SERVICE_EXE_NAME = "ClipboxSvc.exe";

        public const string STARTED_FROM_SERVICE_ARG = "svc";

        public const string STARTED_FROM_SCHEDULER_ARG = "sched";

        public const string STARTED_FROM_GUARD_ARG = "guard";

        public const string RUN_WITH_HIGH_PRIORITY_FLAG_KEY = "_hp";

        // deprecated
        public const string SERVICE_NAMED_PIPE = "ClipboxSvcPipe";

        public const string GUARD_NAMED_PIPE_PREFIX = "ClipboxGuardPipe";

        public const string PIPE_COMMAND_QUIT = "Quit";

        public const string PIPE_COMMAND_DISABLE_SEARCH = "DisableSearch";

        public const string SEARCH_SERVICE_NAMED_PIPE_PREFIX = "ClipboxSearchPipe";

        public const string SEARCH_USE_EXTERNAL_PROCESS_FLAG_KEY = "_useExternalSearch";
        
        public const string SEARCH_PROCESS_EXE_NAME = "ClipboxSearch.exe";

        public const string SEARCH_INDEX_ERROR_LOG_CATEGORY = "searchIdxErrors";

        public const string SEARCH_INDEX_ERROR_LOG_AGGREGATE = "aggregate.log";
        
        public const string MAIN_PROCESS_FAILURE_XML_PERSISTENCE_KEY = "lastException.xml";
        
        public const string GUARD_FAILURE_XML_PERSISTENCE_KEY = "lastGuardException.xml";

        public const string SEARCH_PROCESS_FAILURE_XML_PERSISTENCE_KEY = "lastSearchException.xml";

        public const string MAIN_CRASHFILE_PERSISTENCE_KEY = "mainCrash.log";

        public const string SEARCH_CRASHFILE_PERSISTENCE_KEY = "searchCrash.log";

        // corresponds to the log file, created in DeskBand.cpp
        // TODO: maybe set it in the registry
        public const string DESKBAND_LOG_APPDATA_FILENAME = "deskband.log";

        public const string SHELL_EXTENSION_NAMED_PIPE_PREFIX = "ClipboxShExPipe";
        
        public static readonly IReadOnlyList<byte> CLIENT_EXE_HASH = new byte[] { 0 };
    }
}
