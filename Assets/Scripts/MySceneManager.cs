using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MySceneManager : MonoBehaviour {
	void Start () {
		SceneManager.LoadScene("spaceship", LoadSceneMode.Additive);
	}
}
