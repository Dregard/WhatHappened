using UnityEngine;
using System.Collections;

public class CFX_ShurikenThreadFix : MonoBehaviour
{
	private ParticleSystem[] systems;
	
	void OnEnable()
	{
		systems = GetComponentsInChildren<ParticleSystem>();
		
		foreach(var ps in systems)
		{
			ps.Stop(true);
			ps.Clear(true);
		}
		
		StartCoroutine("WaitFrame");
	}
	
	IEnumerator WaitFrame()
	{
		yield return null;
		
		foreach(var ps in systems)
		{
			ps.Play(true);
		}
	}
}