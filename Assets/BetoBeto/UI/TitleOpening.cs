using System;
using System.Collections;
using BetoBeto.Audio;
using BetoBeto.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BetoBeto.UI
{
    /// <summary>
    /// Opening movie played over the title screen, once per launch.
    /// The clip stays in StreamingAssets and is played by URL because WebGL cannot use VideoClip assets.
    /// </summary>
    public sealed class TitleOpening : MonoBehaviour
    {
        public const string ClipFile = "BetoBetoOpening.mp4";
        const float PrepareTimeout = 8f;
        const float FadeSeconds = .6f;
        const float HintDelay = 1.6f;
        const float SkipGuard = .3f;
        // The synthesized soundtrack is deliberately quiet, so the recorded movie needs about twice
        // the slider value to sit at the same loudness as the title music it replaces.
        const float MusicToMovie = 2f;

        static bool played;
        /// <summary>Play-mode tests that drive the title menu clear this so no movie holds up the screen.</summary>
        public static bool Enabled { get; set; } = true;
        public static bool IsPlaying { get; private set; }
        public static bool ShouldPlay => Enabled && !played;
        /// <summary>A fresh launch, and the movie's own play-mode test, may show the opening again.</summary>
        public static void Rewind() => played = false;
        public static string ClipUrl => Application.streamingAssetsPath + "/Video/" + ClipFile;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void NewLaunch() { played = false; Enabled = true; IsPlaying = false; }

        public static TitleOpening Show(Action finished = null)
        {
            // Built inactive so Awake cannot start before the caller's callback is in place.
            var host = new GameObject("Title opening");
            host.SetActive(false);
            var opening = host.AddComponent<TitleOpening>();
            opening.finished = finished;
            host.SetActive(true);
            return opening;
        }

        Action finished;
        CanvasGroup group;
        RawImage movie;
        AspectRatioFitter frame;
        Text hint;
        AudioSource speaker;
        VideoPlayer player;
        GameAudio bus;
        bool ended, broken, closing;

        void Awake()
        {
            played = true;
            IsPlaying = true;
            bus = GameAudio.Instance;
            Build();
            StartCoroutine(Run());
        }

        /// <summary>Ends the movie early; the title fades in exactly as it does after the last frame.</summary>
        public void Skip() => Close();

        void Build()
        {
            // Its own overlay canvas: the menu design canvas is letterboxed to 1600 x 900, and the movie
            // has to cover the whole window, bars included.
            var canvasObject = new GameObject("Opening canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            group = canvasObject.GetComponent<CanvasGroup>();

            var curtain = Stretch(canvasObject.transform, "Curtain");
            var curtainImage = curtain.gameObject.AddComponent<Image>();
            curtainImage.color = Color.black;
            curtainImage.raycastTarget = true; // swallow clicks aimed at the title behind the movie

            var movieRect = Stretch(curtain, "Movie");
            movie = movieRect.gameObject.AddComponent<RawImage>();
            movie.color = Color.clear; // stays invisible until the first decoded frame is ready
            movie.raycastTarget = false;
            frame = movieRect.gameObject.AddComponent<AspectRatioFitter>();
            frame.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            frame.aspectRatio = 16f / 9f;

            hint = BuildHint(curtain);
            hint.enabled = false;
        }

        static RectTransform Stretch(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static Text BuildHint(Transform parent)
        {
            var rect = new GameObject("Skip hint", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-42, 34);
            rect.sizeDelta = new Vector2(420, 34);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.Load<Font>("Fonts/MPLUSRounded1c-Medium");
            if (label.font == null) label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "ボタンかクリックでスキップ";
            label.fontSize = 20;
            label.color = new Color(1, 1, 1, .72f);
            label.alignment = TextAnchor.MiddleRight;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            return label;
        }

        void SetUpPlayer()
        {
            speaker = gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.loop = false;
            speaker.volume = bus == null ? 1 : Mathf.Clamp01(bus.MusicVolume * MusicToMovie);

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = false;
            player.skipOnDrop = true;
            player.waitForFirstFrame = true;
            player.source = VideoSource.Url;
            player.url = ClipUrl;
            // APIOnly hands out the player's own texture, so no RenderTexture has to be sized up front.
            player.renderMode = VideoRenderMode.APIOnly;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.controlledAudioTrackCount = 1;
            player.EnableAudioTrack(0, true);
            player.SetTargetAudioSource(0, speaker);
            player.errorReceived += OnError;
            player.loopPointReached += OnEnded;
        }

        void OnError(VideoPlayer source, string message)
        {
            broken = true;
            Debug.LogWarning("オープニング映像を再生できません: " + message);
        }
        void OnEnded(VideoPlayer source) => ended = true;

        IEnumerator Run()
        {
            yield return null; // let the title finish building behind the black curtain
            if (closing) yield break;
            SetUpPlayer();
            player.Prepare();
            for (float waited = 0; !player.isPrepared && !broken && !closing && waited < PrepareTimeout; waited += Time.unscaledDeltaTime)
                yield return null;
            if (broken || closing || !player.isPrepared)
            {
                if (!closing && !broken) Debug.LogWarning("オープニング映像の準備が終わりませんでした: " + ClipUrl);
                Close();
                yield break;
            }
            // WebGL refuses to start audio until the page has seen the template's start click.
            while (!GamepadControls.BrowserReady && !closing) yield return null;
            if (closing) yield break;

            if (player.width > 0 && player.height > 0) frame.aspectRatio = (float)player.width / player.height;
            movie.texture = player.texture;
            movie.color = Color.white;
            if (bus != null) bus.MuteMusic(true);
            player.Play();

            float elapsed = 0, limit = (float)player.length + 2f;
            while (!closing && !ended && !broken && elapsed < limit)
            {
                if (movie.texture != player.texture) movie.texture = player.texture;
                elapsed += Time.unscaledDeltaTime;
                hint.enabled = elapsed > HintDelay;
                if (elapsed > SkipGuard && SkipPressed()) break;
                yield return null;
            }
            Close();
        }

        static bool SkipPressed()
        {
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame
                || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame
                || pad.startButton.wasPressedThisFrame)) return true;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        void Close()
        {
            if (closing) return;
            closing = true;
            StartCoroutine(FadeIntoTitle());
        }

        IEnumerator FadeIntoTitle()
        {
            // The title soundtrack comes back up while the movie fades away.
            if (bus != null) bus.MuteMusic(false);
            float from = speaker == null ? 0 : speaker.volume;
            for (float t = 0; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                float remaining = 1 - t / FadeSeconds;
                group.alpha = remaining;
                if (speaker != null) speaker.volume = from * remaining;
                yield return null;
            }
            var callback = finished;
            finished = null;
            Destroy(gameObject);
            callback?.Invoke();
        }

        void OnDestroy()
        {
            IsPlaying = false;
            if (player != null)
            {
                player.errorReceived -= OnError;
                player.loopPointReached -= OnEnded;
                player.Stop();
            }
            // A scene change during the movie must not leave the soundtrack silent.
            if (bus != null) bus.MuteMusic(false);
        }
    }
}
