//Copyright (c) 2024 Columbia University (James Bruska)

//This file is part of CityBlock.
//CityBlock is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//CityBlock is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with CityBlock. If not, see <https://www.gnu.org/licenses/>.


using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEPL.ExternalDevices {

    public class PhotoDiodeSyncBox : SyncBox {
        [SerializeField] private RectTransform imageRect;
        [SerializeField] private Image image;

        private Color offColor;
        private Color onColor;

        private const int TIME_BETWEEN_PULSES_MS = 1000;
        private const int TIME_BETWEEN_PULSES_JITTER_MS = 200;
        private const int TIME_BETWEEN_PULSES_MIN_MS = TIME_BETWEEN_PULSES_MS - TIME_BETWEEN_PULSES_JITTER_MS;
        private const int TIME_BETWEEN_PULSES_MAX_MS = TIME_BETWEEN_PULSES_MS + TIME_BETWEEN_PULSES_JITTER_MS;

        public void InitImage() {
            // Set the name of the GameObject
            gameObject.name = "PhotoDiodeSyncBox";

            // Create the new UI Canvas GameObject, the scaler, and the raycaster
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Set render mode to Overlay
            canvas.sortingOrder = 32767; // Sort order
            CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; // Constant pixel size mode
            GraphicRaycaster _ = gameObject.AddComponent<GraphicRaycaster>();

            // Create the new UI Image GameObject
            GameObject imageObject = new GameObject("PhotoDiodeImage");
            imageObject.transform.SetParent(gameObject.transform); // Set the canvas as the parent
            
            // Create the Image component
            image = imageObject.AddComponent<Image>();
            image.color = Color.black; // Set the color to black
            image.type = Image.Type.Sliced; // Sliced image type
            image.raycastTarget = true;     // Enable raycasting

            // Create the RectTransform (position, width, height, and anchors)
            imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchoredPosition = new Vector2(0, 0);
            imageRect.sizeDelta = new Vector2(100, 100); // Set size
            imageRect.anchorMin = new Vector2(0, 1); // Set anchor to top-left
            imageRect.anchorMax = new Vector2(0, 1);
            imageRect.pivot = new Vector2(0, 1); // Pivot point to top-left
            imageRect.localScale = Vector3.one; // Scale remains at 1        

            // Configure the Canvas Renderer
            imageObject.GetComponent<CanvasRenderer>().cullTransparentMesh = false; // Don't cull transparent mesh
        }

        public override Task Init() {
            InitImage();

            // Set the image size
            if (Config.photoDiodeSyncBoxImageHeightInch <= 0) {
                ErrorNotifier.ErrorTS(new Exception($"Config variable photoDiodeImageHeightInch ({Config.photoDiodeSyncBoxImageHeightInch}) must be greater than 0"));
            } else if (Config.photoDiodeSyncBoxImageWidthInch <= 0) {
                ErrorNotifier.ErrorTS(new Exception($"Config variable photoDiodeImageWidthInch ({Config.photoDiodeSyncBoxImageWidthInch}) must be greater than 0"));
            }
            float dpi = Screen.dpi;
            if (dpi == 0) { dpi = 96; }
            imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dpi * Config.photoDiodeSyncBoxImageWidthInch);
            imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dpi * Config.photoDiodeSyncBoxImageHeightInch);

            // Set the image position
            var imagePosition = Config.photoDiodeSyncBoxImagePosition;
            if (imagePosition.Length != 2) {
                ErrorNotifier.ErrorTS(new Exception("Config variable photoDiodeImagePosition must have exactly 2 elements"));
            } else if (imagePosition[0] > 1 || imagePosition[1] > 1) {
                ErrorNotifier.ErrorTS(new Exception("Config variable photoDiodeImagePosition elements must be 0 or 1"));
            }
            var imagePositionVec2 = new Vector2(imagePosition[0], imagePosition[1]);
            imageRect.anchorMin = imagePositionVec2;
            imageRect.anchorMax = imagePositionVec2;
            imageRect.pivot = imagePositionVec2;
            imageRect.anchoredPosition = Vector2.zero;

            // Set the image colors
            if (!ColorUtility.TryParseHtmlString(Config.photoDiodeSyncBoxImageOffColor, out offColor)) {
                ErrorNotifier.ErrorTS(new Exception($"Config variable photoDiodeImageOffColor ({Config.photoDiodeSyncBoxImageOffColor}) is not a valid color"));
            } else if (!ColorUtility.TryParseHtmlString(Config.photoDiodeSyncBoxImageOnColor, out onColor)) {
                ErrorNotifier.ErrorTS(new Exception($"Config variable photoDiodeImageOnColor ({Config.photoDiodeSyncBoxImageOnColor}) is not a valid color"));
            }

            return Task.CompletedTask;
        }


        public override async Task Pulse() {
            image.color = onColor;
            await manager.Delay(1000);
            image.color = offColor;
            int delayMs = Utilities.Random.Rnd.Next(TIME_BETWEEN_PULSES_MIN_MS, TIME_BETWEEN_PULSES_MAX_MS);
            await manager.Delay(delayMs);
        }

        public override Task TearDown() {
            return Task.CompletedTask;
        }
    }

}

