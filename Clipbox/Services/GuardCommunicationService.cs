using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Clipbox.Services
{
    public class GuardCommunicationService : IGuardCommunicationService
    {
        private readonly NamedPipesServer _pipeServer;
        private readonly IPersistenceProvider _persistenceProvider;
        private readonly ISettingsService _settingsService;

        public GuardCommunicationService(
            IPersistenceProvider persistenceProvider,
            ISettingsService settingsService,
            int sessionId)
        {
            _persistenceProvider = persistenceProvider;
            _settingsService = settingsService;

            SetOrUnsetPersistentFlagForHighPriority();
            _settingsService.SettingChanged += _settingsService_SettingChanged;

            _pipeServer = new NamedPipesServer($"{ProductConstants.MAINPROC_GUARD_COMM_NAMED_PIPE_PREFIX}.{sessionId}");
            _pipeServer.GotMessage += _pipeServer_GotMessage;
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.RunMainProcessWithHighPriority)
            {
                SetOrUnsetPersistentFlagForHighPriority();
            }
        }

        private void _pipeServer_GotMessage(object sender, NamedPipesResponseEventArgs e)
        {
            if (e.ReceivedMessage == ProductConstants.PIPE_COMMAND_QUIT)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                    new Action(() =>
                    {
                        Application.Current.Shutdown((int)ProcessExitCodes.Ok);
                    }));
            }
            else if (e.ReceivedMessage == ProductConstants.PIPE_COMMAND_DISABLE_SEARCH)
            {
                _settingsService.SetSetting(SettingsKeys.IsSearchEnabled, false);
            }
        }

        private void SetOrUnsetPersistentFlagForHighPriority()
        {
            bool highPriority = _settingsService.GetSetting<bool>(SettingsKeys.RunMainProcessWithHighPriority);
            if (highPriority)
            {
                _persistenceProvider.Write(ProductConstants.RUN_WITH_HIGH_PRIORITY_FLAG_KEY, string.Empty);
            }
            else
            {
                _persistenceProvider.Delete(ProductConstants.RUN_WITH_HIGH_PRIORITY_FLAG_KEY);
            }
        }
    }
}
