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


        private void Start() {
            manager = SimulationManager.Instance;
            manager.simulationSpeedFactor = defaultSpeedFactor;
        }

        private void Update() {
            if (immersiveModeOn) {
                CheckPause();
                CheckReset();
                CheckSpeed();
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
    }

}
