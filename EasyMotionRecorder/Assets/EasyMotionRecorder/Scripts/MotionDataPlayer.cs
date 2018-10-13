/**
[EasyMotionRecorder]

Copyright (c) 2018 Duo.inc

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System;

namespace Entum
{
    /// <summary>
    /// モーションデータ再生クラス
    /// SpringBone, DynamicBone, BulletPhysicsImplなどの揺れ物アセットのScript Execution Orderを20000など
    /// 大きな値にしてください。
    /// DefaultExecutionOrder(11000) はVRIK系より処理順を遅くする、という意図です
    /// </summary>
    [DefaultExecutionOrder(11000)]
    public class MotionDataPlayer : MonoBehaviour
    {
        [SerializeField]
        private KeyCode _playStartKey = KeyCode.S;
        [SerializeField]
        private KeyCode _playStopKey = KeyCode.T;

        [SerializeField]
        protected HumanoidPoses RecordedMotionData;
        [SerializeField]
        private Animator _animator;

        [SerializeField, Tooltip("再生開始フレームを指定します。0だとファイル先頭から開始です")]
        private int _startFrame;
        [SerializeField]
        private bool _playing;
        [SerializeField]
        private int _frameIndex;

        [SerializeField, Tooltip("普段はOBJECTROOTで問題ないです。特殊な機材の場合は変更してください")]
        private MotionDataSettings.Rootbonesystem _rootBoneSystem = MotionDataSettings.Rootbonesystem.Objectroot;
        [SerializeField, Tooltip("rootBoneSystemがOBJECTROOTの時は使われないパラメータです。")]
        private HumanBodyBones _targetRootBone = HumanBodyBones.Hips;

        private HumanPoseHandler _poseHandler;
        private Action _onPlayFinish;
        private float _playingTime;

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

        // Update is called once per frame
        private void Update()
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
        private void PlayMotion()
        {
            if (_playing)
            {
                return;
            }

            if (RecordedMotionData == null)
            {
                Debug.LogError("録画済みモーションデータが指定されていません。再生を行いません。");
                return;
            }


            _playingTime = _startFrame * (Time.deltaTime / 1f);
            _frameIndex = _startFrame;
            _playing = true;
        }

        /// <summary>
        /// モーションデータ再生終了。フレーム数が最後になっても自動で呼ばれる
        /// </summary>
        private void StopMotion()
        {
            if (!_playing)
            {
                return;
            }


            _playingTime = 0f;
            _frameIndex = _startFrame;
            _playing = false;
        }

        private void SetHumanPose()
        {
            var pose = new HumanPose();
            pose.muscles = RecordedMotionData.Poses[_frameIndex].Muscles;
            _poseHandler.SetHumanPose(ref pose);
            pose.bodyPosition = RecordedMotionData.Poses[_frameIndex].BodyPosition;
            pose.bodyRotation = RecordedMotionData.Poses[_frameIndex].BodyRotation;

            switch (_rootBoneSystem)
            {
                case MotionDataSettings.Rootbonesystem.Objectroot:
                    //_animator.transform.localPosition = RecordedMotionData.Poses[_frameIndex].BodyRootPosition;
                    //_animator.transform.localRotation = RecordedMotionData.Poses[_frameIndex].BodyRootRotation;
                    break;

                case MotionDataSettings.Rootbonesystem.Hipbone:
                    pose.bodyPosition = RecordedMotionData.Poses[_frameIndex].BodyPosition;
                    pose.bodyRotation = RecordedMotionData.Poses[_frameIndex].BodyRotation;

                    _animator.GetBoneTransform(_targetRootBone).position = RecordedMotionData.Poses[_frameIndex].BodyRootPosition;
                    _animator.GetBoneTransform(_targetRootBone).rotation = RecordedMotionData.Poses[_frameIndex].BodyRootRotation;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            //処理落ちしたモーションデータの再生速度調整
            if (_playingTime > RecordedMotionData.Poses[_frameIndex].Time)
            {
                _frameIndex++;
            }

            if (_frameIndex == RecordedMotionData.Poses.Count - 1)
            {
                if (_onPlayFinish != null)
                {
                    _onPlayFinish();
                }
            }
        }
    }
}