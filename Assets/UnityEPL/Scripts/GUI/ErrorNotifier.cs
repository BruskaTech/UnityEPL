using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEPL {

    public class ErrorNotifier : SingletonEventMonoBehaviour<ErrorNotifier> {
        protected override void AwakeOverride() {
            gameObject.SetActive(false);
        }

        public static void Error<T>(T exception) where T : Exception {
            if (exception.StackTrace == null) {
                try { // This is used to get the stack trace
                    throw exception;
                } catch (T e) {
                    exception = e;
                }
            }

            Instance.Do<StackString, StackString>(Instance.ErrorHelper, exception.Message, exception.StackTrace);
            throw exception;
        }
        protected void ErrorHelper(StackString message, StackString stackTrace) {
            gameObject.SetActive(true);
            var textDisplayer = gameObject.GetComponent<TextDisplayer>();
            textDisplayer.DisplayMB("Error", "Error", message);
            manager.eventReporter.ReportScriptedEvent("Error",
                new Dictionary<string, object>{
                    { "message", message },
                    { "stackTrace", stackTrace } });
            manager.Pause(true);
        }

        public static void Warning<T>(T exception) where T : Exception {
            if (exception.StackTrace == null) {
                try { // This is used to get the stack trace
                    throw exception;
                } catch (T e) {
                    exception = e;
                }
            }

            Instance.Do<StackString, StackString>(Instance.WarningHelper, exception.Message, exception.StackTrace);
        }
        protected void WarningHelper(StackString message, StackString stackTrace) {
            gameObject.SetActive(true);
            var textDisplayer = gameObject.GetComponent<TextDisplayer>();
            textDisplayer.DisplayMB("Warning", "Warning", message);
            manager.eventReporter.ReportScriptedEvent("Warning",
                new Dictionary<string, object>{
                    { "message", message },
                    { "stackTrace", stackTrace } });
            manager.Pause(true);
        }
    }
}