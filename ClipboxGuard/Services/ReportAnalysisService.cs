using Clipbox.Core;
using Clipbox.Core.Diagnostics;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Serialization;
using ClipboxGuard.Enums;
using ClipboxGuard.Services.Common;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Linq;
using System.Text;

namespace ClipboxGuard.Services
{
    public class ReportAnalysisService : IReportAnalysisService
    {
        private readonly IPersistenceProvider _persistenceProvider;

        public ReportAnalysisService(IPersistenceProvider persistenceProvider)
        {
            _persistenceProvider = persistenceProvider;
        }

        public ReportAnalysis GetAnalysis()
        {
            ErrorOrigin errorOrigin = ErrorOrigin.Unknown;
            
            StringBuilder errorDetailsStringBuilder = new StringBuilder();
            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.MAIN_CRASHFILE_PERSISTENCE_KEY))
            {
                errorDetailsStringBuilder.AppendLine("Main process crash error:");
                errorDetailsStringBuilder.Append(_persistenceProvider.ReadText(ProductConstants.MAIN_CRASHFILE_PERSISTENCE_KEY));
                errorOrigin = ErrorOrigin.MainProcess;
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.MAIN_PROCESS_FAILURE_XML_PERSISTENCE_KEY))
            {
                string xml = _persistenceProvider.ReadText(ProductConstants.MAIN_PROCESS_FAILURE_XML_PERSISTENCE_KEY);
                DiagnosticsReport dr = XmlSerialization.Deserialize<DiagnosticsReport>(xml);

                errorDetailsStringBuilder.AppendLine("Main process exception:");
                errorDetailsStringBuilder.AppendLine(dr.FailureInfo.ExceptionStringData);
                errorOrigin = ErrorOrigin.MainProcess;
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.GUARD_FAILURE_XML_PERSISTENCE_KEY))
            {
                string xml = _persistenceProvider.ReadText(ProductConstants.GUARD_FAILURE_XML_PERSISTENCE_KEY);
                DiagnosticsReport dr = XmlSerialization.Deserialize<DiagnosticsReport>(xml);

                errorDetailsStringBuilder.AppendLine("Guard process exception:");
                errorDetailsStringBuilder.AppendLine(dr.FailureInfo.ExceptionStringData);
                errorOrigin |= ErrorOrigin.GuardProcess;
            }
            
            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_CRASHFILE_PERSISTENCE_KEY))
            {
                string crashMessage = _persistenceProvider.ReadText(ProductConstants.SEARCH_CRASHFILE_PERSISTENCE_KEY);
                if (!string.IsNullOrWhiteSpace(crashMessage))
                {
                    errorDetailsStringBuilder.AppendLine("Search process crash error:");
                    errorDetailsStringBuilder.Append(crashMessage);
                }

                // there may not be any crash message in the crash file, but its presence
                // still indicates a crash
                errorOrigin |= ErrorOrigin.SearchProcess;
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_PROCESS_FAILURE_XML_PERSISTENCE_KEY))
            {
                string xml = _persistenceProvider.ReadText(ProductConstants.SEARCH_PROCESS_FAILURE_XML_PERSISTENCE_KEY);
                DiagnosticsReport dr = XmlSerialization.Deserialize<DiagnosticsReport>(xml);

                errorDetailsStringBuilder.AppendLine("Search process exception:");
                errorDetailsStringBuilder.AppendLine(dr.FailureInfo.ExceptionStringData);
                errorOrigin |= ErrorOrigin.SearchProcess;
            }

            if (_persistenceProvider.CheckIfCategoryExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY)
                && _persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE))
            {
                errorDetailsStringBuilder.AppendLine("Search index exception:");
                errorDetailsStringBuilder.Append(_persistenceProvider.ReadText(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE));
                errorOrigin |= ErrorOrigin.SearchIndex;
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.DESKBAND_LOG_APPDATA_FILENAME))
            {
                errorDetailsStringBuilder.AppendLine("Deskband errors:");
                errorDetailsStringBuilder.Append(_persistenceProvider.ReadText(ProductConstants.DESKBAND_LOG_APPDATA_FILENAME));
                errorOrigin |= ErrorOrigin.Deskband;
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.DESKBAND_OWNER_LOG_APPDATA_FILENAME))
            {
                errorDetailsStringBuilder.AppendLine("Deskband owner process errors:");
                errorDetailsStringBuilder.Append(_persistenceProvider.ReadText(ProductConstants.DESKBAND_OWNER_LOG_APPDATA_FILENAME));
                errorOrigin |= ErrorOrigin.DeskbandOwner;
            }

            int flagsCount = Enum.GetValues(typeof(ErrorOrigin))
                .Cast<ErrorOrigin>()
                .Count(v => errorOrigin.HasFlag(v));

            if (flagsCount > 1)
            {
                errorOrigin &= ~ErrorOrigin.Unknown;
            }

            return new ReportAnalysis(errorOrigin, errorDetailsStringBuilder.ToString());
        }
    }
}
