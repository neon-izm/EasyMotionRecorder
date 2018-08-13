/**
[EasyMotionRecorder]

Copyright (c) 2018 Duo.inc

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace Entum
{
    /// <summary>
    /// モーションデータ記録クラス
    /// スクリプト実行順はVRIKの処理が終わった後の姿勢を取得したいので
    /// 最大値=32000を指定しています
    /// </summary>
    [DefaultExecutionOrder(31000)]
    public class MotionDataRecorder : MonoBehaviour
    {
        [SerializeField] KeyCode _recordStartKey = KeyCode.R;

        [SerializeField] KeyCode _recordStopKey = KeyCode.X;

        /// <summary>
        /// 対象のアニメーター
        /// </summary>
        [SerializeField] Animator _animator;

        [SerializeField] bool _recording = false;
        [SerializeField] protected int _frameIndex = 0;

        [SerializeField] [Tooltip("普段はOBJECTROOTで問題ないです。特殊な機材の場合は変更してください")]
        MotionDataSettings.Rootbonesystem _rootBoneSystem = MotionDataSettings.Rootbonesystem.Objectroot;

        [SerializeField] [Tooltip("rootBoneSystemがOBJECTROOTの時は使われないパラメータです。")]
        HumanBodyBones _targetRootBone = HumanBodyBones.Hips;


        protected float _recordedTime = 0;

        HumanPose _currentPose;

        HumanPoseHandler _poseHandler = null;
        protected HumanoidPoses _poses = null;
        Action _onRecordEnd;

     

        void SetHumanBoneTransformToHumanoidPoses(Animator animator, ref HumanoidPoses.SerializeHumanoidPose pose)
        {
            HumanBodyBones[] values = HumanBodyBones.GetValues(typeof(HumanBodyBones)) as HumanBodyBones[];
            foreach (HumanBodyBones b in values)
            {
                if (b < 0 || b >= HumanBodyBones.LastBone) { continue; }

                Transform t = animator.GetBoneTransform(b);
                if (t != null )
                {
                    var bone = new HumanoidPoses.SerializeHumanoidPose.HumanoidBone();
                    bone.Set(animator.transform, t);
                    pose.humanoidBones.Add(bone);
                }
            }

        }

        /// <summary>
        /// 指定したパスにディレクトリが存在しない場合
        /// すべてのディレクトリとサブディレクトリを作成します
        /// </summary>
        public static DirectoryInfo SafeCreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return null;
            }

            return Directory.CreateDirectory(path);
        }

        // Use this for initialization
        void Awake()
        {
            if (_animator == null)
            {
                Debug.LogError("MotionDataRecorderにanimatorがセットされていません。MotionDataRecorderを削除します。");
                Destroy(this);
            }

            _poseHandler = new HumanPoseHandler(_animator.avatar, _animator.transform);
        }

        /// <summary>
        /// 録画開始
        /// </summary>
        public void RecordStart()
        {
            if (_recording == false)
            {
                _frameIndex = 0;
                _recordedTime = 0;
                _poses = ScriptableObject.CreateInstance<HumanoidPoses>();

                _onRecordEnd += WriteAnimationFile;
                _recording = true;
            }
        }

        protected virtual void WriteAnimationFile()
        {
#if UNITY_EDITOR
            SafeCreateDirectory("Assets/Resources");

            string path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Resources/RecordMotion_" + _animator.name + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") +
                ".asset");
            AssetDatabase.CreateAsset(_poses, path);
            AssetDatabase.Refresh();
            _frameIndex = 0;
            _recordedTime = 0;
#endif
        }

        /// <summary>
        /// 録画終了
        /// </summary>
        public void RecordEnd()
        {
            if (_recording)
            {
                if (_onRecordEnd != null)
                {
                    _onRecordEnd();
                    _onRecordEnd = null;
                }

                _recording = false;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(_recordStartKey))
            {
                RecordStart();
            }

            if (Input.GetKeyDown(_recordStopKey))
            {
                RecordEnd();
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (_recording)
            {
                _recordedTime += Time.deltaTime;
                //現在のフレームのHumanoidの姿勢を取得
                _poseHandler.GetHumanPose(ref _currentPose);
                //posesに取得した姿勢を書き込む
                var serializedPose = new HumanoidPoses.SerializeHumanoidPose();

                if (_rootBoneSystem == MotionDataSettings.Rootbonesystem.Objectroot)
                {
                    serializedPose.BodyRootPosition = _animator.transform.localPosition;
                    serializedPose.BodyRootRotation = _animator.transform.localRotation;
                }
                else if (_rootBoneSystem == MotionDataSettings.Rootbonesystem.Hipbone)
                {
                    serializedPose.BodyRootPosition = _animator.GetBoneTransform(_targetRootBone).position;
                    serializedPose.BodyRootRotation = _animator.GetBoneTransform(_targetRootBone).rotation;
                    Debug.LogWarning(_animator.GetBoneTransform(_targetRootBone).position);
                }
                else
                {
                    Debug.LogWarning("enum not set");
                }

                serializedPose.BodyPosition = _currentPose.bodyPosition;
                serializedPose.BodyRotation = _currentPose.bodyRotation;
                serializedPose.FrameCount = _frameIndex;
                serializedPose.Muscles = new float[_currentPose.muscles.Length];
                serializedPose.Time = _recordedTime;
                for (int i = 0; i < serializedPose.Muscles.Length; i++)
                {
                    serializedPose.Muscles[i] = _currentPose.muscles[i];
                }

                
                SetHumanBoneTransformToHumanoidPoses(_animator, ref serializedPose);
                

                _poses.Poses.Add(serializedPose);
                _frameIndex++;
            }
        }
    }
}