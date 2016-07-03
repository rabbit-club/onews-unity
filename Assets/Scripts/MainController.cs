using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityChan;
using System;
using System.IO;
using System.Collections;
using LitJson;
using System.Linq;

public class MainController : MonoBehaviour
{
	public GameObject Display;
	SpriteRenderer DisplaySprite;
	string articlesUrlListURL = "https://www.dropbox.com/s/a5qvgdcx1sdzbwb/articles_url_list.json?dl=1";

	public string filePath = "";
	private AudioSource audioSource;
	private float maxAudioTime;
	private float audioTime;
	private GameObject shortDescription;
	private GameObject title;
	private GameObject startTime;
	private GameObject endTime;
	private GameObject circle;
	public FaceUpdate faceUpdate;
	GameObject UnityChan;
	UnityChanTouch unityChanTouch;
	bool isOffLine = false;

	// ディスプレイサイズ
	double displayWidth = 400;
	double displayHeight = 300;

	public void Movie ()
	{
		StartCoroutine (MovieStart ());
	}

	IEnumerator MovieStart ()
	{
		UnityChan = GameObject.Find ("unitychan");
		unityChanTouch = UnityChan.GetComponent<UnityChanTouch>();
		DisplaySprite = Display.GetComponent<SpriteRenderer> ();
		audioTime = 0.0f;
		maxAudioTime = 0.0f;
		audioSource = GetComponent<AudioSource> ();
		shortDescription = GameObject.Find ("Canvas/Footer/subtitles/Text");
		title = GameObject.Find ("Content/Info/Text");
		startTime = GameObject.Find ("Canvas/Footer/Seekbar/Time");
		endTime = GameObject.Find ("Canvas/Footer/Seekbar/EndTime");
		circle = GameObject.Find ("Canvas/Footer/Seekbar/circle");

		AudioClip seStart = Resources.Load ("SE/start", typeof(AudioClip)) as AudioClip;
		audioSource.PlayOneShot(seStart);
		yield return new WaitForSeconds(seStart.length);

		AudioClip hello = Resources.Load("Voices/ohiru", typeof(AudioClip)) as AudioClip;
		audioSource.PlayOneShot(hello);
		audioTime += hello.length; // 挨拶音声の時間を初回のウェイトにする
		audioTime += 0.5f;         // ワンテンポの間

		// JSON List取得
		WWW wwwArticlesUrlList = new WWW (articlesUrlListURL);
		yield return wwwArticlesUrlList;
		LitJson.JsonData articlesUrlList = JsonMapper.ToObject(wwwArticlesUrlList.text);

		foreach (var key in articlesUrlList.Keys) {
			string articlesUrl = Convert.ToString(articlesUrlList[key]);
			if (String.IsNullOrEmpty(articlesUrl)) {
				continue;
			}

			// JSON取得
			WWW wwwArticles = new WWW (Convert.ToString(articlesUrl));
			yield return wwwArticles;
			ArticleData[] articles = JsonMapper.ToObject<ArticleData[]> (wwwArticles.text);
			// TODO: 現在時刻から近い順に並び替え
			
			// ローカルキャッシュを作成する
//			createLocalCache(articles);
			if(isOffLine) {
				return false;
			}
			foreach (var article in articles) {
				// 音声の取得と再生
				yield return new WaitForSeconds (audioTime);
				StartCoroutine (download (article.voice));
				yield return new WaitForSeconds (1.0f);
			
				// 画像を取得する
				if (article.image == "") {
					article.image = "http://i.yimg.jp/images/jpnews/cre/common/all/images/fbico_ogp_1200x630.png";
				}
				WWW wwwImage = new WWW (article.image);
				yield return wwwImage;
			
				Texture2D tex = wwwImage.texture;
				// 画像をリサイズする
				reseizeTexture(tex);
			
				// 要約記事テキストの表示
				if (shortDescription != null) {
					// 位置を初期化
					shortDescription.transform.localPosition = new Vector3 (5500.0f, shortDescription.transform.localPosition.y, shortDescription.transform.localPosition.z);
					shortDescription.GetComponent<Text> ().text = article.title + " " + article.description;
				}
			
				// 記事タイトルの表示
				if (title != null) {
					title.GetComponent<Text> ().text = article.title;
				}
			
				// シークバーを動かす
				if (circle != null) {
					circle.transform.position = new Vector3 (-204, circle.transform.position.y, circle.transform.position.z);
					iTween.MoveTo (circle, iTween.Hash ("position", new Vector3 (373, circle.transform.position.y, 0), "time", maxAudioTime - 1, "easeType", "linear"));
				}
			
				// 音声時間maxの表示
				TimeSpan maxTs = TimeSpan.FromSeconds (maxAudioTime);
				endTime.GetComponent<Text> ().text = maxTs.Seconds.ToString ();
			}
		}
			
	}

	IEnumerator download (string filePathUrl)
	{
		WWW www = new WWW (filePathUrl);

		while (!www.isDone) { // ダウンロードの進捗を表示
			print (Mathf.CeilToInt (www.progress * 100));
			yield return null;
		}

		if (!string.IsNullOrEmpty (www.error)) { // ダウンロードでエラーが発生した
			Debug.Log ("error:" + www.error);
		} else { // ダウンロードが正常に完了した
			filePath = Application.persistentDataPath + "/" + Path.GetFileName (www.url);
			File.WriteAllBytes (filePath, www.bytes);
			Debug.Log ("download file write success." + filePath);
			audioSource.clip = www.GetAudioClip(false, true, AudioType.MPEG);
			audioSource.Play();
			// 音声の時間を保存しておく
			maxAudioTime = www.GetAudioClip(false, true, AudioType.MPEG).length;
			audioTime = maxAudioTime;
		}
	}

	void Update ()
	{
		if (audioSource != null && audioSource.isPlaying && audioTime >= 0.0f) {
			// 口パク
			unityChanTouch.useLip = true;

			audioTime -= Time.deltaTime;
			shortDescription.transform.localPosition = new Vector3 (shortDescription.transform.localPosition.x - (Time.deltaTime * 240.0f), shortDescription.transform.localPosition.y, shortDescription.transform.localPosition.z);
			float nowAudioTime = maxAudioTime - audioTime;
			TimeSpan nts = TimeSpan.FromSeconds (nowAudioTime);
			startTime.GetComponent<Text>().text = nts.Seconds.ToString();
		}
	}

	// 画像をディスプレイに内接する最大サイズにリサイズしてセット
	void reseizeTexture(Texture2D tex) {
		double texWidth = tex.width;
		double texHeight = tex.height;
		double ratio = 1;

		// 4:3よりも縦長か横長か
		if (texWidth / texHeight >= displayWidth / displayHeight) {
			ratio = displayWidth / texWidth;
		}  else {
			ratio = displayHeight / texHeight;
		}

		double dWidth = (double)(texWidth * ratio);
		double dHeight = (double)(texHeight * ratio);
		int width = (int)Math.Ceiling (dWidth);
		int height = (int)Math.Ceiling (dHeight);

		TextureScale.Bilinear (tex, width, height);
		DisplaySprite.sprite = Sprite.Create (
			tex, 
			new Rect (0, 0, width, height), 
			new Vector2 (0.5f, 0.5f)
		);
	}

	void createLocalCache(ArticleData[] articles) {
		string json = LitJson.JsonMapper.ToJson(articles);
		PlayerPrefs.SetString("ONEWS_ARTICLES", json);
	}

	[System.Serializable]
	public class ArticleData
	{
		public string link;
		public string title;
		public string description;
		public string image;
		public string voice;
	}
}
