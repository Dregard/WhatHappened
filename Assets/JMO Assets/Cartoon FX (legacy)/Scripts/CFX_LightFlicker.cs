using UnityEngine;

[RequireComponent(typeof(Light))]
public class CFX_LightFlicker : MonoBehaviour
{
	public bool loop;

	public float smoothFactor = 1f;
	
	/// Max intensity will be: baseIntensity + addIntensity
	public float addIntensity = 1.0f;
	
	private float minIntensity;
	private float maxIntensity;
	private float baseIntensity;
	
	void Awake()
	{
		baseIntensity = GetComponent<Light>().intensity;
	}
	
	void OnEnable()
	{
		minIntensity = baseIntensity;
		maxIntensity = minIntensity + addIntensity;
	}
	
	void Update ()
	{
		GetComponent<Light>().intensity = Mathf.Lerp(minIntensity, maxIntensity, Mathf.PerlinNoise(Time.time * smoothFactor, 0f));
	}
}
