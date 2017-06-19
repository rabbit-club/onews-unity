using UnityEngine;
using System.Collections;
using System.Linq;

public class UnityChanTouch : TapBehaviour {

	Animator anim;
	SkinnedMeshRenderer skinMesh;
	float rndAnimTimer = 0.0f;
	float rndAnimRangeMin = 10.0f;
	float rndAnimRangeMax = 20.0f;
	float mouseTimer = 0.0f;
	bool mouseClose = true;
	public bool useLip = false;
	bool nowAnimation = false;

	void Start() {
		anim = GetComponent<Animator>();
		skinMesh = this.GetComponentsInChildren<SkinnedMeshRenderer>().First(s => s.name == "MTH_DEF");
//		useLip = true; //デバッグ用
		rndAnimTimer = Random.Range(5.0f, 10.0f);
	}

	// タッチしたときに呼ばれる。
	public override void TapDown(ref RaycastHit hit) {
		// アニメーションしたのでランダムアニメーションの値は再取得する
		rndAnimTimer = Random.Range(rndAnimRangeMin, rndAnimRangeMax);
		// アニメーションさせる
		unityChanAnimation();
	}
	
	void Update() {
		if(anim != null) {
			rndAnimTimer -= Time.deltaTime;
			if(anim.GetCurrentAnimatorStateInfo(0).IsName("FIRST_WAIT")) {
				// 初めのポーズを取るため一瞬動かしてからアニメーションをストップさせ口パクに譲る
				if(anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.3f) {
					anim.Stop();
				}
			} else if(anim.GetCurrentAnimatorStateInfo(0).IsName("WAIT00")) {
				if(nowAnimation) {
					anim.Stop();
					nowAnimation = false;
				}
			}

			if(rndAnimTimer <= 0.0f) {
				unityChanAnimation();
				rndAnimTimer = Random.Range(rndAnimRangeMin, rndAnimRangeMax);
			}
		}
		if(useLip) {
			// 口パク
			mouseTimer += Time.deltaTime;
			if (mouseTimer > 0.1f) {
				paku();
				mouseTimer = 0;
			}
		}
	}

	void paku() {
		if (mouseClose) {
			skinMesh.SetBlendShapeWeight(0, 0.0f);
		} else {
			skinMesh.SetBlendShapeWeight(0, 50.0f);
		}
		mouseClose = !mouseClose;
	}

	void unityChanAnimation() {
		anim.Rebind();
		nowAnimation = true;
		int animIndex = Random.Range(0, 8); // intの場合max値は含まない
		switch (animIndex) {
		case 0:
			anim.Play("WIN00");
			break;
		case 1:
			anim.Play("WAIT01");
			break;
		case 2:
			anim.Play("WAIT02");
			break;
		case 3:
			anim.Play("WAIT03");
			break;
		case 4:
			anim.Play("WAIT04");
			break;
		case 5:
			anim.Play("DAMAGED00");
			break;
		case 6:
			anim.Play("LOSE00");
			break;
		case 7:
			anim.Play("JUMP01B");
			break;
		default:
			break;
		}
	}
}
