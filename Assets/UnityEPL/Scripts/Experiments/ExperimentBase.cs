using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEPL {

    public struct KeyMsg {
        public string key;
        public bool down;

        public KeyMsg(string key, bool down) {
            this.key = key;
            this.down = down;
        }
    }

    public abstract class ExperimentBase<T> : SingletonEventMonoBehaviour<T>
            where T : ExperimentBase<T> {
        TaskCompletionSource<KeyMsg> tcs = new TaskCompletionSource<KeyMsg>();

        protected InputManager inputManager;

        public ExperimentBase() {
            this.inputManager = InputManager.Instance;
        }

        private bool endTrials = false;
        protected uint trialNum { get; private set; } = 0;

        protected abstract Task PreTrials();
        protected abstract Task TrialStates();
        protected abstract Task PostTrials();

        protected void EndTrials() {
            endTrials = true;
        }

        protected void Run() {
            _ = DoWaitFor(RunHelper);
        }

        protected async Task RunHelper() {
            await PreTrials();
            while (!endTrials) {
                trialNum++;
                await TrialStates();
            }
            await PostTrials();
            manager.QuitTS();
        }

        protected void LogExperimentInfo() {
            //write versions to logfile
            Dictionary<string, object> versionsData = new() {
                { "application version", Application.version },
                { "build date", BuildInfo.ToString() }, // compiler magic, gives compile date
                { "experiment version", Config.experimentName },
                { "logfile version", "0" },
                { "participant", Config.subject },
                { "session", Config.session },
            };

            manager.eventReporter.ReportScriptedEvent("session start", versionsData);
        }
    }

}