using UnityEngine;

public class CFX_InspectorHelp : MonoBehaviour
{
	public bool Locked;
	public string Title;
	public string HelpText;
	public int MsgType;
	
	[ContextMenu("Unlock editing")]
	void Unlock()
	{
		this.Locked = false;
	}
}
