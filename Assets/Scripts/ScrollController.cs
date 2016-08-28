using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;

public class ScrollController : MonoBehaviour {

	[SerializeField]
	RectTransform prefab = null;

	// ディスプレイサイズ
	double displayWidth = 200;
	double displayHeight = 150;

	public void setItem(int number, string text, Texture2D texture, string articleUrl) 
	{
		var item = GameObject.Instantiate(prefab) as RectTransform;
		item.SetParent(transform, true);

		Vector2 itemPosition = item.transform.localPosition;
		itemPosition.x = 532;
		itemPosition.y = -456 - number * 200;
		item.transform.localPosition = itemPosition;

		var itemText = item.GetComponentInChildren<Text>();
		itemText.text = text;

		var itemImage = item.GetComponentsInChildren<Image>();
		itemImage[1].sprite = reseizeTexture(texture);

		var itemButton = item.GetComponentInChildren<Button>();
		UnityAction onClickAction = () => Application.OpenURL(articleUrl);
		itemButton.onClick.AddListener(onClickAction);
	}

	Sprite reseizeTexture(Texture2D texture) {
		double texWidth = texture.width;
		double texHeight = texture.height;
		double ratio = 1;

		// 表示領域の比率よりも縦長か横長か
		if (texWidth / texHeight >= displayWidth / displayHeight) {
			ratio = displayWidth / texWidth;
		}  else {
			ratio = displayHeight / texHeight;
		}

		double dWidth = (double)(texWidth * ratio);
		double dHeight = (double)(texHeight * ratio);
		int width = (int)Math.Ceiling (dWidth);
		int height = (int)Math.Ceiling (dHeight);

		TextureScale.Bilinear (texture, width, height);
		return Sprite.Create (
			texture, 
			new Rect (0, 0, width, height), 
			new Vector2 (0.5f, 0.5f)
		);
	}
}