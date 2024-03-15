//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace UnityEPL {

    public class ErrorNotifier : SingletonEventMonoBehaviour<ErrorNotifier> {
        protected override void AwakeOverride() {
            gameObject.SetActive(false);
        }

        public static void ErrorTS(Exception exception) {
            if (!IsInstatiated) {
                throw new Exception("THIS SHOULD NOT HAPPEN! ErrorNotifier was accessed before it's awake method has been called.", exception);
            }

            Instance.DoTS(() => { Instance.ErrorHelper(new Mutex<Exception>(exception)); });
            throw new Exception("ErrorNotifier", exception);
        }
        protected void ErrorHelper(Mutex<Exception> exception) {
            try {
                Exception e = exception.Get();
                // Only show first error on screen, but report all errors
                if (!gameObject.activeSelf) {
                    gameObject.SetActive(true);
                    var msg = e.Message == "" ? e.GetType().Name : e.Message;
                    TextDisplayer.Instance.Display("Error", "<color=red><b>Error</b></color>", msg);
                    Debug.Log($"Error: {msg}\n{e.StackTrace}");
                }
                manager.eventReporter.LogTS("Error", new() {
                    { "message", e.Message },
                    { "stackTrace", e.StackTrace } });
                manager.PauseTS(true);
            } catch (Exception e) {
                Debug.Log("UNSAVEABLE ERROR IN ErrorHelper... Quitting...\n" + e);
                Debug.Assert(gameObject != null);
                Debug.Assert(manager != null);
                Debug.Assert(manager.eventReporter != null);
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

            Instance.DoTS(Instance.WarningHelper, exception.Message.ToNativeText(), exception.StackTrace.ToNativeText());
        }
        protected void WarningHelper(NativeText message, NativeText stackTrace) {
            TextDisplayer.Instance.Display("Warning", "<color=yellow><b>Warning</b></color>", message.ToString());
            Debug.Log($"Warning: {message}\n{stackTrace}");
            manager.eventReporter.LogTS("Warning", new() {
                { "message", message.ToString() },
                { "stackTrace", stackTrace.ToString() } });
            manager.PauseTS(true);
            message.Dispose();
            stackTrace.Dispose();
        }
    }
}