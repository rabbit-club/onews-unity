﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Advertisements;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityChan;
using LitJson;

public class MainController : MonoBehaviour
{
	string articlesUrlListURL = "https://www.dropbox.com/s/a5qvgdcx1sdzbwb/articles_url_list.json?dl=1";
	string emptyImage = "Images/main_empty";
	int articleHunkCount = 6;

	// アプリ内ディスプレイサイズ
	double displayWidth = 1280;
	double displayHeight = 960;

	// アプリ画面幅
	float windowWidth = 1080.0f;

	// 字幕フォント幅
	float fontWidth = 40.0f;

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
	Text infoSourceText;

	GameObject startTime;
	GameObject endTime;
	GameObject seekedBar;

	AudioSource audioSource;
	AudioSource bgmSource;
	AudioSource[] bgmSources;
	float audioTime;
	float maxAudioTime;
	UIController uiController;

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
		infoSourceText = GameObject.Find ("Footer/Fixed View/Source/Text").GetComponent<Text> ();

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

		DateTime now = DateTime.Now;
		int hour = now.Hour;
		int timeZoneBGM;
		int timeZoneVoice;
		if (hour >= 6 && hour < 12) {
			// 朝
			timeZoneBGM = 1;
			timeZoneVoice = 1;
		} else if (hour >= 12 && hour < 18) {
			// 昼
			timeZoneBGM = 2;
			timeZoneVoice = 2;
		} else if (hour >= 18 && hour < 24) {
			// 夜
			timeZoneBGM = 2;
			timeZoneVoice = 3;
		} else {
			// 深夜
			timeZoneBGM = 3;
			timeZoneVoice = 3;
		}

		bgmSource = GameObject.Find ("BGM").GetComponent<AudioSource>();
		bgmSource.clip = Resources.Load("BGM/bgm_" + timeZoneBGM) as AudioClip;
		bgmSource.Play();

		AudioClip seStart = Resources.Load ("SE/start", typeof(AudioClip)) as AudioClip;
		audioSource.PlayOneShot(seStart);
		yield return new WaitForSeconds(seStart.length);

		AudioClip voiceA = Resources.Load("Voices/a_" + timeZoneVoice, typeof(AudioClip)) as AudioClip;
		audioSource.PlayOneShot(voiceA);
		audioTime += voiceA.length + 0.5f;

		yield return new WaitForSeconds (audioTime);

		int voiceType = UnityEngine.Random.Range (0, 4);
		int voiceNumber;
		if (voiceType == 0) {
			voiceNumber = UnityEngine.Random.Range (0, 15);
		} else {
			voiceType = timeZoneVoice;
			voiceNumber = UnityEngine.Random.Range (0, 5);
		}

		AudioClip voiceB = Resources.Load("Voices/b_" + voiceType + "_" + voiceNumber, typeof(AudioClip)) as AudioClip;
		audioSource.PlayOneShot(voiceB);
		audioTime += voiceB.length + 0.5f; // 挨拶音声の時間を初回のウェイトにする

		// JSON List取得
		WWW wwwArticlesUrlList = new WWW (articlesUrlListURL);
		yield return wwwArticlesUrlList;
		LitJson.JsonData articlesUrlList = JsonMapper.ToObject(wwwArticlesUrlList.text);

		// 新しい順に並び替え
		var articlesUrlListToday = new Dictionary<string, string>();
		var articlesUrlListYesterday = new Dictionary<string, string>();
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
			int itemNumber = 0;
			foreach (var article in articlesHunk) {
				// 画像を取得する
				if (article.twitterImage != "" && checkExtension(article.twitterImage)) {
					WWW wwwImage = new WWW (article.twitterImage);
					yield return wwwImage;
					article.texture = wwwImage.texture;
				} else if (article.image != "" && checkExtension(article.image)) {
					WWW wwwImage = new WWW (article.image);
					yield return wwwImage;
					article.texture = wwwImage.texture;
				} else {
					// 画像なし画像
					article.texture = Resources.Load (emptyImage) as Texture2D;
				}

				scrollController.setItem (itemNumber, article.title, article.timeString, article.texture, article.link);
				itemNumber++;
			}

			if (!adFinished) {
				int cmVoiceNumber = UnityEngine.Random.Range (0, 15);
				AudioClip voiceC = Resources.Load("Voices/c_" + cmVoiceNumber, typeof(AudioClip)) as AudioClip;
				audioSource.PlayOneShot(voiceC);
				audioTime += voiceC.length + 0.5f;
				yield return new WaitForSeconds (audioTime);

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

			// 記事テキストの表示
			if (shortDescription != null) {
				// 位置を初期化
				shortDescription.transform.localPosition = new Vector3 (windowWidth, shortDescription.transform.localPosition.y, shortDescription.transform.localPosition.z);
				shortDescription.GetComponent<Text> ().text = article.text;

				// テキストスクロール
				int textBites = Encoding.GetEncoding("Shift_JIS").GetByteCount(article.text);
				float moveAmount = fontWidth / 2 * textBites + windowWidth;
				iTween.MoveAdd(shortDescription, iTween.Hash("x", -moveAmount, "easeType", "linear", "time", maxAudioTime));
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

					var infoSourceTextColor = infoSourceText.color;
					infoSourceTextColor.a = 255;
					infoSourceText.color = infoSourceTextColor;
				}
			}

			// シークバーを動かす
			seekedBar.transform.position = new Vector3 (-windowWidth, seekedBar.transform.position.y, seekedBar.transform.position.z);
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

		while (!www.isDone) {
			// ダウンロード中
			yield return null;
		}

		if (!string.IsNullOrEmpty (www.error)) {
			// ダウンロードでエラーが発生した
		} else {
			// ダウンロードが正常に完了した
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
			float nowAudioTime = maxAudioTime - audioTime;
			TimeSpan nts = TimeSpan.FromSeconds (nowAudioTime);
			var seconds = nts.Seconds;
			if (seconds >= 0) {
				startTime.GetComponent<Text> ().text = "0:" + seconds.ToString ().PadLeft (2, '0');
			}
		} else {
			unityChanTouch.useLip = false;
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
		} else {
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

	bool checkExtension(string url){
		string ext = System.IO.Path.GetExtension(url);
		if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
			return true;
		}

		return false;
	}

	[System.Serializable]
	public class ArticleData
	{
		public String link;
		public String title;
		public String description;
		public String text;
		public String image;
		public String itemImage;
		public String twitterImage;
		public String voice;
		public Texture2D texture;
		public Int32 time;
		public String timeString;
	}
}
