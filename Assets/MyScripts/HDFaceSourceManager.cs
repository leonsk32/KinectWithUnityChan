using UnityEngine;
using System;//enumに必要
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using Microsoft.Kinect.Face;
using UnityEngine.UI;//UIに必要

public class HDFaceSourceManager : MonoBehaviour
{
    /* Kinect関連 */
    private KinectSensor _KinectSensor;
    private int bodyCount;
    private Body[] _Bodies;

    //顔の特徴点（目，鼻，口）
    private FaceAlignment _FaceAlignment = null;
    //顔モデル
    private FaceModel _FaceModel = null;
    //顔モデルの作成を管理
    private FaceModelBuilder _FaceModelBuilder = null;
    //顔情報データ取得元
    private HighDefinitionFaceFrameSource _HDFaceFrameSource = null;
    //顔情報データ用のFrameReader
    private HighDefinitionFaceFrameReader _HDFaceFrameReader = null;

    /* その他 */
    private UIManager _UIManager;
    private bool isTracked = false;
    private FaceMove _FaceMove;

    /* Blender Shape */
    public SkinnedMeshRenderer _MthDef;
    public SkinnedMeshRenderer _BlwDef;
    public SkinnedMeshRenderer _EyeDef;
    public SkinnedMeshRenderer _ElDef;

    void Start()
    {
        //Kinectセンサを取得
        _KinectSensor = KinectSensor.GetDefault();

        //Kinectで取得できる最大body数
        bodyCount = _KinectSensor.BodyFrameSource.BodyCount;
        _Bodies = new Body[bodyCount];

        //FaceTracking関連の初期化
        InitializeHDFace();

        //FaceSync関連の初期化
        InitializeFaceSync();

        _UIManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        _FaceMove = this.GetComponent<FaceMove>();

        //UIの初期化
        InitializeUI();
    }

    void InitializeHDFace()
    {
        //コンストラクタがないらしく，= new FaceAlignment();ではだめ．Create()を使う
        _FaceAlignment = FaceAlignment.Create();
        _FaceModel = FaceModel.Create();
        _HDFaceFrameSource = HighDefinitionFaceFrameSource.Create(_KinectSensor);
        if (_HDFaceFrameSource == null) Debug.Log("Cannot create HD Face Frame Source");
        _HDFaceFrameReader = _HDFaceFrameSource.OpenReader();
        _FaceModelBuilder = _HDFaceFrameSource.OpenModelBuilder(FaceModelBuilderAttributes.SkinColor | FaceModelBuilderAttributes.HairColor);
        if (_FaceModelBuilder == null) Debug.Log("Cannot open Face Model Builder");
    }

    void InitializeUI()
    {
        _UIManager.MakeTextBox("Status", "START", "black", 0.5f, 0.1f, 0.9f, 0.2f, 1);

        int count = 0;
        //構造体の名前の一覧を取得
        foreach (string name in Enum.GetNames(typeof(FaceShapeAnimations)))
        {
            _UIManager.MakeTextBox(name, name, "black", 0.8f, 0.7f, 1 - (count + 1) * 0.03f, 0.8f, 1 - count * 0.03f);
            _UIManager.MakeTextBox(name + "Value", "0", "black", 0.8f, 0.9f, 1 - (count + 1) * 0.03f, 1, 1 - count * 0.03f);
            count++;
        }
    }

    void InitializeFaceSync()
    {
        _MthDef = GameObject.Find("MTH_DEF").GetComponent<SkinnedMeshRenderer>();
        _EyeDef = GameObject.Find("EYE_DEF").GetComponent<SkinnedMeshRenderer>();
        _ElDef = GameObject.Find("EL_DEF").GetComponent<SkinnedMeshRenderer>();
    }

    void Update()
    {
        //bodySourceManagerからbody情報を取得
        var bodySourceManager = this.GetComponent<BodySourceManager>();
        _Bodies = bodySourceManager.GetData();
        if (_Bodies == null) return;

        //_HDFaceFrameSourceのIDを最も頭がKinectに近いBodyのIDにする
        FindClosestBody(_Bodies);

        using (HighDefinitionFaceFrame frame = _HDFaceFrameReader.AcquireLatestFrame())
        {
            //LatestFrameはUnityのUpdate毎に必ずあるわけではない．2~3Updateにつき1frameっぽい
            if (frame != null)
            {
                //LatestFrameが必ずあるわけではないので，ここでisTrackedを書き換える
                //bodyが見つかっている≒frame.IsFaceTracked=ture っぽい．
                //顔が隠れててもframe.IsFaceTrackedはtrue
                isTracked = frame.IsFaceTracked;

                //FaceAlignmentを更新
                if (frame.IsFaceTracked) frame.GetAndRefreshFaceAlignmentResult(_FaceAlignment);
                //各種FaceShapeAnimation.以下の値を更新
                UpdateFaceInformations();
            }
        }


        if (isTracked) _UIManager.ChangeTextBox("Status", "TRACKED", "red");
        else _UIManager.ChangeTextBox("Status", "NOT FOUND", "black");
    }

    void FindClosestBody(Body[] _Bodies)
    {
        float closestDistance2 = float.MaxValue;
        Body closestBody = null;
        foreach (Body body in _Bodies)
        {
            if (body == null) continue;
            if (!body.IsTracked) continue;

            Windows.Kinect.Joint head = body.Joints[JointType.Head];
            if (head.TrackingState == TrackingState.NotTracked) continue;
            CameraSpacePoint headPoint = head.Position;
            float distance2 = headPoint.X * headPoint.X +
                              headPoint.Y * headPoint.Y +
                              headPoint.Z * headPoint.Z;
            if (closestDistance2 <= distance2) continue;
            closestDistance2 = distance2;
            closestBody = body;
        }
        if (closestBody == null) return;
        ulong trackingID = closestBody.TrackingId;
        if (_HDFaceFrameSource == null) return;
        _HDFaceFrameSource.TrackingId = trackingID;
    }

    void UpdateFaceInformations()
    {
        foreach (FaceShapeAnimations name in Enum.GetValues(typeof(FaceShapeAnimations)))
        {
            _UIManager.ChangeTextBox(name.ToString() + "Value", (Mathf.Floor(_FaceAlignment.AnimationUnits[name] * Mathf.Pow(10, 3)) / Mathf.Pow(10, 3)).ToString(), "black");
        }

        //Animationと表情（に伴ってBlendShapeも）が紐づいてるので，Animatorは切る必要がある
        _MthDef.SetBlendShapeWeight(0, (int)(_FaceAlignment.AnimationUnits[FaceShapeAnimations.JawOpen] * 100));
        var value = (int)(_FaceAlignment.AnimationUnits[FaceShapeAnimations.LefteyeClosed] + _FaceAlignment.AnimationUnits[FaceShapeAnimations.RighteyeClosed]);
        _EyeDef.SetBlendShapeWeight(6, value * 100);
        _ElDef.SetBlendShapeWeight(6, value * 100);
    }
}
