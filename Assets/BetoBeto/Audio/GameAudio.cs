using UnityEngine;

namespace BetoBeto.Audio
{
    /// <summary>Small original synthesized placeholder soundtrack; no external audio assets required.</summary>
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }
        public static GameAudio GetOrCreate()
        {
            if (Instance != null) return Instance;
            return new GameObject("BetoBeto Audio").AddComponent<GameAudio>();
        }
        AudioSource music;
        AudioSource effects;
        AudioClip scare, scareFull, scareReady, splash, crunch, warning, clear, wall, slide, jelly, scone, cookieBreak, chocolate, iceWall;
        readonly AudioClip[] chainNotes = new AudioClip[8];
        public float MusicVolume { get; private set; }
        public float EffectsVolume { get; private set; }
        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            music = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            MusicVolume = PlayerPrefs.GetFloat("BetoBeto.Music", .32f);
            EffectsVolume = PlayerPrefs.GetFloat("BetoBeto.Sfx", .65f);
            music.volume = MusicVolume;
            effects.volume = EffectsVolume;
            scare = JuiceSound("Ghost boo", .3f, 480, .35f, .08f);
            scareFull = JuiceSound("Ghost big boo", .5f, 340, .22f, .25f);
            scareReady = Tone("Scare charged", 880, .2f, 1.5f);
            splash = Tone("Drool", 210, .24f, .4f);
            crunch = JuiceSound("Shredder crunch", .3f, 175, .3f, .7f);
            wall = JuiceSound("Cookie wall thump", .25f, 135, .35f, .43f);
            slide = JuiceSound("Slippery whoosh", .21f, 430, 1.8f, .27f);
            jelly = JuiceSound("Jelly boing", .34f, 160, 3.2f, .04f);
            scone = Tone("Scone ricochet", 920, .2f, .48f);
            cookieBreak = JuiceSound("Cookie crumble", .35f, 240, .23f, 1);
            chocolate = JuiceSound("Chocolate plop", .3f, 180, .45f, .08f);
            iceWall = JuiceSound("Ice crack", .19f, 1350, .32f, .35f);
            int[] notes = { 0, 4, 7, 12, 16, 19, 24, 28 };
            for (int i = 0; i < chainNotes.Length; i++) chainNotes[i] = Tone("Chain " + (i + 1), 392 * Mathf.Pow(2, notes[i] / 12f), .19f, 1.07f);
            warning = Tone("Escape", 200, .35f, .65f);
            clear = Tone("Complete", 660, .65f, 2);
            music.clip = MakeMusic();
            if (Player.GamepadControls.BrowserReady) music.Play();
        }
        // Called by the WebGL template inside the initial click gesture.
        public void UnlockFromBrowser()
        {
            Player.GamepadControls.UnlockBrowser();
            if (!music.isPlaying) music.Play();
        }
        /// <summary>Silences the loop while the opening movie has the screen, without losing its place.</summary>
        public void MuteMusic(bool muted) { if (music != null) music.volume = muted ? 0 : MusicVolume; }
        public void SetMusic(float volume) { MusicVolume = Mathf.Clamp01(volume); music.volume = MusicVolume; PlayerPrefs.SetFloat("BetoBeto.Music", MusicVolume); }
        public void SetEffects(float volume) { EffectsVolume = Mathf.Clamp01(volume); effects.volume = EffectsVolume; PlayerPrefs.SetFloat("BetoBeto.Sfx", EffectsVolume); }
        public void Play(string cue)
        {
            var clip = cue switch { "jelly" => jelly, "scone" => scone, "cookieBreak" => cookieBreak, "chocolate" => chocolate, "iceWall" => iceWall,
                "scareReady" => scareReady, "drool" => splash, "slide" => slide, "wall" => wall, "escape" => warning, "win" => clear, _ => crunch };
            effects.PlayOneShot(clip);
        }
        public void PlayChain(int chain)
        {
            effects.PlayOneShot(chainNotes[Mathf.Clamp(chain - 2, 0, chainNotes.Length - 1)], .9f);
            effects.PlayOneShot(slide, .32f);
        }
        public void PlayScare(float charge)
        {
            effects.PlayOneShot(scare, 1 - charge * .45f);
            if (charge > .2f) effects.PlayOneShot(scareFull, charge * .85f);
        }
        static AudioClip JuiceSound(string name, float duration, float hz, float sweep, float noiseAmount)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            var random = new System.Random(627);
            float phase = 0, filteredNoise = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += Mathf.PI * 2 * Mathf.Lerp(hz, hz * sweep, t) / rate;
                filteredNoise = Mathf.Lerp(filteredNoise, (float)random.NextDouble() * 2 - 1, .6f);
                float envelope = Mathf.Min(1, t * 55) * Mathf.Exp(-t * 6);
                samples[i] = (Mathf.Sin(phase) * .55f + Mathf.Sin(phase * 1.52f) * .16f + filteredNoise * noiseAmount) * envelope * .65f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        static AudioClip Tone(string name, float hz, float duration, float sweep)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += 6.283185f * Mathf.Lerp(hz, hz * sweep, t) / rate;
                samples[i] = (Mathf.Sin(phase) + .2f * Mathf.Sin(phase * 2)) * Mathf.Sin(Mathf.PI * t) * (1 - t) * .32f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        static AudioClip MakeMusic()
        {
            const int rate = 22050;
            const float beat = .34f;
            int[] notes = { 72, 76, 79, 76, 74, 77, 81, 77, 71, 74, 79, 74, 72, 76, 79, 84,
                            76, 79, 84, 79, 74, 77, 81, 77, 71, 74, 79, 74, 72, 79, 76, 72 };
            var samples = new float[Mathf.RoundToInt(rate * beat * notes.Length)];
            for (int n = 0; n < notes.Length; n++)
            {
                float hz = 440 * Mathf.Pow(2, (notes[n] - 69) / 12f);
                int start = Mathf.RoundToInt(n * beat * rate);
                for (int j = 0; j < beat * rate && start + j < samples.Length; j++)
                {
                    float t = j / (float)rate;
                    float env = Mathf.Min(1, t * 120) * Mathf.Exp(-t * 12);
                    float note = Mathf.Sin(t * hz * 6.283185f) + .28f * Mathf.Sin(t * hz * 12.56637f);
                    float bass = Mathf.Sin(t * hz * .25f * 6.283185f) * Mathf.Exp(-t * 8);
                    samples[start + j] = note * env * .13f + bass * .07f;
                }
            }
            var clip = AudioClip.Create("Kitchen waltz - prototype", samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            PlayerPrefs.Save();
            if (music != null && music.clip != null) Destroy(music.clip);
            foreach (var clip in new[] { scare, scareFull, scareReady, splash, crunch, warning, clear, wall, slide, jelly, scone, cookieBreak, chocolate, iceWall }) if (clip != null) Destroy(clip);
            foreach (var clip in chainNotes) if (clip != null) Destroy(clip);
        }
    }
}
