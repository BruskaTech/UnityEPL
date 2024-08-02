//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

using UnityEPL.Utilities;
using UnityEPL.Networking;
using UnityEPL.Threading;
using UnityEPL.Extensions;

namespace UnityEPL.GUI {

    [AddComponentMenu("UnityEPL/Internal/TextDisplayer")]
    public class TextDisplayer : SingletonEventMonoBehaviour<TextDisplayer> {
        protected override void AwakeOverride() {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Subscribe to this event to be notified of changes in the displayed text.
        /// 
        /// Single string argument is the new text which is being displayed.
        /// </summary>
        public delegate void TextDisplayed(string text);
        public static TextDisplayed OnText;

        /// <summary>
        /// Drag a scripted event reporter here to have this monobehavior automatically report when text is displayed or cleared.
        /// </summary>
        public EventReporter eventReporter = null;

        /// <summary>
        /// These text elements will all be updated when this monobehaviors public methods are used.
        /// </summary>
        public TextMeshProUGUI textElement;
        public TextMeshProUGUI titleElement;

        private Color[] originalColors;

        protected void Start() {
            originalColors = new Color[2];
            originalColors[0] = textElement.color;
            originalColors[1] = titleElement.color;
        }

        /// <summary>
        /// Hides the Text display by deactivating it
        /// </summary>
        public void Hide() {

            Do(HideHelper);
        }
        public void HideTS() {
            DoTS(HideHelper);
        }
        protected void HideHelper() {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Is the TextDisplayer Active
        /// </summary>
        /// <returns>if the TextDisplayer is active</returns>
        public bool IsActive() {
            return DoGet(IsActiveHelper);
        }
        public async Task<bool> IsActiveTS() {
            return await DoGetTS(IsActiveHelper);
        }
        protected Bool IsActiveHelper() {
            return gameObject.activeSelf;
        }

        /// <summary>
        /// Returns the color of the assigned text elements to whatever they were when this monobehavior initialized (usually scene load).
        /// </summary>
        public void OriginalColor() {
            Do(OriginalColorHelper);
        }
        public void OriginalColorTS() {
            DoTS(OriginalColorHelper);
        }     
        protected void OriginalColorHelper() {
            textElement.color = originalColors[0];
            titleElement.color = originalColors[1];
            if (eventReporter != null)
                eventReporter.LogTS("restore original text color", new());
        }


        /// <summary>
        /// First argument is a description of the text to be displayed.  This is logged if the wordEventReporter field is populated in the editor.
        /// 
        /// Second argument is the text to be displayed.  All elements in the textElements field will be updated.  This is logged in the "data" field under "displayed text" if the wordEventReporter field is populated in the editor.
        /// </summary>
        /// <param name="description">Description.</param>
        /// <param name="text">Text.</param>
        public void DisplayText(string description, LangString text, float textFontSize = 0) {
            Do(DisplayTextHelper, description.ToNativeText(), text.ToNativeText(), textFontSize);
        }
        public void DisplayTextTS(string description, LangString text, float textFontSize = 0) {
            DoTS(DisplayTextHelper, description.ToNativeText(), text.ToNativeText(), textFontSize);
        }
        protected void DisplayTextHelper(NativeText description, NativeText text, float textFontSize) {
            var displayedText = text.ToString();
            if (OnText != null)
                OnText(displayedText);

            if (textElement == null) {
                return;
            }

            if (textFontSize > 0) {
                textElement.enableAutoSizing = false;
                textElement.fontSize = textFontSize;
            }

            textElement.text = displayedText;
            Dictionary<string, object> dataDict = new() {
                { "displayed text", displayedText },
            };
            gameObject.SetActive(true);
            if (eventReporter != null)
                eventReporter.LogTS(description.ToString(), dataDict);

            description.Dispose();
            text.Dispose();
        }

        public void DisplayTitle(string description, LangString title) {
            Do(DisplayTitleHelper, description.ToNativeText(), title.ToNativeText());
        }
        public void DisplayTitleTS(string description, LangString title) {
            DoTS(DisplayTitleHelper, description.ToNativeText(), title.ToNativeText());
        }
        protected void DisplayTitleHelper(NativeText description, NativeText title) {
            var displayedTitle = title.ToString();
            if (OnText != null)
                OnText(displayedTitle);

            if (titleElement == null) {
                return;
            }

            titleElement.text = displayedTitle;
            Dictionary<string, object> dataDict = new() {
                { "displayed title", displayedTitle },
            };
            gameObject.SetActive(true);
            if (eventReporter != null)
                eventReporter.LogTS(description.ToString(), dataDict);

            description.Dispose();
            title.Dispose();
        }

        public void Display(string description, LangString title, LangString text, float textFontSize = 0) {
            Do(DisplayHelper, description.ToNativeText(), title.ToNativeText(), text.ToNativeText(), textFontSize);
        }
        public void DisplayTS(string description, LangString title, LangString text, float textFontSize = 0) {
            DoTS(DisplayHelper, description.ToNativeText(), title.ToNativeText(), text.ToNativeText(), textFontSize);
        }
        protected void DisplayHelper(NativeText description, NativeText title, NativeText text, float textFontSize) {
            var displayedTitle = title.ToString();
            var displayedText = text.ToString();
            if (OnText != null) {
                OnText(title.ToString());
                OnText(text.ToString());
            }
                
            if (titleElement == null || textElement == null) {
                return;
            }

            if (textFontSize > 0) {
                textElement.enableAutoSizing = false;
                textElement.fontSize = textFontSize;
            }

            titleElement.text = displayedTitle;
            textElement.text = displayedText;
            Dictionary<string, object> dataDict = new() {
                { "displayed title", displayedTitle },
                { "displayed text", displayedText },
            };
            gameObject.SetActive(true);
            if (eventReporter != null)
                eventReporter.LogTS(description.ToString(), dataDict);

            description.Dispose();
            title.Dispose();
            text.Dispose();
        }

        public async Task DisplayForTask(string description, LangString title, LangString text, float textFontSize, CancellationToken ct, Func<CancellationToken, Task> func) {
            // Remember the current state
            var activeOld = IsActive();
            var titleOld = titleElement.text;
            var textOld = textElement.text;
            var textAutoSizingOld = textElement.enableAutoSizing;

            // Display the new text and wait for the task to complete
            Display(description, title, text, textFontSize);
            await Awaitable.NextFrameAsync();
            await func(ct);

            // Put the old state back
            titleElement.text = titleOld;
            textElement.text = textOld;
            textElement.enableAutoSizing = textAutoSizingOld;
            if (!activeOld) { Hide(); }
        }
        public async Task DisplayForTask(string description, LangString title, LangString text, CancellationToken ct, Func<CancellationToken, Task> func) {
            await DisplayForTask(description, title, text, 0, ct, func);
        }


        /// <summary>
        /// Clears the text of all textElements.  This is logged if the wordEventReporter field is populated in the editor.
        /// </summary>
        public void ClearText() {
            Do(ClearTextHelper);
        }
        public void ClearTextTS() {
            DoTS(ClearTextHelper);
        }
        protected void ClearTextHelper() {
            textElement.text = "";
            textElement.enableAutoSizing = true;
            if (eventReporter != null)
                eventReporter.LogTS("text display cleared", new());
        }
       
        public void ClearTitle() {
            Do(ClearTitleHelper);
        }
        public void ClearTitleTS() {
            DoTS(ClearTitleHelper);
        }
        protected void ClearTitleHelper() {
            titleElement.text = "";
            if (eventReporter != null)
                eventReporter.LogTS("title display cleared", new());
        }

        public void ClearOnly() {
            Do(ClearOnlyHelper);
        }
        public void ClearOnlyTS() {
            DoTS(ClearOnlyHelper);
        }
        protected void ClearOnlyHelper() {
            titleElement.text = "";
            textElement.text = "";
            textElement.enableAutoSizing = true;
            if (eventReporter != null)
                eventReporter.LogTS("title display cleared", new());
        }

        public void Clear() {
            Do(ClearHelper);
        }
        public void ClearTS() {
            DoTS(ClearHelper);
        }
        protected void ClearHelper() {
            ClearOnlyHelper();
            HideHelper();
        }

        /// <summary>
        /// Changes the color of all textElements.  This is logged if the wordEventReporter field is populated in the editor.
        /// </summary>
        /// <param name="newColor">New color.</param>
        public void ChangeColor(Color newColor) {
            Do(ChangeColorHelper, newColor);
        }
        public void ChangeColorTS(Color newColor) {
            DoTS(ChangeColorHelper, newColor);
        }
        protected void ChangeColorHelper(Color newColor) {
            textElement.color = newColor;
            Dictionary<string, object> dataDict = new();
            dataDict.Add("new color", newColor.ToString());
            if (eventReporter != null)
                eventReporter.LogTS("text color changed", dataDict);
        }

        /// <summary>
        /// Returns the current text being displayed on the first textElement.  Throws an error if there are no textElements.
        /// </summary>
        public string CurrentText() {
            var text = DoGet(CurrentTextHelper);
            var ret = text.ToString();
            text.Dispose();
            return ret;
        }
        public async Task<string> CurrentTextTS() {
            var text = await DoGetTS(CurrentTextHelper);
            var ret = text.ToString();
            text.Dispose();
            return ret;
        }
        protected NativeText CurrentTextHelper() {
            if (textElement == null)
                throw new UnityException("There aren't any text elements assigned to this TextDisplayer.");
            return textElement.text.ToNativeText();
        }

        /// <summary>
        /// Display a message and wait for keypress
        /// </summary>
        /// <param name="description"></param>
        /// <param name="displayText"></param>
        /// <param name="displayText"></param>
        /// <returns></returns>
        public async Task<KeyCode> PressAnyKey(string description, LangString displayText) {
            return await PressAnyKey(description, LangStrings.Blank(), displayText);
        }
        public async Task<KeyCode> PressAnyKeyTS(string description, LangString displayText) {
            return await PressAnyKeyTS(description, LangStrings.Blank(), displayText);
        }
        public async Task<KeyCode> PressAnyKey(string description, LangString displayTitle, LangString displayText) {
            return await DoGet(PressAnyKeyHelper, description.ToNativeText(), displayTitle.ToNativeText(), displayText.ToNativeText());
        }
        public async Task<KeyCode> PressAnyKeyTS(string description, LangString displayTitle, LangString displayText) {
            return await DoGetTS(PressAnyKeyHelper, description.ToNativeText(), displayTitle.ToNativeText(), displayText.ToNativeText());
        }
        protected async Task<KeyCode> PressAnyKeyHelper(NativeText description, NativeText displayTitle, NativeText displayText) {
            _ = manager.hostPC?.SendStateMsgTS(HostPcStateMsg.WAITING());
            // TODO: JPB: (needed) Add Ramulator to match this
            var displayTitleStr = LangStrings.GenForCurrLang(displayTitle.ToStringAndDispose());
            var displayTextStr = LangStrings.GenForCurrLang(displayText.ToStringAndDispose());
            Display($"{description.ToStringAndDispose()} (press any key prompt)", displayTitleStr, displayTextStr);
            var keyCode = await InputManager.Instance.WaitForKey();
            Clear();
            return keyCode;
        }

        public float FindMaxFittingFontSize(List<LangString> strings) {
            List<string> strs = strings.Select(str => str.ToString()).ToList();
            return DoGet(FindMaxFittingFontSizeHelper, strs.ToNativeArray());
        }
        public async Task<float> FindMaxFittingFontSizeTS(List<LangString> strings) {
            List<string> strs = strings.Select(str => str.ToString()).ToList();
            return await DoGetTS(FindMaxFittingFontSizeHelper, strs.ToNativeArray());
        }
        protected float FindMaxFittingFontSizeHelper(NativeArray<NativeText> strings) {
            // Remember the current state
            var activeOld = IsActive();
            var titleOld = titleElement.text;
            var textOld = textElement.text;
            var textAutoSizingOld = textElement.enableAutoSizing;

            // Find the max fitting font size
            gameObject.SetActive(true);
            var size = textElement.FindMaxFittingFontSize(strings.ToListAndDispose());

            // Put the old state back
            titleElement.text = titleOld;
            textElement.text = textOld;
            textElement.enableAutoSizing = textAutoSizingOld;
            if (!activeOld) { Hide(); }
            
            return size;
        }
    }

}