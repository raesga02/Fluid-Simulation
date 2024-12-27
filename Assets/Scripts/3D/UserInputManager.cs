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
    }

}
