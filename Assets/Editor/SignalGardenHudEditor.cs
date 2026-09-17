using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SignalGarden.Editor
{
    public static class SignalGardenHudEditor
    {
        private static readonly Color PanelColor = new Color(0.035f, 0.085f, 0.092f, 0.96f);
        private static readonly Color ButtonColor = new Color(0.10f, 0.23f, 0.22f, 0.98f);
        private static readonly Color ButtonHoverColor = new Color(0.16f, 0.36f, 0.31f, 1f);
        private static readonly Color TextColor = new Color(0.94f, 0.95f, 0.88f, 1f);
        private static readonly Color MutedTextColor = new Color(0.78f, 0.86f, 0.81f, 1f);
        private static readonly Color TealColor = new Color(0.42f, 0.86f, 0.74f, 1f);
        private static readonly Color OverlayColor = new Color(0.015f, 0.035f, 0.044f, 0.78f);

        [MenuItem("Signal Garden/Install UGUI HUD")]
        public static void InstallHudInActiveScene()
        {
            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            if (game == null)
            {
                throw new InvalidOperationException("Signal Garden Game is required before installing the HUD.");
            }

            InstallInScene(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            Debug.Log("Signal Garden UGUI HUD installed in the active scene.");
        }

        [MenuItem("Signal Garden/Install UGUI HUD in SignalGarden Scene")]
        public static void InstallHudInSignalGardenScene()
        {
            const string scenePath = "Assets/Scenes/SignalGarden.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException("SignalGarden scene could not be opened at " + scenePath + ".");
            }

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            if (game == null)
            {
                throw new InvalidOperationException("Signal Garden Game is required before installing the HUD.");
            }

            InstallInScene(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Signal Garden UGUI HUD installed in " + scenePath + ".");
        }

        [MenuItem("Signal Garden/Validate UGUI HUD")]
        public static void ValidateHudInActiveScene()
        {
            ValidateInScene();
            Debug.Log("Signal Garden UGUI HUD validation passed.");
        }

        public static void ValidateInScene()
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<SignalGardenHud>();
            if (hud == null)
            {
                throw new InvalidOperationException("Signal Garden UGUI HUD is missing from the scene.");
            }

            var canvas = hud.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.sortingOrder != 50)
            {
                throw new InvalidOperationException("Signal Garden UGUI HUD requires a ScreenSpaceOverlay Canvas at sorting order 50.");
            }

            if (hud.GetComponent<CanvasScaler>() == null || hud.GetComponent<GraphicRaycaster>() == null)
            {
                throw new InvalidOperationException("Signal Garden UGUI HUD requires CanvasScaler and GraphicRaycaster components.");
            }

            var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null || eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                throw new InvalidOperationException("Signal Garden requires an EventSystem with InputSystemUIInputModule for keyboard focus.");
            }

            if (!hud.HasCompleteBinding)
            {
                throw new InvalidOperationException("Signal Garden UGUI HUD has an incomplete serialized binding set.");
            }
        }

        public static SignalGardenHud EnsureInScene()
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<SignalGardenHud>();
            if (existing != null)
            {
                return existing;
            }

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            if (game == null)
            {
                throw new InvalidOperationException("Signal Garden Game is required before installing the HUD.");
            }

            return InstallInScene(game);
        }

        public static SignalGardenHud InstallInScene(SignalGardenGame game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            var previous = GameObject.Find("Signal Garden HUD");
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous);
            }

            EnsureEventSystem(game.gameObject.scene);

            var canvasObject = new GameObject("Signal Garden HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SignalGardenHud));
            SceneManager.MoveGameObjectToScene(canvasObject, game.gameObject.scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvas.pixelPerfect = false;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var objectivePanel = CreatePanel(canvasRect, "Objective Panel", PanelColor, true);
            SetTopLeft(objectivePanel.rectTransform, 26f, 26f, 560f, 164f);
            var objectiveAccent = CreatePanel(objectivePanel.rectTransform, "Objective Accent", TealColor, false);
            SetAnchored(objectiveAccent.rectTransform, new Vector2(0f, 0.12f), new Vector2(0f, 0.88f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(2f, 0f));
            CreateText(objectivePanel.rectTransform, "Eyebrow", "SIGNAL GARDEN  /  FIELD STUDY 01", font, 13, TealColor, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-44f, 26f), new Vector2(22f, -14f));
            CreateText(objectivePanel.rectTransform, "Title", "Wake the garden with one clear line.", font, 25, TextColor, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-44f, 48f), new Vector2(22f, -48f));
            CreateText(objectivePanel.rectTransform, "Instruction", "Drag coral to blue along the gold stones. WASD pans. Esc cancels or pauses.", font, 15, MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-44f, 58f), new Vector2(22f, -94f));

            var soundButton = CreateButton(canvasRect, "Sound Cues Button", "Sound cues: Off", font, 168f, 56f, new Vector2(-378f, -26f));
            var motionButton = CreateButton(canvasRect, "Reduced Motion Button", "Motion: Full", font, 172f, 56f, new Vector2(-198f, -26f));
            var soundButtonLabel = soundButton.GetComponentInChildren<Text>(true);
            var motionButtonLabel = motionButton.GetComponentInChildren<Text>(true);

            var statusPanel = CreatePanel(canvasRect, "Status Panel", PanelColor, true);
            SetBottomStretch(statusPanel.rectTransform, 26f, 26f, 26f, 76f);
            var phaseLabel = CreateText(statusPanel.rectTransform, "Phase Label", "OBSERVE  /  FIND THE PATH", font, 12, TealColor, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-238f, 25f), new Vector2(22f, -10f));
            var statusLabel = CreateText(statusPanel.rectTransform, "Status Label", "Ready. Drag from the coral source to begin.", font, 16, TextColor, FontStyle.Normal, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(-238f, -42f), new Vector2(22f, 4f));
            var retryButton = CreateButton(statusPanel.rectTransform, "Try Again Button", "Try again", font, 158f, 52f, Vector2.zero);
            SetRightCenter(retryButton.GetComponent<RectTransform>(), 18f, 52f, 0f);

            var overlay = CreatePanel(canvasRect, "State Overlay", OverlayColor, true);
            SetStretch(overlay.rectTransform, 0f, 0f, 0f, 0f);
            var overlayCard = CreatePanel(overlay.rectTransform, "State Card", PanelColor, true);
            SetCenter(overlayCard.rectTransform, 480f, 310f);
            var overlayEyebrow = CreateText(overlayCard.rectTransform, "Overlay Eyebrow", "GARDEN AT REST", font, 13, TealColor, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-68f, 34f), new Vector2(34f, -22f));
            var overlayTitle = CreateText(overlayCard.rectTransform, "Overlay Title", "Paused", font, 27, TextColor, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-68f, 52f), new Vector2(34f, -65f));
            var overlayBody = CreateText(overlayCard.rectTransform, "Overlay Body", "Your turn is safe. Resume, or reset the garden for the next player.", font, 16, MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-68f, 80f), new Vector2(34f, -130f));
            var overlayPrimaryButton = CreateButton(overlayCard.rectTransform, "Overlay Primary Button", "Resume  /  Esc", font, 412f, 52f, Vector2.zero);
            SetBottomLeft(overlayPrimaryButton.GetComponent<RectTransform>(), 34f, 38f, 52f, 412f);
            var overlaySecondaryButton = CreateButton(overlayCard.rectTransform, "Overlay Secondary Button", "Restart this turn", font, 412f, 42f, Vector2.zero);
            SetBottomLeft(overlaySecondaryButton.GetComponent<RectTransform>(), 34f, 0f, 42f, 412f);
            overlay.gameObject.SetActive(false);

            var hud = canvasObject.GetComponent<SignalGardenHud>();
            hud.Configure(
                game,
                objectivePanel.gameObject,
                phaseLabel,
                statusLabel,
                retryButton,
                soundButton,
                motionButton,
                soundButtonLabel,
                motionButtonLabel,
                overlay.gameObject,
                overlayEyebrow,
                overlayTitle,
                overlayBody,
                overlayPrimaryButton,
                overlaySecondaryButton);
            var gameSerialized = new SerializedObject(game);
            var hudProperty = gameSerialized.FindProperty("hud");
            if (hudProperty != null)
            {
                hudProperty.objectReferenceValue = hud;
                gameSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(canvasObject);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(game);
            return hud;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                var legacy = eventSystem.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    UnityEngine.Object.DestroyImmediate(legacy);
                }

                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }

                return;
            }

            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventObject, scene);
        }

        private static Image CreatePanel(Transform parent, string name, Color color, bool raycastTarget)
        {
            var image = CreateChild(parent, name).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, float width, float height, Vector2 anchoredPosition)
        {
            var buttonObject = CreateChild(parent, name);
            var image = buttonObject.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            var button = buttonObject.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHoverColor;
            colors.pressedColor = TealColor;
            colors.selectedColor = ButtonHoverColor;
            colors.disabledColor = new Color(ButtonColor.r, ButtonColor.g, ButtonColor.b, 0.45f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            SetAnchored(buttonObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(width, height), anchoredPosition);
            CreateText(buttonObject, "Label", label, font, 15, TextColor, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return button;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            Font font,
            int fontSize,
            Color color,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta,
            Vector2 anchoredPosition)
        {
            var child = CreateChild(parent, name);
            var text = child.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;
            SetAnchored(child, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
            text.text = value;
            return text;
        }

        private static RectTransform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetBottomStretch(RectTransform rect, float left, float right, float bottom, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        private static void SetStretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetCenter(RectTransform rect, float width, float height)
        {
            SetAnchored(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(width, height), Vector2.zero);
        }

        private static void SetRightCenter(RectTransform rect, float right, float height, float verticalOffset)
        {
            SetAnchored(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(rect.sizeDelta.x, height), new Vector2(-right, verticalOffset));
        }

        private static void SetBottomLeft(RectTransform rect, float left, float bottom, float height, float width)
        {
            SetAnchored(rect, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(width, height), new Vector2(left, bottom));
        }
    }
}
