using UnityEngine;
using System.Collections;

public class TapBehaviour : MonoBehaviour {

	// タッチしたときに呼ばれる。
	public virtual void TapDown(ref RaycastHit hit){}
}
