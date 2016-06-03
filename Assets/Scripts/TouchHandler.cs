using UnityEngine;
using System.Collections;

public class TouchHandler : MonoBehaviour {

	public float distance = 100; // Rayの届く距離
	
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButtonDown(0)) {
			// メインカメラからクリックしたポジションに向かってRayを撃つ。
//			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			// メインじゃないカメラ
			Camera camera = this.GetComponent<Camera> ();
			Ray ray = camera.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit = new RaycastHit();

			if (Physics.Raycast(ray, out hit, distance)) {
				GameObject selectedGameObject = hit.collider.gameObject;
				TapBehaviour target = selectedGameObject.GetComponent(typeof(TapBehaviour)) as TapBehaviour;
				if(target != null){
					target.TapDown(ref hit);
				}
			}
		}
	}
}
