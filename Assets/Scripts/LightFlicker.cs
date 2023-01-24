using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public float flickerIntensity = 1.0f;
    public float flickersPerSecond = 3.0f;
    public float speedRandomness = 1.0f;

    private float time;
    private float startingIntensity;
    private Light light;

    void Start()
    {
        light = GetComponent<Light>();
        startingIntensity = light.intensity;
    }

    void Update()
    {
        time += Time.deltaTime * (1 - Random.Range(-speedRandomness, speedRandomness)) * Mathf.PI;
        light.intensity = startingIntensity + Mathf.Sin(time * flickersPerSecond) * flickerIntensity;
    }
}