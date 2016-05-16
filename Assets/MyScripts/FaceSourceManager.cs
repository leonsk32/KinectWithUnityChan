using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using Microsoft.Kinect.Face;
using UnityEngine.UI;

public class FaceSourceManager : MonoBehaviour
{
    private KinectSensor _KinectSensor;
    private int bodyCount;
    private Body[] _Bodies;
    private FaceFrameSource[] _FaceFrameSources;
    private FaceFrameReader[] _FaceFrameReaders;

    private GameObject _Status;
    private bool bodyTrackingOnFlag = false;
    private bool happyFlag = false;
    private int targetPersonId;
    private FaceMove _FaceMove;

    void Start()
    {
        // one sensor is currently supported
        _KinectSensor = KinectSensor.GetDefault();

        // set the maximum number of bodies that would be tracked by Kinect
        bodyCount = _KinectSensor.BodyFrameSource.BodyCount;
        //Debug.Log(bodyCount); 6

        // allocate storage to store body objects
        _Bodies = new Body[bodyCount];

        // specify the required face frame results
        FaceFrameFeatures faceFrameFeatures =
            FaceFrameFeatures.BoundingBoxInColorSpace
                | FaceFrameFeatures.PointsInColorSpace
                | FaceFrameFeatures.BoundingBoxInInfraredSpace
                | FaceFrameFeatures.PointsInInfraredSpace
                | FaceFrameFeatures.RotationOrientation
                | FaceFrameFeatures.FaceEngagement
                | FaceFrameFeatures.Glasses
                | FaceFrameFeatures.Happy
                | FaceFrameFeatures.LeftEyeClosed
                | FaceFrameFeatures.RightEyeClosed
                | FaceFrameFeatures.LookingAway
                | FaceFrameFeatures.MouthMoved
                | FaceFrameFeatures.MouthOpen;

        // create a face frame source + reader to track each face in the FOV
        _FaceFrameSources = new FaceFrameSource[bodyCount];
        _FaceFrameReaders = new FaceFrameReader[bodyCount];
        for (int i = 0; i < bodyCount; i++)
        {
            // create the face frame source with the required face frame features and an initial tracking Id of 0
            _FaceFrameSources[i] = FaceFrameSource.Create(_KinectSensor, 0, faceFrameFeatures);

            // open the corresponding reader
            _FaceFrameReaders[i] = _FaceFrameSources[i].OpenReader();
        }

        _Status = GameObject.Find("Status");
        _FaceMove = this.GetComponent<FaceMove>();
    }

    void Update()
    {
        // get bodies either from BodySourceManager object get them from a BodyReader
        var bodySourceManager = this.GetComponent<BodySourceManager>();
        _Bodies = bodySourceManager.GetData();
        if (_Bodies == null) return;

        bodyTrackingOnFlag = false;
        // iterate through each body and update face source
        for (int i = 0; i < bodyCount; i++)
        {
            // check if a valid face is tracked in this face source				
            if (_FaceFrameSources[i].IsTrackingIdValid)
            {
                bodyTrackingOnFlag = true;//bodyがとれてさえいればtrue
                //顔が隠れていても，bodyがあればfaceFrameSourceのIdVaidはtrueになるっぽい
                using (FaceFrame frame = _FaceFrameReaders[i].AcquireLatestFrame())
                {
                    if (frame != null)
                    {
                        if (frame.TrackingId == 0)
                        {
                            continue;
                        }
                        
                        // do something with result
                        var result = frame.FaceFrameResult;
                        if (result.FaceProperties[FaceProperty.Happy].ToString() == "Yes")
                        {
                            happyFlag = true;
                            targetPersonId = i;
                            //Debug.Log(targetPersonId);
                            //Debug.Log("<color=red>Happy</color>");
                        }
                        if(targetPersonId == i && result.FaceProperties[FaceProperty.Happy].ToString() == "No")
                        {
                            //笑顔じゃなくなったら
                            //Happy判定頻度がUnityフレイム速度より遅いので，No判定を使わないと顔が安定しない
                            happyFlag = false;
                        }
                    }
                }
            }
            else
            {
                // check if the corresponding body is tracked 
                if (_Bodies[i].IsTracked)
                {
                    // update the face frame source to track this body
                    _FaceFrameSources[i].TrackingId = _Bodies[i].TrackingId;
                }
            }
        }

        if (bodyTrackingOnFlag) _Status.GetComponent<Text>().text = "<color=red>BODY TRACKED</color>";
        else _Status.GetComponent<Text>().text = "<color=black>BODY NOT FOUND</color>";
        if (happyFlag) _FaceMove.ChangeFace("eye_close@unitychan");
        else _FaceMove.ChangeFace("default@unitychan");
    }
}