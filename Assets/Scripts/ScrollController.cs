using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;

public class ScrollController : MonoBehaviour {

	[SerializeField]
	RectTransform prefab = null;

	// リスト画像の枠の大きさ
	double displayWidth = 200;
	double displayHeight = 150;

	public void setItem(int number, string title, string time, Texture2D texture, string articleUrl) 
	{
		var item = GameObject.Instantiate(prefab) as RectTransform;
		item.SetParent(transform, true);

		Vector2 itemPosition = item.transform.localPosition;
		itemPosition.x = 532;
		itemPosition.y = -100 - number * 200;
		item.transform.localPosition = itemPosition;

		var itemText = item.GetComponentsInChildren<Text>();
		itemText[0].text = title;
		itemText[1].text = time;

		var itemImage = item.GetComponentsInChildren<Image>();
		var size = reseizeTexture(texture);
		int imageWidth = size[0];
		int imageHeight = size[1];
		RectTransform itemTransform = itemImage[1].GetComponent<RectTransform>();
		Vector2 itemSize = itemTransform.sizeDelta;
		itemTransform.sizeDelta = new Vector2 (imageWidth, imageHeight);
		itemImage[1].sprite = Sprite.Create (
			texture, 
			new Rect (0, 0, imageWidth, imageHeight), 
			new Vector2 (0.5f, 0.5f)
		);

		var itemButton = item.GetComponentInChildren<Button>();
		UnityAction onClickAction = () => Application.OpenURL(articleUrl);
		itemButton.onClick.AddListener(onClickAction);
	}

	// 画像が枠に内接する最大サイズを取得
	int [] reseizeTexture(Texture2D texture) {
		double texWidth = texture.width;
		double texHeight = texture.height;
		double ratio = 1;

		// 枠の比率よりも縦長か横長か
		if (texWidth / texHeight >= displayWidth / displayHeight) {
			ratio = displayWidth / texWidth;
		} else {
			ratio = displayHeight / texHeight;
		}

		double dWidth = (double)(texWidth * ratio);
		double dHeight = (double)(texHeight * ratio);
		int width = (int)Math.Ceiling (dWidth);
		int height = (int)Math.Ceiling (dHeight);

		TextureScale.Bilinear (texture, width, height);

		int [] size = {width, height};
		return size;
	}
}
