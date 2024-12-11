using UnityEngine;

public class CameraController : MonoBehaviour {

    [Header("Movement Control")]
    public Vector3 targetPosition;
    public Vector3 velocity = Vector3.zero;

    [Header("Rotation Control")]
    public float mouseSensitivity = 100f;
    public float xRotation = 0f;
    public float yRotation = 0f;

    private void Start() {
        targetPosition = transform.localPosition;
    }

    private void Update() {
        // Movement
        if (Input.GetKey("w")) { targetPosition += transform.forward; }
        if (Input.GetKey("s")) { targetPosition -= transform.forward; }
        if (Input.GetKey("d")) { targetPosition += transform.right; }
        if (Input.GetKey("a")) { targetPosition -= transform.right; }
        if (Input.GetKey("space")) { targetPosition += transform.up; }
        if (Input.GetKey("left shift")) { targetPosition -= transform.up; }

        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPosition, ref velocity, 0.25f);

        // Rotation
        if (Input.GetKey("mouse 1") || true) {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            yRotation += mouseX;
            
            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        }

        // Cursor show
        if (Input.GetKeyDown("escape")) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Cursor hide
        if (Input.GetMouseButtonDown(0)) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
