using Clipbox.Core;
using Clipbox.Core.Diagnostics;
using Clipbox.Infrastructure.Diagnostics;
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

            AggregateReport aggregateReport = AggregateReportGenerator.CreateNewEmptyReport(ProductVersionHelper.GetFullProductVersion());

            StringBuilder errorDetailsStringBuilder = new StringBuilder();
            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.MAIN_CRASHFILE_PERSISTENCE_KEY))
            {
                string mainProcessCrashText = _persistenceProvider.ReadText(ProductConstants.MAIN_CRASHFILE_PERSISTENCE_KEY);
                errorDetailsStringBuilder.AppendLine("Main process crash error:");
                errorDetailsStringBuilder.Append(mainProcessCrashText);
                errorOrigin = ErrorOrigin.MainProcess;

                aggregateReport.AddComponentError(Component.MainProcess, mainProcessCrashText);
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.MAIN_PROCESS_FAILURE_XML_PERSISTENCE_KEY))
            {
                string xml = _persistenceProvider.ReadText(ProductConstants.MAIN_PROCESS_FAILURE_XML_PERSISTENCE_KEY);
                DiagnosticsReport dr = XmlSerialization.Deserialize<DiagnosticsReport>(xml);

                errorDetailsStringBuilder.AppendLine("Main process exception:");
                errorDetailsStringBuilder.AppendLine(dr.FailureInfo.ExceptionStringData);
                errorOrigin = ErrorOrigin.MainProcess;

                aggregateReport.MainProcessReport = dr;
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.GUARD_FAILURE_XML_PERSISTENCE_KEY))
            {
                string xml = _persistenceProvider.ReadText(ProductConstants.GUARD_FAILURE_XML_PERSISTENCE_KEY);
                DiagnosticsReport dr = XmlSerialization.Deserialize<DiagnosticsReport>(xml);

                errorDetailsStringBuilder.AppendLine("Guard process exception:");
                errorDetailsStringBuilder.AppendLine(dr.FailureInfo.ExceptionStringData);
                errorOrigin |= ErrorOrigin.GuardProcess;

                aggregateReport.GuardProcessReport = dr;
            }
            
            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_CRASHFILE_PERSISTENCE_KEY))
            {
                string crashMessage = _persistenceProvider.ReadText(ProductConstants.SEARCH_CRASHFILE_PERSISTENCE_KEY);
                if (!string.IsNullOrWhiteSpace(crashMessage))
                {
                    errorDetailsStringBuilder.AppendLine("Search process crash error:");
                    errorDetailsStringBuilder.Append(crashMessage);

                    aggregateReport.AddComponentError(Component.SearchProcess, crashMessage);
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

                aggregateReport.SearchProcessReport = dr;
            }

            if (_persistenceProvider.CheckIfCategoryExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY)
                && _persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE))
            {
                string searchIndexErrorText = _persistenceProvider.ReadText(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE);
                errorDetailsStringBuilder.AppendLine("Search index exception:");
                errorDetailsStringBuilder.Append(searchIndexErrorText);
                errorOrigin |= ErrorOrigin.SearchIndex;

                aggregateReport.AddComponentError(Component.SearchIndex, searchIndexErrorText);
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.DESKBAND_LOG_APPDATA_FILENAME))
            {
                string deskbandLogText = _persistenceProvider.ReadText(ProductConstants.DESKBAND_LOG_APPDATA_FILENAME);
                if (!string.IsNullOrWhiteSpace(deskbandLogText)
                    && deskbandLogText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length > 3) // the first three lines are the "started" part, which is always there
                {
                    errorDetailsStringBuilder.AppendLine("Deskband errors:");
                    errorDetailsStringBuilder.Append(deskbandLogText);
                    errorOrigin |= ErrorOrigin.Deskband;
                    aggregateReport.AddComponentError(Component.Deskband, deskbandLogText);
                }
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.DESKBAND_OWNER_LOG_APPDATA_FILENAME))
            {
                string deskbandOwnerLogText = _persistenceProvider.ReadText(ProductConstants.DESKBAND_OWNER_LOG_APPDATA_FILENAME);
                if (!string.IsNullOrWhiteSpace(deskbandOwnerLogText)
                    && deskbandOwnerLogText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length > 3)
                {
                    errorDetailsStringBuilder.AppendLine("Deskband owner process errors:");
                    errorDetailsStringBuilder.Append(deskbandOwnerLogText);
                    errorOrigin |= ErrorOrigin.DeskbandOwner;
                    aggregateReport.AddComponentError(Component.DeskbandOwner, deskbandOwnerLogText);
                }
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.DESKBAND_CONTROL_LOG_APPDATA_FILENAME))
            {
                string deskbandControlLogText = _persistenceProvider.ReadText(ProductConstants.DESKBAND_CONTROL_LOG_APPDATA_FILENAME);
                if (!string.IsNullOrWhiteSpace(deskbandControlLogText)
                    && deskbandControlLogText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length > 3)
                {
                    errorDetailsStringBuilder.AppendLine("Deskband control process errors:");
                    errorDetailsStringBuilder.Append(deskbandControlLogText);
                    errorOrigin |= ErrorOrigin.DeskbandControl;
                    aggregateReport.AddComponentError(Component.DeskbandControl, deskbandControlLogText);
                }
            }

            int flagsCount = Enum.GetValues(typeof(ErrorOrigin))
                .Cast<ErrorOrigin>()
                .Count(v => errorOrigin.HasFlag(v));

            if (flagsCount > 1)
            {
                errorOrigin &= ~ErrorOrigin.Unknown;
            }

            return new ReportAnalysis(errorOrigin, errorDetailsStringBuilder.ToString(), aggregateReport);
        }

    }
}
