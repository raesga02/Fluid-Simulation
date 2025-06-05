using System;
using UnityEngine;

namespace _2D {

    public class UserInputManager : MonoBehaviour {

        [Header("Immersive Mode Controls")]
        [SerializeField] bool immersiveModeOn = true;

        [Header("Speed Control")]
        [SerializeField] float defaultSpeedFactor = 1f;
        [SerializeField] float minSpeedFactor = 0.1f;
        [SerializeField] float maxSpeedFactor = 1.6f;
        [SerializeField] float speedChangeStep = 0.2f;

        // private fields
        SimulationManager manager;
        Vector2 defaultGravity = Vector2.zero;


        private void Start() {
            manager = SimulationManager.Instance;
            manager.simulationSpeedFactor = defaultSpeedFactor;
        }

        private void Update() {
            if (immersiveModeOn) {
                CheckPause();
                CheckReset();
                CheckSpeed();
                CheckColoringMode();
                CheckGravityChange();
            }

            UpdateImmersionState();
        }

        void UpdateImmersionState() {
            // Enter immersive mode
            if (Input.GetKeyDown("mouse 0")) {
                immersiveModeOn = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Exit immersive mode
            if (Input.GetKeyDown("escape")) {
                immersiveModeOn = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void CheckPause() => manager.isPaused = Input.GetKeyDown("p") ? !manager.isPaused : manager.isPaused;

        void CheckReset() => manager.pendingReset = Input.GetKeyDown("r") ? true : manager.pendingReset;

        private void CheckSpeed() {
            float scrollValue = Mathf.Clamp(Input.GetAxis("Mouse ScrollWheel"), -1, 1);
            float rotationDirection = Mathf.Sign(scrollValue);

            if (Mathf.Abs(scrollValue) > 0.01f) {
                float newSpeedFactor = manager.simulationSpeedFactor + rotationDirection * speedChangeStep;
                manager.simulationSpeedFactor = Mathf.Clamp(newSpeedFactor, minSpeedFactor, maxSpeedFactor);
                manager.RequestSettingsUpdate();
            }
        }

        void CheckColoringMode() {
            if (Input.GetKey(KeyCode.Alpha1)) { manager.fluidRenderer.colorMode = ColoringMode.FlatColor; manager.fluidRenderer.needsUpdate = true; }
            if (Input.GetKey(KeyCode.Alpha2)) { manager.fluidRenderer.colorMode = ColoringMode.VelocityMagnitude; manager.fluidRenderer.needsUpdate = true; }
            if (Input.GetKey(KeyCode.Alpha3)) { manager.fluidRenderer.colorMode = ColoringMode.DensityDeviation; manager.fluidRenderer.needsUpdate = true; }
        }

        void CheckGravityChange() {
            // U/J -> X axis
            // I/K -> Y axis
            // O/L -> Z axis
            if (defaultGravity == Vector2.zero) {
                defaultGravity = manager.fluidUpdater.gravity;
            }

            if (!Input.GetKey(KeyCode.G)) { return; }

            Vector2 gravityDirection = Vector2.zero;


            // Y axis
            if (Input.GetKey(KeyCode.I)) {
                gravityDirection += Vector2.up;
            }
            else if (Input.GetKey(KeyCode.K)) {
                gravityDirection += Vector2.down;
            }

            // X axis
            if (Input.GetKey(KeyCode.U)) {
                gravityDirection += Vector2.right;
            }
            else if (Input.GetKey(KeyCode.J)) {
                gravityDirection += Vector2.left;
            }

            // Default
            if (gravityDirection == Vector2.zero) {
                gravityDirection = defaultGravity;
            }

            Vector2 newGravity = gravityDirection.normalized * 9.81f;

            if (newGravity != manager.fluidUpdater.gravity) {
                manager.fluidUpdater.gravity = newGravity;
                manager.fluidUpdater.needsUpdate = true;
            }
        }
    }

}
