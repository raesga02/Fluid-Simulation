using UnityEngine;

public class CameraController : MonoBehaviour {

    [Header("Movement Control")]
    public Vector3 targetPosition;
    public Vector3 velocity = Vector3.zero;

    [Header("Rotation Control")]
    public float mouseSensitivity = 100f;
    public float xRotation = 0f;
    public float yRotation = 0f;

    [Header("Immersive Mode Controls")]
    public bool immersiveModeOn = false;

    private void Start() {
        targetPosition = transform.localPosition;
    }

    private void Update() {
        UpdateImmersionState();

        if (immersiveModeOn) {
            CheckMovement();
            CheckRotation();
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
        if (Input.GetKey("w")) { targetPosition += transform.forward; }
        if (Input.GetKey("s")) { targetPosition -= transform.forward; }
        if (Input.GetKey("d")) { targetPosition += transform.right; }
        if (Input.GetKey("a")) { targetPosition -= transform.right; }
        if (Input.GetKey("space")) { targetPosition += Vector3.up; }
        if (Input.GetKey("left shift")) { targetPosition -= Vector3.up; }

        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPosition, ref velocity, 0.25f);
    }

    void CheckRotation() {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        yRotation += mouseX;

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    }
}
