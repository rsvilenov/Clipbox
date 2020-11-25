using System.Collections.Generic;

namespace Clipbox.Core
{
    public static class ProductConstants
    {
        public const string PRODUCT_NAME = "Clipbox";

        public const double PRODUCT_VERSION = 1.0;

        public const string PRODUCT_GUID = "95A2CB1E-AD54-41F4-B1F0-D1E7876A7437";

        public const string WIX_BUNDLE_UPGRADE_CODE = "555C5BB0-4DFA-4559-871F-1408F39A2987";

        public const string COMPANY_NAME = "MyCompany";

        public const string MAIN_PROCESS_EXE_NAME = "Clipbox.exe";

        public const string GUARD_EXE_NAME = "ClipboxGuard.exe";

        public const string SERVICE_EXE_NAME = "ClipboxSvc.exe";

        public const string SERVICE_SC_NAME = "Clipbox Service";

        public static string SERVICE_SC_DESCRIPTION = "This service is vital to the proper functionning of Clipbox.";

        public const string DESKBAND_CTRL_EXE_NAME = "ClipboxDeskbandControl.exe";

        public const string DESKBAND_SHELL_EXT_DLL_NAME = "ClipboxDeskband.dll";

        public const string DESKBAND_SHELL_EXT_GUID = "3C6D5961-E6C7-44F6-A522-A20DE33D2DAC";

        public const string STARTED_FROM_SERVICE_ARG = "svc";

        public const string OPEN_SETTINGS_WINDOW_ARG = "settings";

        public const string DISABLE_CLIPOARD_TRAKING_ARG = "settings";

        public const string STARTED_FROM_SCHEDULER_ARG = "sched";

        public const string STARTED_FROM_GUARD_ARG = "guard";

        public const string OPEN_MAIN_WINDOW_ARG = "main";

        public const string STORE_QV_STATE_ARG = "storeqvstate";

        public const string STARTED_FROM_SHORTCUT = "scut";

        public const string RUN_WITH_HIGH_PRIORITY_FLAG_KEY = "_hp";

        // deprecated
        public const string SERVICE_NAMED_PIPE = "ClipboxSvcPipe";

        public const string MAINPROC_GUARD_COMM_NAMED_PIPE_PREFIX = "ClipboxMainProcGuardCommPipe";

        public const string GUARD_NAMED_PIPE_PREFIX = "ClipboxServiceGuardCommPipe";

        public const string PIPE_COMMAND_QUIT = "Quit";

        public const string PIPE_COMMAND_PING = "Ping";

        public const string PIPE_COMMAND_DISABLE_SEARCH = "DisableSearch";

        public const string PIPE_COMMAND_OPEN_MAIN_WINDOW = "OpenMainWindow";

        public const string PIPE_COMMAND_OPEN_SETTINGS_WINDOW = "OpenSettingsWindow";

        public const string PIPE_COMMAND_DISABLE_TRACKING = "DisableTracking";

        public const string PIPE_COMMAND_STORE_QV_STATE = "StoreQvState";

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

        public const string DESKBAND_INSTALL_FAILURE_FLAG_FILENAME = "noband";

        // corresponds to the log file, created in DeskBand.cpp
        // TODO: maybe set it in the registry
        public const string DESKBAND_LOG_APPDATA_FILENAME = "deskband.log";

        public const string DESKBAND_OWNER_LOG_APPDATA_FILENAME = "deskbandOwner.log";

        public const string DESKBAND_CONTROL_LOG_APPDATA_FILENAME = "deskbandControl.log";

        public const string SHELL_EXTENSION_NAMED_PIPE_PREFIX = "ClipboxShExPipe";
        
        public static readonly IReadOnlyList<byte> CLIENT_EXE_HASH = new byte[] { 0 };

        public const string INITIAL_CULTURE_CODE_FILENAME = "initialCultureCode";

        public const string PRODUCT_LATEST_VERSION_URL = "someUrl";

        public const string PRODUCT_UPDATE_PAGE_URL = "someUrl";

        public const string PRODUCT_EULA_URL = "https://clipbox.org/eula";

        public const string PRODUCT_PRIVACY_POLICY_URL = "https://clipbox.org/privacy";

        public const string HELP_URL = "https://clipbox.org/help";

        public const string REPORT_BUG_URL = "https://clipbox.org/bugreport";

        public const string FEEDBACK_URL = "https://clipbox.org/feedback";

    }
}
