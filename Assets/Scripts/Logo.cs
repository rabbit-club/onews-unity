using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class Logo : MonoBehaviour, IPointerClickHandler {
	GameObject MainObject;
	MainController MainController;

	GameObject UnityChan;
//	Animator UnityChanAnim;

	public GameObject Circle;

	public void OnPointerClick (PointerEventData eventData){
//		iTween.MoveAdd( Circle, new Vector3(588, 0, 0), 180f );

//		UnityChan = GameObject.Find ("unitychan");
//		UnityChanAnim = UnityChan.GetComponent<Animator>();
//		UnityChanAnim.SetBool ("Next", true);

//		MainController = GameObject.Find("Camera").GetComponent<MainController>();;
//		MainController.Movie();
//		this.gameObject.SetActive(false);
	}

	public void Reset() {
		Application.LoadLevel("Main");
	}
}
