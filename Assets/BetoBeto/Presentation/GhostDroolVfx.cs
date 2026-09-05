using UnityEngine;
using UnityEngine.Rendering;

namespace BetoBeto.Presentation
{
    /// <summary>Fixed pools of CPU simulated liquid ribbons, drops and impact rings. No physics/compute dependency.</summary>
    [DisallowMultipleComponent]
    public sealed class GhostDroolVfx : MonoBehaviour
    {
        public Transform origin;
        public Material material;
        [Range(4, 16)] public int streamCount = 10;
        [Range(16, 96)] public int dropletCount = 64;
        [Range(40, 240)] public float dropsPerSecond = 160;
        [Min(.01f)] public float streamWidth = .035f;
        public float groundHeight = .045f;
        const int Nodes = 24, RingNodes = 13, ImpactCount = 20;
        const float Step = .025f, Gravity = 8.5f;
        sealed class Stream
        {
            public LineRenderer line;
            public readonly Vector3[] position = new Vector3[Nodes];
            public readonly Vector3[] velocity = new Vector3[Nodes];
            public int count;
        }
        sealed class Drop
        {
            public LineRenderer line;
            public Vector3 position, velocity;
            public readonly Vector3[] points = new Vector3[2];
            public bool alive;
            public float length;
        }
        sealed class Impact
        {
            public LineRenderer line;
            public Vector3 position;
            public readonly Vector3[] points = new Vector3[RingNodes];
            public float age, duration, radius;
        }
        Stream[] streams;
        Drop[] drops;
        Impact[] impacts;
        float flowTime, streamClock, dropClock, impactClock;
        int nextDrop, nextImpact;
        uint randomState = 0x123f81u;
        public bool IsEmitting { get; private set; }
        public int ActiveDropCount { get; private set; }
        public int ImpactEvents { get; private set; }

        void Awake() => EnsurePool();

        void EnsurePool()
        {
            if (streams != null || origin == null || material == null) return;
            streams = new Stream[streamCount];
            drops = new Drop[dropletCount];
            impacts = new Impact[ImpactCount];
            var container = new GameObject("Drool effects (pooled)").transform;
            container.SetParent(transform, false);
            for (int i = 0; i < streams.Length; i++)
                streams[i] = new Stream { line = CreateLine(container, "Liquid stream " + i, streamWidth * Mathf.Lerp(.65f, 1.3f, Random01())) };
            for (int i = 0; i < drops.Length; i++)
                drops[i] = new Drop { line = CreateLine(container, "Falling droplet " + i, streamWidth * .55f) };
            for (int i = 0; i < impacts.Length; i++)
                impacts[i] = new Impact { line = CreateLine(container, "Ground splash " + i, .015f), age = 1 };
        }

        LineRenderer CreateLine(Transform parent, string name, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.generateLightingData = true;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.numCapVertices = 3;
            line.numCornerVertices = 2;
            line.widthMultiplier = width;
            line.widthCurve = AnimationCurve.Linear(0, 1, 1, .3f);
            line.positionCount = 0;
            line.enabled = false;
            return line;
        }

        float Random01()
        {
            randomState ^= randomState << 13;
            randomState ^= randomState >> 17;
            randomState ^= randomState << 5;
            return (randomState & 0xffffff) / 16777216f;
        }

        public void Tick(float dt, bool emit)
        {
            EnsurePool();
            if (streams == null || dt <= 0) return;
            IsEmitting = emit;
            // Keep trajectories stable on slower WebGL frames, with a bounded amount of work.
            for (float remaining = Mathf.Min(dt, .1f); remaining > .00001f;)
            {
                float step = Mathf.Min(remaining, 1f / 60);
                Simulate(step, emit);
                remaining -= step;
            }
            Draw();
        }

        void Simulate(float dt, bool emit)
        {
            flowTime += dt;
            impactClock = Mathf.Max(0, impactClock - dt);
            streamClock += dt;
            bool addHead = emit && streamClock >= Step;
            if (streamClock >= Step) streamClock %= Step;
            for (int s = 0; s < streams.Length; s++)
            {
                var stream = streams[s];
                for (int i = 0; i < stream.count; i++)
                {
                    stream.velocity[i].y -= Gravity * dt;
                    stream.position[i] += stream.velocity[i] * dt;
                    if (stream.position[i].y <= groundHeight)
                    {
                        stream.position[i].y = groundHeight;
                        if (impactClock <= 0)
                        {
                            Splash(stream.position[i]);
                            impactClock = .035f;
                        }
                        stream.count = i == 0 ? 0 : i + 1;
                        break;
                    }
                }
                if (!addHead) continue;
                stream.count = Mathf.Min(Nodes, stream.count + 1);
                for (int i = stream.count - 1; i > 0; i--)
                {
                    stream.position[i] = stream.position[i - 1];
                    stream.velocity[i] = stream.velocity[i - 1];
                }
                float angle = s * 2.399963f;
                Vector3 forward = -transform.forward;
                Vector3 right = transform.right;
                stream.position[0] = origin.position + right * (Mathf.Sin(angle) * .025f);
                stream.velocity[0] = forward * (.12f + .05f * Mathf.Sin(flowTime * 9 + s))
                    + right * (Mathf.Sin(angle + flowTime * 5) * .11f) + Vector3.down * .18f;
            }
            if (emit)
            {
                dropClock += dt * dropsPerSecond;
                while (dropClock >= 1)
                {
                    dropClock--;
                    var drop = drops[nextDrop++ % drops.Length];
                    drop.alive = true;
                    drop.position = origin.position;
                    drop.velocity = -transform.forward * Mathf.Lerp(.08f, .5f, Random01())
                        + transform.right * Mathf.Lerp(-.4f, .4f, Random01())
                        + Vector3.up * Mathf.Lerp(-.8f, .15f, Random01());
                    drop.length = Mathf.Lerp(.035f, .12f, Random01());
                }
            }
            else dropClock = 0;
            foreach (var drop in drops)
            {
                if (!drop.alive) continue;
                drop.velocity.y -= Gravity * dt;
                drop.position += drop.velocity * dt;
                if (drop.position.y > groundHeight) continue;
                drop.position.y = groundHeight;
                drop.alive = false;
                if (impactClock <= 0) { Splash(drop.position); impactClock = .025f; }
            }
            foreach (var impact in impacts) impact.age += dt;
        }

        void Splash(Vector3 position)
        {
            var impact = impacts[nextImpact++ % impacts.Length];
            impact.position = position;
            impact.age = 0;
            impact.duration = Mathf.Lerp(.18f, .38f, Random01());
            impact.radius = Mathf.Lerp(.07f, .22f, Random01());
            ImpactEvents++;
        }

        void Draw()
        {
            foreach (var stream in streams)
            {
                stream.line.enabled = stream.count >= 2;
                stream.line.positionCount = stream.count;
                for (int i = 0; i < stream.count; i++) stream.line.SetPosition(i, stream.position[i]);
                if (IsEmitting && stream.count > 0) stream.line.SetPosition(0, origin.position);
            }
            ActiveDropCount = 0;
            foreach (var drop in drops)
            {
                drop.line.enabled = drop.alive;
                if (!drop.alive) continue;
                ActiveDropCount++;
                drop.points[0] = drop.position;
                drop.points[1] = drop.position - drop.velocity.normalized * drop.length;
                drop.line.positionCount = 2;
                drop.line.SetPositions(drop.points);
            }
            foreach (var impact in impacts)
            {
                bool alive = impact.age < impact.duration;
                impact.line.enabled = alive;
                if (!alive) continue;
                float t = impact.age / impact.duration;
                float radius = impact.radius * Mathf.Lerp(.15f, 1, t);
                for (int i = 0; i < RingNodes; i++)
                {
                    float angle = i * Mathf.PI * 2 / (RingNodes - 1);
                    impact.points[i] = impact.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius
                        + Vector3.up * (.012f + Mathf.Sin(t * Mathf.PI) * .045f);
                }
                impact.line.startColor = impact.line.endColor = new Color(1, 1, 1, 1 - t);
                impact.line.positionCount = RingNodes;
                impact.line.SetPositions(impact.points);
            }
        }

        void OnDisable()
        {
            IsEmitting = false;
            if (streams == null) return;
            foreach (var stream in streams) { stream.count = 0; stream.line.enabled = false; }
            foreach (var drop in drops) { drop.alive = false; drop.line.enabled = false; }
            foreach (var impact in impacts) { impact.age = 1; impact.line.enabled = false; }
            ActiveDropCount = 0;
        }
    }
}
