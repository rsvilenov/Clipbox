using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.Threading
{
    public class CancellableTaskManager
    {
        private ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
        private ConcurrentDictionary<string, string> _internalIdToTaskIdMapping = new ConcurrentDictionary<string, string>();

        public Task ScheduleTask(string taskId, Action<string> taskAction)
        {
            // if the caller has added a second task with the same id,
            // cancel the first task. (the linq method is thread-safe as it relies on GetEnumerator)
            if (_internalIdToTaskIdMapping.Any(kvp => kvp.Value == taskId))
            {
                CancelScheduledTask(taskId);
            }

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            // we use internal task ids because the user may (and is expected to be able to)
            // shchedule a task with the same id twice - the second task replaces the first.
            string internalId = $"{taskId}{Guid.NewGuid()}";
            // the key here is a newly generated unique guid, so TryAdd should never fail
            _internalIdToTaskIdMapping.TryAdd(internalId, taskId);

            var task = Task.Run(() => {
                
                CancellationTokenSource cts = null;

                // cancel only the tasks that have been scheduled but are not yet executing
                if (_cancellationTokens.TryGetValue(internalId, out cts))
                {
                    if (cts.IsCancellationRequested)
                    {
                        if (_cancellationTokens.TryRemove(internalId, out cts))
                        {
                            cts.Dispose();
                        }
                        // stop the task here
                        return;
                    }
                }

                taskAction(taskId);

                if (_cancellationTokens.TryRemove(internalId, out cts))
                {
                    cts.Dispose();
                }
            }, token);

            if (_cancellationTokens.ContainsKey(internalId))
            {
                throw new InvalidOperationException($"The token for task {taskId} should have been removed from the token dictionary by now.");
            }
            else
            {
                if (!_cancellationTokens.TryAdd(internalId, tokenSource))
                {
                    // this can happen if I change the code above without paying much attention...
                    throw new InvalidOperationException($"Key {internalId} already exists in {nameof(_cancellationTokens)} dictionary.");
                }
            }

            return task;
        }

        public void CancelScheduledTask(string taskId)
        {
            // linq uses GetEnumerator, which takes a snapshot of the dictionary's content,
            // so the code below should be thread-safe
            // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getenumerator?redirectedfrom=MSDN&view=netframework-4.8#System_Collections_Concurrent_ConcurrentDictionary_2_GetEnumerator
            foreach (string internalId in _internalIdToTaskIdMapping
                .Where(kv => kv.Value == taskId)
                .Select(kv => kv.Key))
            {
                CancellationTokenSource cts = null;
                if (_cancellationTokens.TryGetValue(internalId, out cts))
                {
                    cts.Cancel();
                }
            }
        }
    }
}
