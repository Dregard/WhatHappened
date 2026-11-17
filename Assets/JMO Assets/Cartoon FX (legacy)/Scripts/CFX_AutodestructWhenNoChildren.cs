using UnityEngine;

public class CFX_AutodestructWhenNoChildren : MonoBehaviour
{
	void Update ()
	{
		if( transform.childCount == 0)
		{
			GameObject.Destroy(this.gameObject);
		}
	}
}
