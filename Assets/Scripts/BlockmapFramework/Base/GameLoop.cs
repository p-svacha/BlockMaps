using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The core loop of the game. Provides a fixed tick-interval for the simulation and a render interval (which is every frame). 
    /// <br/> This is intended to be the only MonoBehaviour used, so that the rest of the game systems can remain independent of Unity-specific classes.
    /// </summary>
    public abstract class GameLoop : MonoBehaviour
    {
        // ------------------------------------------------------------
        // Constants & Fields
        // ------------------------------------------------------------

        /// <summary>
        /// The target Ticks Per Second (TPS). 60 ticks per second = 16.67 ms per tick.
        /// </summary>
        public const float TPS = 60f;

        /// <summary>
        /// Allows you to speed up or slow down the simulation as desired.
        /// For example, 2.0 would run simulation at double speed,
        /// 0.5 would run it at half speed.
        /// </summary>
        public float SimulationSpeed = 1f;

        /// <summary>
        /// Accumulates real time (scaled by SimulationSpeed) so we know when to run ticks.
        /// </summary>
        private float accumulator = 0f;

        /// <summary>
        /// Keeps track of the last frame's real time (via Time.realtimeSinceStartup).
        /// </summary>
        private float lastFrameTime = 0f;

        /// <summary>
        /// Derived from TPS. This is how many seconds each discrete simulation tick spans.
        /// For example, at 60 TPS, each tick is 1/60 = 0.01666... seconds.
        /// </summary>
        public const float TickDeltaTime = 1f / TPS;

        // ------------------------------------------------------------
        // Unity Lifecycle Methods
        // ------------------------------------------------------------

        private void Awake()
        {
            lastFrameTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            // 1) Register inputs for this frame
            HandleInputs();

            // 2) Calculate how much real time has passed since the last frame.
            float currentFrameTime = Time.realtimeSinceStartup;
            float deltaTime = currentFrameTime - lastFrameTime;
            lastFrameTime = currentFrameTime;

            // 3) Scale the real time by SimulationSpeed to allow slow-mo or fast-forward effects.
            deltaTime *= SimulationSpeed;

            // 4) Accumulate time to know how many discrete ticks we need to simulate.
            accumulator += deltaTime;

            // 5) Run as many "ticks" as needed to catch up with real time.
            //    If the user paused or a heavy frame spike occurred,
            //    we might run multiple ticks in one Update.
            while (accumulator >= TickDeltaTime)
            {
                Tick();
                accumulator -= TickDeltaTime;
            }

            // 6) General stuff called every frame
            OnFrame();

            // 7) Render the game, using interpolation alpha (0..1) wherever needed (e.g., positions)
            // alpha tells us how far along we are from the last tick to the next tick.
            float alpha = accumulator / TickDeltaTime;
            Render(alpha);
        }

        private void FixedUpdate()
        {
            OnFixedUpdate();
        }

        protected abstract void OnFixedUpdate();

        // ------------------------------------------------------------
        // Simulation + Rendering
        // ------------------------------------------------------------

        /// <summary>
        /// This function is called one or more times per frame
        /// (depending on how many ticks are needed),
        /// each advancing the simulation by 'tickDeltaTime'.
        /// </summary>
        protected abstract void Tick();

        /// <summary>
        /// Called once per frame after we've processed our simulation ticks. 'alpha' tells us how far between the last tick and the next tick we are.
        /// Use 'alpha' to interpolate your entities' positions, rotations, or animation states for smooth rendering.
        /// </summary>
        protected abstract void Render(float alpha);

        /// <summary>
        /// Called once per frame at the very beginning of the frame.
        /// </summary>
        protected abstract void HandleInputs();

        /// <summary>
        /// Gets called every frame after handling inputs and tick processing and before rendering.
        /// </summary>
        protected abstract void OnFrame();

        /// <summary>
        /// Example function that can set simulation speed 
        /// to 0 for pausing, or back to normal for unpausing, etc.
        /// </summary>
        protected void SetSimulationSpeed(float newSpeed)
        {
            SimulationSpeed = Mathf.Max(0f, newSpeed);
        }
    }
}
