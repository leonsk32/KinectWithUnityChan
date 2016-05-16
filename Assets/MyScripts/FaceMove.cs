using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FaceMove : MonoBehaviour {
    public AnimationClip[] _Animations;
    Animator _Animator;
    private float delayWeight = 0.3f;
    private bool isKeepFace = false;
    private float current = 0; //レイヤの重みづけ

    // Use this for initialization
    void Start () {
        _Animator = GetComponent<Animator>();
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetMouseButton(0))
        {
            current = 1;
        }
        else if (!isKeepFace)
        {
            current = Mathf.Lerp(current, 0, delayWeight);//3割ずつ減る
        }
        _Animator.SetLayerWeight(1, current);//1番目のレイヤの重みをcurrentで指定
    }

    public void ChangeFace(string str)
    {
        isKeepFace = true;
        current = 1;
        _Animator.CrossFade(str, 0);
        //Animation.Play()は指定したアニメーションに即座に切り替える
        //Animation.CrossFade()はやんわり補完
    }
}
