using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UniRx.Async;

namespace Entum
{
    public class MotionDataPlayer_RunTime : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        [ Tooltip("再生開始フレームを指定します。0だとファイル先頭から開始です")]
        private int _startFrame=0;
        [SerializeField]
        private bool _playing;
        [SerializeField]
        private int _frameIndex;

        [SerializeField, Tooltip("普段はOBJECTROOTで問題ないです。特殊な機材の場合は変更してください")]
        private MotionDataSettings.Rootbonesystem _rootBoneSystem = MotionDataSettings.Rootbonesystem.Objectroot;
        [SerializeField, Tooltip("rootBoneSystemがOBJECTROOTの時は使われないパラメータです。")]
        private HumanBodyBones _targetRootBone = HumanBodyBones.Hips;

        [SerializeField, Tooltip("スラッシュで終わる形で")]
        private string _recordedDirectory;
        [SerializeField, Tooltip("拡張子も")]
        private string _recordedFileName;

        private HumanPoseHandler _poseHandler;
        private Action _onPlayFinish;
        private float _playingTime;

        const int bufferSize = 512;
        HumanoidPoses.SerializeHumanoidPose[] poseBuffer = new HumanoidPoses.SerializeHumanoidPose[bufferSize];
        int bufferReadingPosition;
        int fileReadingPosition;
        ulong bufferReadCount;
        ulong fileReadCount;
        string motionCSVPath;

        private void Awake()
        {
            if (_animator == null)
            {
                Debug.LogError("MotionDataPlayerにanimatorがセットされていません。MotionDataPlayerを削除します。");
                Destroy(this);
                return;
            }


            _poseHandler = new HumanPoseHandler(_animator.avatar, _animator.transform);
            _onPlayFinish += StopMotion;
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(_recordedDirectory))
            {
                _recordedDirectory = Application.streamingAssetsPath + "/";
            }

            motionCSVPath = _recordedDirectory + _recordedFileName;
        }

        private void LateUpdate()
        {
            if (!_playing)
            {
                return;
            }


            _playingTime += Time.deltaTime;
            SetHumanPose();
        }

        /// <summary>
        /// モーションデータ再生開始
        /// </summary>
        public void PlayMotion()
        {
            if (_playing)
            {
                return;
            }

            _playingTime = _startFrame * (Time.deltaTime / 1f);
            _frameIndex = _startFrame;
            fileReadCount = 0;
            fileReadingPosition = 0;
            bufferReadCount = 0;
            bufferReadingPosition = 0;
            UniTask.Run(loadAnimationFileAsync);
            _playing = true;
        }

        /// <summary>
        /// モーションデータ再生終了。フレーム数が最後になっても自動で呼ばれる
        /// </summary>
        public void StopMotion()
        {
            if (!_playing)
            {
                return;
            }


            _playingTime = 0f;
            _frameIndex = _startFrame;
            _playing = false;


        }

        async UniTask loadAnimationFileAsync()
        {
            //ファイルが存在しなければ終了
            if (!File.Exists(motionCSVPath))
            {
                _playing = false;
                return;
            }

            FileStream fs = null;
            StreamReader sr = null;

            try
            {
                fs = new FileStream(motionCSVPath, FileMode.Open);
                sr = new StreamReader(fs);
                //バッファーへの書き込み
                string line;
                var seriHumanPose = new HumanoidPoses.SerializeHumanoidPose();
                while(_playing&&sr.Peek()>-1)
                {
                    await UniTask.WaitUntil(() => fileReadCount < bufferReadCount+bufferSize);
                    line = sr.ReadLine();
                    if(line!="")
                    {
                        seriHumanPose.DeserializeCSV(line);
                        poseBuffer[fileReadingPosition] = seriHumanPose;
                        Debug.Log("書き込んだ");
                    }
                    fileReadingPosition++;
                    fileReadCount++;
                    if(fileReadingPosition>=bufferSize)
                    {
                        fileReadingPosition = 0;
                    }
                }
                sr.Close();
                fs.Close();
                sr = null;
                fs = null;
            }
            catch(System.Exception e)
            {
                Debug.LogError("ファイル読み込み失敗！" + e.Message + e.StackTrace);
            }
            finally
            {
                sr.Close();
                fs.Close();
                sr = null;
                fs = null;
            }
        }

        private void SetHumanPose()
        {
            var pose = new HumanPose();
            pose.muscles = poseBuffer[bufferReadingPosition].Muscles;
            _poseHandler.SetHumanPose(ref pose);
            pose.bodyPosition = poseBuffer[bufferReadingPosition].BodyPosition;
            pose.bodyRotation = poseBuffer[bufferReadingPosition].BodyRotation;

            switch (_rootBoneSystem)
            {
                case MotionDataSettings.Rootbonesystem.Objectroot:
                    //_animator.transform.localPosition = RecordedMotionData.Poses[_frameIndex].BodyRootPosition;
                    //_animator.transform.localRotation = RecordedMotionData.Poses[_frameIndex].BodyRootRotation;
                    break;

                case MotionDataSettings.Rootbonesystem.Hipbone:
                    pose.bodyPosition = poseBuffer[bufferReadingPosition].BodyPosition;
                    pose.bodyRotation = poseBuffer[bufferReadingPosition].BodyRotation;

                    _animator.GetBoneTransform(_targetRootBone).position = poseBuffer[bufferReadingPosition].BodyRootPosition;
                    _animator.GetBoneTransform(_targetRootBone).rotation = poseBuffer[bufferReadingPosition].BodyRootRotation;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            //処理落ちしたモーションデータの再生速度調整
            if(_playingTime > poseBuffer[bufferReadingPosition].Time)
            {
                bufferReadingPosition++;
                bufferReadCount++;
            }
            if (bufferReadingPosition >= bufferSize)
            {
                bufferReadingPosition = 0;
            }
            if (bufferReadCount>fileReadCount)
            {
                StopMotion();
            }
        }
    }
}