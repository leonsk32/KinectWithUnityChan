using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour {

    public Font _Font;

	// Use this for initialization
	void Start () {
        //できなくなったのでGUIから
        //_Font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void MakeTextBox(string objectName, string contentText, string fontColor, float fontSizeMultiplier,float fromX, float fromY, float toX, float toY)
    {
        //objectNameという名前の空オブジェクト生成
        GameObject textObj = new GameObject(objectName);
        //Canvasを親objectに
        textObj.transform.SetParent(GameObject.Find("Canvas").transform);

        //textコンポーネント
        textObj.AddComponent<Text>();
        Text textComponent = textObj.GetComponent<Text>();
        textComponent.text = "<color=" + fontColor + ">" + contentText + "</color>";
        textComponent.fontSize = (int)(Screen.height * (toY - fromY) * fontSizeMultiplier);
        textComponent.font = _Font;
        textComponent.alignment = TextAnchor.MiddleLeft;

        //RectTransformコンポーネントの設定．こっちは自動で追加されるらしいのでAddComponentは必要なし
        RectTransform rectComponent = textObj.GetComponent<RectTransform>();
        //親objectに対するアンカーの位置(0~1)
        //(fromX, fromY)はTextBoxの左下，(toX, toY)は右上
        rectComponent.anchorMin = new Vector2(fromX, fromY);
        rectComponent.anchorMax = new Vector2(toX, toY);
        rectComponent.anchoredPosition = new Vector2(0, 0);
    }

    public void ChangeTextBox(string objectName, string contentText, string fontColor)
    {
        GameObject textObj = GameObject.Find(objectName);
        Text textComponent = textObj.GetComponent<Text>();
        textComponent.text = "<color=" + fontColor + ">" + contentText + "</color>";
    }
}
