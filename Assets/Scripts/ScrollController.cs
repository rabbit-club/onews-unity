using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ScrollController : MonoBehaviour {

	[SerializeField]
	RectTransform prefab = null;

	public void setItem(int number, string text, string imgUrl, string articleUrl) 
	{
		var item = GameObject.Instantiate(prefab) as RectTransform;
		item.SetParent(transform, true);

		Vector2 itemPosition = item.transform.localPosition;
		itemPosition.x = 532;
		itemPosition.y = -456 - number * 200;
		item.transform.localPosition = itemPosition;

		var itemText = item.GetComponentInChildren<Text>();
		itemText.text = text;

		// TODO: 画像とURLもやる
		// TODO: プレハブが適当なので修正
	}
}