using UnityEngine;
using System.Collections;

public class UIController : MonoBehaviour {
	public string currentUrl = "";
	MainController MainController;

	public void OpenURL() {
		if (currentUrl != "") {
			Application.OpenURL(currentUrl);
		}
	}

	public void mute() {
		MainController = GameObject.Find("Camera").GetComponent<MainController>();
		MainController.mute();
	}
}
