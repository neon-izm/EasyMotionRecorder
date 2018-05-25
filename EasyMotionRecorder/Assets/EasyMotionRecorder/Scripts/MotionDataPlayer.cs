/**
[EasyMotionRecorder]

Copyright (c) 2018 Duo.inc

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;

namespace Entum
{
    /// <summary>
    /// モーションデータ再生クラス
    /// DynamicBoneやSpringBoneやBulletPhysincsImplなどの揺れ物アセットのScript Execution Orderを20000など
    /// 大きな値にしてください。
    /// DefaultExecutionOrder(11000) はVRIK系より処理順を遅くする、という意図です
    /// </summary>
    [DefaultExecutionOrder(11000)]
    public class MotionDataPlayer : MonoBehaviour
    {
        [SerializeField] KeyCode _playStartKey = KeyCode.S;
        [SerializeField] KeyCode _playStopKey = KeyCode.T;

        [SerializeField] protected HumanoidPoses _recordedMotionData;

        [SerializeField] Animator _animator;

        HumanPoseHandler _poseHandler;

        System.Action _onPlayFinish;

        [SerializeField] [Tooltip("再生開始フレームを指定します。0だとファイル先頭から開始です")]
        int _startFrame = 0;

        [SerializeField] bool _playing = false;
        [SerializeField] int _frameIndex = 0;

        [SerializeField] [Tooltip("普段はOBJECTROOTで問題ないです。特殊な機材の場合は変更してください")]
        MotionDataSettings.Rootbonesystem _rootBoneSystem = MotionDataSettings.Rootbonesystem.Objectroot;

        [SerializeField] [Tooltip("rootBoneSystemがOBJECTROOTの時は使われないパラメータです。")]
        HumanBodyBones _targetRootBone = HumanBodyBones.Hips;


        float _playingTime = 0;

        /// <summary>
        /// モーションデータ再生開始
        /// </summary>
        public void PlayMotion()
        {
            if (_playing == false)
            {
                if (_recordedMotionData == null)
                {
                    Debug.LogError("録画済みモーションデータが指定されていません。再生を行いません。");
                }
                else
                {
                    _playingTime = _startFrame * (Time.deltaTime / 1f);
                    _frameIndex = _startFrame;
                    _playing = true;
                }
            }
        }

        /// <summary>
        /// モーションデータ再生終了。フレーム数が最後になっても自動で呼ばれる
        /// </summary>
        public void StopMotion()
        {
            if (_playing)
            {
                _playingTime = 0;
                _frameIndex = _startFrame;
                _playing = false;
            }
        }


        void SetHumanPose(int frame)
        {
            var pose = new HumanPose();
            pose.muscles = _recordedMotionData.Poses[_frameIndex].Muscles;
            _poseHandler.SetHumanPose(ref pose);
            pose.bodyPosition = _recordedMotionData.Poses[_frameIndex].BodyPosition;
            pose.bodyRotation = _recordedMotionData.Poses[_frameIndex].BodyRotation;

            if (_rootBoneSystem == MotionDataSettings.Rootbonesystem.Objectroot)
            {
                //animator.transform.localPosition = recordedMotionData.poses[frameIndex].bodyRootPosition;
                //animator.transform.localRotation = recordedMotionData.poses[frameIndex].bodyRootRotation;
            }
            else if (_rootBoneSystem == MotionDataSettings.Rootbonesystem.Hipbone)
            {
                pose.bodyPosition = _recordedMotionData.Poses[_frameIndex].BodyPosition;
                pose.bodyRotation = _recordedMotionData.Poses[_frameIndex].BodyRotation;

                _animator.GetBoneTransform(_targetRootBone).position =
                    _recordedMotionData.Poses[_frameIndex].BodyRootPosition;
                _animator.GetBoneTransform(_targetRootBone).rotation =
                    _recordedMotionData.Poses[_frameIndex].BodyRootRotation;
            }

            //処理落ちしたモーションデータの再生速度調整
            if (_playingTime > _recordedMotionData.Poses[_frameIndex].Time)
            {
                _frameIndex++;
            }

            if (_frameIndex == _recordedMotionData.Poses.Count - 1)
            {
                if (_onPlayFinish != null)
                {
                    _onPlayFinish();
                }
            }
        }


        void Awake()
        {
            if (_animator == null)
            {
                Debug.LogError("MotionDataPlayerにanimatorがセットされていません。MotionDataPlayerを削除します。");
                Destroy(this);
            }

            _poseHandler = new HumanPoseHandler(_animator.avatar, (_animator.transform));
            _onPlayFinish += StopMotion;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(_playStartKey))
            {
                PlayMotion();
            }

            if (Input.GetKeyDown(_playStopKey))
            {
                StopMotion();
            }
        }

        private void LateUpdate()
        {
            if (!_playing) return;
            _playingTime += Time.deltaTime;
            SetHumanPose(_frameIndex);
        }
    }
}