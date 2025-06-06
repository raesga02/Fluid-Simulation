using System;
using UnityEngine;

namespace _3D {

    public class UserInputManager : MonoBehaviour {

        [Header("Immersive Mode Controls")]
        [SerializeField] bool immersiveModeOn = true;

        [Header("Movement Control")]
        [SerializeField] Vector3 targetPosition;
        [SerializeField] Vector3 velocity = Vector3.zero;

        [Header("Rotation Control")]
        [SerializeField] float mouseSensitivity = 100f;
        [SerializeField] float xRotation = 0f;
        [SerializeField] float yRotation = 180f;

        [Header("Speed Control")]
        [SerializeField] float defaultSpeedFactor = 1f;
        [SerializeField] float minSpeedFactor = 0.1f;
        [SerializeField] float maxSpeedFactor = 1.6f;
        [SerializeField] float speedChangeStep = 0.2f;

        // private fields
        SimulationManager manager;
        Camera mainCamera;
        Vector3 defaultGravity = Vector3.zero;


        private void Start() {
            manager = SimulationManager.Instance;
            mainCamera = Camera.main;
            targetPosition = mainCamera.transform.localPosition;
            manager.simulationSpeedFactor = defaultSpeedFactor;

            xRotation = mainCamera.transform.localRotation.eulerAngles.x;
            yRotation = mainCamera.transform.localRotation.eulerAngles.y;
        }

        private void Update() {
            if (immersiveModeOn) {
                CheckMovement();
                CheckRotation();
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

        void CheckMovement() {
            if (Input.GetKey("w")) { targetPosition += mainCamera.transform.forward; }
            if (Input.GetKey("s")) { targetPosition -= mainCamera.transform.forward; }
            if (Input.GetKey("d")) { targetPosition += mainCamera.transform.right; }
            if (Input.GetKey("a")) { targetPosition -= mainCamera.transform.right; }
            if (Input.GetKey("space")) { targetPosition += Vector3.up; }
            if (Input.GetKey("left shift")) { targetPosition -= Vector3.up; }

            mainCamera.transform.localPosition = Vector3.SmoothDamp(mainCamera.transform.localPosition, targetPosition, ref velocity, 0.25f);
        }

        void CheckRotation() {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            yRotation += mouseX;

            mainCamera.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
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
            // Surface display
            if (Input.GetKey(KeyCode.Alpha4)) {
                manager.fluidRenderer.visualizationMode = VisualizationMode.Surface;
            }
            // Particle based display methods
            else {
                if (Input.GetKey(KeyCode.Alpha1)) { 
                    manager.fluidRenderer.visualizationMode = VisualizationMode.Particles;
                    manager.fluidRenderer.colorMode = ColoringMode.FlatColor; manager.fluidRenderer.needsUpdate = true; 
                }
                if (Input.GetKey(KeyCode.Alpha2)) { 
                    manager.fluidRenderer.visualizationMode = VisualizationMode.Particles;
                    manager.fluidRenderer.colorMode = ColoringMode.VelocityMagnitude; manager.fluidRenderer.needsUpdate = true; 
                }
                if (Input.GetKey(KeyCode.Alpha3)) { 
                    manager.fluidRenderer.visualizationMode = VisualizationMode.Particles;
                    manager.fluidRenderer.colorMode = ColoringMode.DensityDeviation; manager.fluidRenderer.needsUpdate = true; 
                }
            }
        }

        private void CheckGravityChange() {
            // U/J -> X axis
            // I/K -> Y axis
            // O/L -> Z axis
            if (defaultGravity == Vector3.zero) {
                defaultGravity = manager.fluidUpdater.gravity;
            }

            if (!Input.GetKey(KeyCode.G)) { return; }

            Vector3 gravityDirection = Vector3.zero;


            // Y axis
            if (Input.GetKey(KeyCode.I)) {
                gravityDirection += Vector3.up;
            }
            else if (Input.GetKey(KeyCode.K)) {
                gravityDirection += Vector3.down;
            }

            // X axis
            if (Input.GetKey(KeyCode.U)) {
                gravityDirection += Vector3.right;
            }
            else if (Input.GetKey(KeyCode.J)) {
                gravityDirection += Vector3.left;
            }

            // Z axis
            if (Input.GetKey(KeyCode.O)) {
                gravityDirection += Vector3.forward;
            }
            else if (Input.GetKey(KeyCode.L)) {
                gravityDirection += Vector3.back;
            }

            // Default
            if (gravityDirection == Vector3.zero) {
                gravityDirection = defaultGravity;
            }

            Vector3 newGravity = gravityDirection.normalized * 9.81f;

            if (newGravity != manager.fluidUpdater.gravity) {
                manager.fluidUpdater.gravity = newGravity;
                manager.fluidUpdater.needsUpdate = true;
            }
        }
    }
}
