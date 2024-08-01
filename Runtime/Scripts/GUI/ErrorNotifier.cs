//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

using UnityEPL.Utilities;

namespace UnityEPL {

    public class ErrorNotifier : SingletonEventMonoBehaviour<ErrorNotifier> {
        protected override void AwakeOverride() {}

        bool errorSet = false;

        public static void ErrorTS(Exception exception) {
            if (!IsInstatiated) {
                throw new Exception("THIS SHOULD NOT HAPPEN! ErrorNotifier was accessed before it's awake method has been called.", exception);
            }

            Instance.DoTS(async () => { await Instance.ErrorHelper(new Mutex<Exception>(exception)); });
            throw new Exception("ErrorNotifier", exception);
        }
        protected async Task ErrorHelper(Mutex<Exception> exception) {
            try {
                Exception e = exception.Get();
                // Only show first error on screen, but report all errors
                if (!errorSet) {
                    errorSet = true;
                    var msg = e.Message == "" ? e.GetType().Name : e.Message;
                    msg += "\n\nPress Q to quit";
                    TextDisplayer.Instance.Display("Error", LangStrings.Error().Color("red"), LangStrings.GenForCurrLang(msg));
                    Debug.Log($"Error: {msg}\n{e.StackTrace}");
                }
                EventReporter.Instance.LogTS("Error", new() {
                    { "message", e.Message },
                    { "stackTrace", e.StackTrace } });
                await Awaitable.NextFrameAsync();
                manager.Pause(true);
                await InputManager.Instance.WaitForKey(KeyCode.Q, true);
                manager.Quit();
            } catch (Exception e) {
                Debug.Log("UNSAVEABLE ERROR IN ErrorHelper... Quitting...\n" + e);
                Debug.Assert(gameObject != null);
                Debug.Assert(manager != null);
                Debug.Assert(EventReporter.Instance != null);
                manager.Quit();
            }
        }

        public static void WarningTS(Exception exception) {
            if (exception.StackTrace == null) {
                try { // This is used to get the stack trace
                    throw exception;
                } catch (Exception e) {
                    exception = e;
                }
            }

            // TODO: JPB: (bug) What will happen to the error in WarningTS if it errors
            _ = Instance.DoWaitFor(Instance.WarningHelper, exception.Message.ToNativeText(), exception.StackTrace.ToNativeText());
        }
        // TODO: JPB: (feature) Implement WarningHelper
        protected Task WarningHelper(NativeText message, NativeText stackTrace) {
            manager.Pause(true);
            TextDisplayer.Instance.Display("Warning", LangStrings.Warning().Color("yellow"), LangStrings.GenForCurrLang(message.ToString()));
            Debug.Log($"Warning: {message}\n{stackTrace}");
            EventReporter.Instance.LogTS("Warning", new() {
                { "message", message.ToString() },
                { "stackTrace", stackTrace.ToString() } });
            message.Dispose();
            stackTrace.Dispose();

            // var keyCode = await InputManager.Instance.WaitForKey(new() {KeyCode.Y, KeyCode.N}, true);
            // if ()

            manager.Pause(false);
            return Task.CompletedTask;
        }
    }
}