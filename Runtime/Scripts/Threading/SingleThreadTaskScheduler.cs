//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PsyForge.Threading {

    // Origin: https://stackoverflow.com/a/30726903
    // If something more complex is needed in the future, look at the following links
    // https://devblogs.microsoft.com/pfxteam/parallelextensionsextras-tour-7-additional-taskschedulers/
    // https://github.com/ChadBurggraf/parallel-extensions-extras/tree/master/TaskSchedulers
    public sealed class SingleThreadTaskScheduler : TaskScheduler {
        [ThreadStatic]
        private static bool _isExecuting;

        private readonly CancellationToken _cancellationToken;

        private readonly BlockingCollection<Task> _taskQueue;

        private readonly Thread _singleThread;
        public override int MaximumConcurrencyLevel => 1;

        public SingleThreadTaskScheduler(CancellationToken cancellationToken) {
            this._cancellationToken = cancellationToken;
            this._taskQueue = new(new ConcurrentQueue<Task>());
            this._singleThread = new(RunOnCurrentThread) { Name = "STTS Thread", IsBackground = true };
            this._singleThread.Start();
        }

        public void Abort() {
            _singleThread.Abort();
        }

        private void RunOnCurrentThread() {
            _isExecuting = true;

            try {
                foreach (var task in _taskQueue.GetConsumingEnumerable(_cancellationToken)) {
                    TryExecuteTask(task);
                }
            } catch (OperationCanceledException) {
                // Do nothing if the operation is cancelled
            } finally {
                _isExecuting = false;
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks() => _taskQueue.ToList();

        protected override void QueueTask(Task task) {
            try {
                _taskQueue.Add(task, _cancellationToken);
            } catch (OperationCanceledException) {
                // Do nothing if the operation is cancelled
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
            // We'd need to remove the task from queue if it was already queued. 
            // That would be too hard.
            if (taskWasPreviouslyQueued) { return false; }
            return _isExecuting && TryExecuteTask(task);
        }
    }

}