using UnityEngine;

namespace _3D {

    public class UserInputManager : MonoBehaviour {

        [Header("Movement Control")]
        public Vector3 targetPosition;
        public Vector3 velocity = Vector3.zero;

        [Header("Rotation Control")]
        public float mouseSensitivity = 100f;
        public float xRotation = 0f;
        public float yRotation = 0f;

        [Header("Immersive Mode Controls")]
        public bool immersiveModeOn = false;

        [Header("References")]
        Camera mainCamera;

        // private fields
        SimulationManager manager;

        private void Start() {
            manager = SimulationManager.Instance;
            mainCamera = Camera.main;
            targetPosition = mainCamera.transform.localPosition;
        }

        private void Update() {
            UpdateImmersionState();

            if (immersiveModeOn) {
                CheckMovement();
                CheckRotation();
                CheckPause();
                CheckReset();
            }
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
    }

}
