using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.Threading
{
    public class CancellableTaskManager
    {
        private Dictionary<string, CancellationTokenSource> _cancellationTokens = new Dictionary<string, CancellationTokenSource>();
        private Dictionary<string, string> _internalIdToTaskIdMapping = new Dictionary<string, string>();

        public Task ScheduleTask(string taskId, Action<string> taskAction)
        {
            // if the caller has added a second task with the same id,
            // cancel the first task.
            if (_internalIdToTaskIdMapping.ContainsValue(taskId))
            {
                CancelScheduledTask(taskId);
            }

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            // we use internal task ids because the user may (and is expected to be able to)
            // shchedule a task with the same id twice - the second task replaces the first.
            string internalId = $"{taskId}{Guid.NewGuid()}";
            _internalIdToTaskIdMapping.Add(internalId, taskId);

            var task = Task.Run(() => {
                // cancel only the tasks that have been scheduled but are not yet executing
                if (_cancellationTokens.ContainsKey(internalId)
                    && _cancellationTokens[internalId].IsCancellationRequested)
                {
                    _cancellationTokens.Remove(internalId);
                    return;
                }

                taskAction(taskId);

                _cancellationTokens.Remove(internalId);
            }, token);

            if (_cancellationTokens.ContainsKey(internalId))
            {
                throw new InvalidOperationException($"The token for task {taskId} should have been removed from the token dictionary by now.");
            }
            else
            {
                _cancellationTokens.Add(internalId, tokenSource);
            }

            return task;
        }

        public void CancelScheduledTask(string taskId)
        {
            foreach (string internalId in _internalIdToTaskIdMapping
                .Where(kv => kv.Value == taskId)
                .Select(kv => kv.Key))
            {
                if (_cancellationTokens.ContainsKey(internalId))
                {
                    _cancellationTokens[internalId].Cancel();
                }
            }
        }
    }
}
