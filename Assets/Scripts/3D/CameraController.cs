using UnityEngine;

public class CameraController : MonoBehaviour {

    [Header("Movement Control")]
    public Vector3 targetPosition;
    public Vector3 velocity = Vector3.zero;
    public float maxSpeed = 1.0f;

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
    }
}
