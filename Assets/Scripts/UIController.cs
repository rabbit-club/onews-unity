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

	public void volumeOn() {
		MainController = GameObject.Find("Camera").GetComponent<MainController>();
		MainController.volumeOn();
	}

	public void volumeOff() {
		MainController = GameObject.Find("Camera").GetComponent<MainController>();
		MainController.volumeOff();
	}
}
