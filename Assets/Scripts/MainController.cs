using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Advertisements;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityChan;
using LitJson;

public class MainController : MonoBehaviour
{
	string articlesUrlListURL = "https://www.dropbox.com/s/a5qvgdcx1sdzbwb/articles_url_list.json?dl=1";
	string emptyImage = "Assets/Images/main_empty.png";
	int articleHunkCount = 6;

	// ディスプレイサイズ
	double displayWidth = 400;
	double displayHeight = 300;

	bool adFinished = true;

	GameObject UnityChan;
	UnityChanTouch unityChanTouch;
	public FaceUpdate faceUpdate;
	public GameObject Display;
	SpriteRenderer DisplaySprite;

	GameObject shortDescription;
	Text infoTitle;
	Text infoTime;
	Image infoSource;

	GameObject startTime;
	GameObject endTime;
	GameObject seekedBar;

	AudioSource audioSource;
	AudioSource bgmSource;
	float audioTime;
	float maxAudioTime;
	UIController uiController;

//	bool isOffLine = false;

//	public void Movie ()
	void Start()
	{
		StartCoroutine (MovieStart ());
	}

	IEnumerator MovieStart ()
	{
		UnityChan = GameObject.Find ("unitychan");
		unityChanTouch = UnityChan.GetComponent<UnityChanTouch>();
		DisplaySprite = Display.GetComponent<SpriteRenderer> ();

		shortDescription = GameObject.Find ("Footer/subtitles/Text");
		infoTitle = GameObject.Find ("Footer/Fixed View/Title").GetComponent<Text> ();
		infoTime = GameObject.Find ("Footer/Fixed View/Time").GetComponent<Text> ();
		infoSource = GameObject.Find ("Footer/Fixed View/Source").GetComponent<Image> ();

		startTime = GameObject.Find ("Footer/Seekbar/Time");
		endTime = GameObject.Find ("Footer/Seekbar/EndTime");
		seekedBar = GameObject.Find ("Footer/Seekbar/SeekedBar");

		GameObject scrollContent = GameObject.Find ("Viewport/Content");
		ScrollController scrollController = scrollContent.GetComponent<ScrollController> ();
		RectTransform scrollContentTransform = scrollContent.GetComponent<RectTransform> ();

		uiController = GameObject.Find ("UICamera").GetComponent<UIController>();

		audioSource = GetComponent<AudioSource> ();
		audioTime = 0.0f;
		maxAudioTime = 0.0f;

		bgmSource = GameObject.Find ("BGM").GetComponent<AudioSource>();

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

		// 新しい順に並び替え
		var articlesUrlListToday = new Dictionary<string, string>();
		var articlesUrlListYesterday = new Dictionary<string, string>();
		DateTime now = DateTime.Now;
		Int32 nowInt = Int32.Parse (now.ToString ("HHmm"));
		foreach (var key in articlesUrlList.Keys) {
			if (Int32.Parse (key) >= nowInt) {
				articlesUrlListYesterday.Add (key, articlesUrlList [key].ToString ());
			} else {
				articlesUrlListToday.Add (key, articlesUrlList [key].ToString ());
			}
		}
		var articlesUrlListSorted = articlesUrlListToday.Concat(articlesUrlListYesterday).ToDictionary(x => x.Key, x => x.Value);

		List<ArticleData> articlesHunk = new List<ArticleData>();

		foreach (var key in articlesUrlListSorted.Keys) {
			string articlesUrl = Convert.ToString(articlesUrlList[key]);
			if (String.IsNullOrEmpty(articlesUrl)) {
				continue;
			}

			// JSON取得
			WWW wwwArticles = new WWW (Convert.ToString(articlesUrl));
			yield return wwwArticles;
			ArticleData[] articles = JsonMapper.ToObject<ArticleData[]> (wwwArticles.text);

			foreach (var article in articles) {
				DateTime articleDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(article.time).ToLocalTime();

				// 更新に失敗した古い記事は飛ばす
				if (articleDateTime < now.AddDays(-1)) {
					continue;
				}

				article.timeString = articleDateTime.ToString("yyyy/MM/dd HH:mm:ss");
				articlesHunk.Add (article);
			}

			// 記事数が一定量になるまで溜める
			if (articlesHunk.Count < articleHunkCount) {
				continue;
			}

			// 先に前の周のitemを削除
			GameObject[] oldListItems = GameObject.FindGameObjectsWithTag("ListItem");
			foreach (GameObject item in oldListItems) {
				Destroy (item);
			}

			// リストの長さを合わせる
			scrollContentTransform.localPosition = new Vector2 (0, 0);
			Vector2 scrollContentSize = scrollContentTransform.sizeDelta;
			scrollContentTransform.sizeDelta = new Vector2 (scrollContentSize.x, articlesHunk.Count * 200.0f);

			// リストのitem生成
			// TODO: 切り替わるタイミングが早いため修正
			int itemNumber = 0;
			foreach (var article in articlesHunk) {
				// 画像を取得する
				if (article.image == "") {
					// 画像なし画像
					article.texture = ReadTexture (emptyImage, (int)displayWidth, (int)displayHeight);
				} else {
					WWW wwwImage = new WWW (article.image);
					yield return wwwImage;
					article.texture = wwwImage.texture;
				}

				scrollController.setItem (itemNumber, article.title, article.timeString, article.texture, article.link);
				itemNumber++;
			}

			// ローカルキャッシュを作成する
//			createLocalCache(articles);
//			if(isOffLine) {
//				return false;
//			}

			if (!adFinished) {
				// TODO: 広告入りのセリフ
				// 広告再生
				yield return StartCoroutine (showAd());
			}

			// 広告が終わるまで待つ
			while (!adFinished) {
				yield return new WaitForSeconds (0.5f);
			}
			adFinished = false;

			// ニュースを読む
			yield return StartCoroutine (readNews(articlesHunk));

			articlesHunk = new List<ArticleData>();
		}
	}

	IEnumerator readNews (List<ArticleData> articlesHunk)
	{
		foreach (var article in articlesHunk) {
			// 音声の取得と再生
			yield return StartCoroutine (download (article.voice));

			// 画像を表示
			DisplaySprite.sprite = reseizeTexture(article.texture);

			// 要約記事テキストの表示
			if (shortDescription != null) {
				// 位置を初期化
				shortDescription.transform.localPosition = new Vector3 (5500.0f, shortDescription.transform.localPosition.y, shortDescription.transform.localPosition.z);
				shortDescription.GetComponent<Text> ().text = article.title + " " + article.description;
			}

			// 記事タイトルの表示
			if (infoTitle != null) {
				infoTitle.text = article.title;
				infoTime.text = article.timeString;
				uiController.currentUrl = article.link;

				if (infoSource.color.a == 0) {
					var infoSourceColor = infoSource.color;
					infoSourceColor.a = 255;
					infoSource.color = infoSourceColor;
				}
			}

			// シークバーを動かす
			seekedBar.transform.position = new Vector3 (-1080, seekedBar.transform.position.y, seekedBar.transform.position.z);
			iTween.MoveTo (seekedBar, iTween.Hash ("position", new Vector3 (0, seekedBar.transform.position.y, seekedBar.transform.position.z), "time", maxAudioTime, "easeType", "linear"));

			// 音声時間maxの表示
			TimeSpan maxTs = TimeSpan.FromSeconds (maxAudioTime);
			endTime.GetComponent<Text> ().text = "0:" + maxTs.Seconds.ToString().PadLeft(2, '0');

			yield return new WaitForSeconds (audioTime);
		}
	}

	IEnumerator download (string filePathUrl)
	{
		WWW www = new WWW (filePathUrl);

		while (!www.isDone) { // ダウンロードの進捗を表示
//			print (Mathf.CeilToInt (www.progress * 100));
			yield return null;
		}

		if (!string.IsNullOrEmpty (www.error)) { // ダウンロードでエラーが発生した
			Debug.Log ("error:" + www.error);
		} else { // ダウンロードが正常に完了した
			string filePath = Application.persistentDataPath + "/" + Path.GetFileName (www.url);
			File.WriteAllBytes (filePath, www.bytes);
			Debug.Log ("download file write success." + filePath);
			audioSource.clip = www.GetAudioClip(false, true, AudioType.MPEG);
			audioSource.Play();
			// 音声の時間を保存しておく
			maxAudioTime = audioSource.clip.length;
			audioTime = maxAudioTime;
		}
	}

	IEnumerator showAd ()
	{
		if (Advertisement.IsReady()) {
			Advertisement.Show(null, new ShowOptions {
				resultCallback = result => {
					switch (result) {
						case ShowResult.Finished:
						case ShowResult.Skipped:
						case ShowResult.Failed:
						default:
							adFinished = true;
							break;
					}
				}
			});
		}

		yield return null; 
	}

	void Update ()
	{
		if (audioSource != null && audioSource.isPlaying && audioTime >= 0.0f) {
			// 口パク
			unityChanTouch.useLip = true;

			audioTime -= Time.deltaTime;
			shortDescription.transform.localPosition = new Vector3 (
				shortDescription.transform.localPosition.x - (Time.deltaTime * 240.0f), 
				shortDescription.transform.localPosition.y, 
				shortDescription.transform.localPosition.z
			);
			float nowAudioTime = maxAudioTime - audioTime;
			TimeSpan nts = TimeSpan.FromSeconds (nowAudioTime);
			var seconds = nts.Seconds;
			if (seconds >= 0) {
				startTime.GetComponent<Text>().text = "0:" + seconds.ToString().PadLeft(2, '0');
			}
		}
	}

	// 画像をディスプレイに内接する最大サイズにリサイズしてセット
	Sprite reseizeTexture(Texture2D texture) {
		double texWidth = texture.width;
		double texHeight = texture.height;
		double ratio = 1;

		// ディスプレイの比率よりも縦長か横長か
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

	public void volumeOn() {
		audioSource.volume = 1;
		bgmSource.volume = 1;
	}

	public void volumeOff() {
		audioSource.volume = 0;
		bgmSource.volume = 0;
	}

	void createLocalCache(ArticleData[] articles) {
		string json = LitJson.JsonMapper.ToJson(articles);
		PlayerPrefs.SetString("ONEWS_ARTICLES", json);
	}

	byte[] ReadPngFile(string path){
		FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
		BinaryReader bin = new BinaryReader(fileStream);
		byte[] values = bin.ReadBytes((int)bin.BaseStream.Length);

		bin.Close();

		return values;
	}

	Texture2D ReadTexture(string path, int width, int height){
		byte[] readBinary = ReadPngFile(path);

		Texture2D texture = new Texture2D(width, height);
		texture.LoadImage(readBinary);

		return texture;
	}

	[System.Serializable]
	public class ArticleData
	{
		public String link;
		public String title;
		public String description;
		public String image;
		public String voice;
		public Texture2D texture;
		public Int32 time;
		public String timeString;
	}
}
