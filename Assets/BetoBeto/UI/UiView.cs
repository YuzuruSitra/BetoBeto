using System;
using System.Collections.Generic;
using System.Linq;
using BetoBeto.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BetoBeto.UI
{
    public abstract class UiView : MonoBehaviour
    {
        protected RectTransform root;
        Font font;
        Sprite rounded;
        protected EventSystem events;
        InputSystemUIInputModule inputModule;
        InputActionAsset uiActions;
        Canvas uiCanvas;
        float textScale = -1;
        readonly List<InputActionReference> actionReferences = new List<InputActionReference>();
        protected static readonly Color Ink = Hex("253E51"), Muted = Hex("6A8090"), Cream = Hex("FFF8EA"), Pink = Hex("E8788B"), Mint = Hex("81CFC7");
        protected static readonly Color[] FruitColors = { Hex("E96778"), Hex("7C86C3"), Hex("EDAA55"), Hex("9BAF68") };
        protected void InitializeUi()
        {
            font = Resources.Load<Font>("Fonts/MPLUSRounded1c-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded = CreateRoundedSprite();
            var canvasObject = new GameObject("BetoBeto UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root = canvasObject.GetComponent<RectTransform>();
            uiCanvas = canvasObject.GetComponent<Canvas>(); uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            events = FindAnyObjectByType<EventSystem>();
            if (events == null) events = new GameObject("Event System", typeof(EventSystem)).GetComponent<EventSystem>();
            inputModule = events.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null) inputModule = events.gameObject.AddComponent<InputSystemUIInputModule>();
            uiActions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = uiActions.AddActionMap("Kitchen UI");
            var move = map.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddBinding("<Gamepad>/leftStick"); move.AddBinding("<Gamepad>/dpad");
            var submit = map.AddAction("Submit", InputActionType.Button, "<Gamepad>/buttonSouth");
            var point = map.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position", expectedControlLayout: "Vector2");
            var click = map.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton");
            inputModule.actionsAsset = uiActions;
            inputModule.move = Reference(move); inputModule.submit = Reference(submit);
            inputModule.point = Reference(point); inputModule.leftClick = Reference(click);
            inputModule.cancel = null;
            inputModule.deselectOnBackgroundClick = false;
            inputModule.moveRepeatDelay = .35f; inputModule.moveRepeatRate = .14f;
            inputModule.enabled = GamepadControls.BrowserReady;
        }
        InputActionReference Reference(InputAction action)
        {
            var reference = InputActionReference.Create(action); actionReferences.Add(reference); return reference;
        }
        protected void FocusScope(Transform scope, Selectable preferred = null)
        {
            Canvas.ForceUpdateCanvases();
            var controls = scope.GetComponentsInChildren<Selectable>().Where(s => s.IsActive() && s.IsInteractable()).ToArray();
            foreach (var control in controls)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = Neighbor(control, controls, Vector2.up);
                nav.selectOnDown = Neighbor(control, controls, Vector2.down);
                if (control is not Slider)
                {
                    nav.selectOnLeft = Neighbor(control, controls, Vector2.left);
                    nav.selectOnRight = Neighbor(control, controls, Vector2.right);
                }
                control.navigation = nav;
            }
            events.sendNavigationEvents = true;
            var selected = preferred != null && controls.Contains(preferred) ? preferred : controls.FirstOrDefault();
            events.SetSelectedGameObject(selected == null ? null : selected.gameObject);
        }
        static Selectable Neighbor(Selectable current, Selectable[] controls, Vector2 direction)
        {
            var rect = (RectTransform)current.transform;
            Vector2 center = rect.TransformPoint(rect.rect.center);
            float best = 0; Selectable next = null;
            foreach (var other in controls)
            {
                if (other == current) continue;
                var target = (RectTransform)other.transform;
                Vector2 delta = (Vector2)target.TransformPoint(target.rect.center) - center;
                float score = Vector2.Dot(delta, direction) / Mathf.Max(1, delta.sqrMagnitude);
                if (score > best) { best = score; next = other; }
            }
            return next;
        }
        protected void DisableNavigation()
        {
            events.sendNavigationEvents = false; events.SetSelectedGameObject(null);
        }
        void LateUpdate()
        {
            if (inputModule != null && inputModule.enabled != GamepadControls.BrowserReady) inputModule.enabled = GamepadControls.BrowserReady;
            if (uiCanvas != null && !Mathf.Approximately(textScale, uiCanvas.scaleFactor))
            {
                // A browser resize can change only the Canvas scale, leaving old low-resolution glyph meshes cached.
                textScale = uiCanvas.scaleFactor;
                foreach (var label in root.GetComponentsInChildren<Text>()) label.SetVerticesDirty();
            }
        }
        protected void SliderRow(RectTransform parent, string text, float y, float value, Action<float> changed)
        {
            Label(parent, text, new Vector2(50, y), new Vector2(160, 30), 19, Ink, FontStyle.Bold);
            var percent = Label(parent, Mathf.RoundToInt(value * 100) + "%", new Vector2(625, y), new Vector2(85, 30), 18, Ink, FontStyle.Bold, TextAnchor.MiddleRight);
            var sliderRoot = Box(parent, text + " Slider", new Vector2(206, y - 6), new Vector2(397, 22), Hex("E4E8E2"));
            var slider = sliderRoot.gameObject.AddComponent<Slider>();
            var fill = Fill(sliderRoot, Mint);
            slider.fillRect = fill.rectTransform;
            var handle = Box(sliderRoot, "Handle", Vector2.zero, new Vector2(24, 32), Ink);
            handle.pivot = new Vector2(.5f, .5f); handle.anchorMin = new Vector2(0, .5f); handle.anchorMax = new Vector2(1, .5f);
            slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = value;
            slider.onValueChanged.AddListener(v => { changed(v); percent.text = Mathf.RoundToInt(v * 100) + "%"; });
            sliderRoot.gameObject.AddComponent<PadSelection>();
        }
        protected GameObject Overlay(string name, float alpha)
        {
            var rect = Stretch(root, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.065f, .15f, .20f, alpha));
            rect.GetComponent<Image>().sprite = null;
            return rect.gameObject;
        }
        protected RectTransform CenterCard(Transform parent, string name, Vector2 size)
        {
            var card = Box(parent, name, Vector2.zero, size, Cream);
            card.anchorMin = card.anchorMax = new Vector2(.5f, .5f); card.pivot = new Vector2(.5f, .5f);
            return card;
        }
        protected RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color; image.sprite = rounded; image.type = Image.Type.Sliced;
            return rect;
        }
        protected RectTransform Stretch(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var rect = Box(parent, name, Vector2.zero, Vector2.zero, color);
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            return rect;
        }
        protected Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var go = new GameObject(text.Length > 20 ? text.Substring(0, 20) : text, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            var label = go.GetComponent<Text>(); label.font = font; label.text = text; label.fontSize = fontSize; label.color = color; label.fontStyle = style;
            label.alignment = anchor; label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = false;
            return label;
        }
        protected RectTransform Button(Transform parent, string text, Vector2 position, Vector2 size, Color background, Color foreground, Action clicked, int fontSize = 16)
        {
            var rect = Box(parent, text, position, size, background);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f); colors.pressedColor = new Color(.88f, .88f, .88f); button.colors = colors;
            button.onClick.AddListener(() => clicked());
            rect.gameObject.AddComponent<PadSelection>();
            Label(rect, text, Vector2.zero, size, fontSize, foreground, FontStyle.Bold, TextAnchor.MiddleCenter);
            return rect;
        }
        protected Image Fill(RectTransform parent, Color color)
        {
            var rect = Stretch(parent, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, color);
            rect.GetComponent<Image>().raycastTarget = false;
            return rect.GetComponent<Image>();
        }
        static Sprite CreateRoundedSprite()
        {
            const int size = 64; const float radius = 14;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "UI rounded rectangle" };
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0);
                float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0);
                float a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        }
        protected static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
        protected static string FormatTime(float elapsed) => $"{(int)elapsed / 60:00}:{(int)elapsed % 60:00}";
        protected virtual void OnDestroy()
        {
            if (rounded != null) { Destroy(rounded.texture); Destroy(rounded); }
            foreach (var reference in actionReferences) Destroy(reference);
            if (uiActions != null) { uiActions.Disable(); Destroy(uiActions); }
        }
    }
}
