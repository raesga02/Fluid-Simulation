using UnityEngine;

public class LightingTest : MonoBehaviour {
    public Material material;
    public Light sceneLight;

    void Update() {
        if (material != null && sceneLight != null) {
            material.SetColor("_LightColor", sceneLight.color * sceneLight.intensity);
            material.SetVector("_LightDirection", -sceneLight.transform.forward);
        }
    }
}