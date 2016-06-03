using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MySceneManager : MonoBehaviour {

	[SerializeField]
	Image backGroundImage;

	void Start () {
		SceneManager.LoadScene("spaceship", LoadSceneMode.Additive);
//		SceneManager.LoadScene("UIScene", LoadSceneMode.Additive);
		backGroundImage.CrossFadeAlpha (0, 1.0f, true);
	}
	
}
