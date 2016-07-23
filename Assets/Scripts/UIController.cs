using UnityEngine;
using System.Collections;

public class UIController : MonoBehaviour {
	public string currentUrl = "";

	public void OpenURL() {
		if (currentUrl != "") {
			Application.OpenURL(currentUrl);
		}
	}
}
