using UnityEngine;
using System.Collections;
using System.Linq;

public class LipSync : MonoBehaviour {

	SkinnedMeshRenderer skinMesh;
	float timer;
	bool mouseClose;

	void Start () {
		timer = 0;
		mouseClose = true;
		skinMesh = this.GetComponentsInChildren<SkinnedMeshRenderer>().First(s => s.name == "MTH_DEF");
	}
	
	void Update () {
		timer += Time.deltaTime;
		if(timer > 0.1f) {
			paku();
			timer = 0;
			mouseClose = !mouseClose;
		}
	}

	void paku() {
		if (mouseClose) {
			skinMesh.SetBlendShapeWeight (0, 0.0f);
		} else {
			skinMesh.SetBlendShapeWeight(0, 50.0f);
		}
	}
}
