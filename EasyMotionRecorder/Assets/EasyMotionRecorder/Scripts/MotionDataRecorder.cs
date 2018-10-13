/**
[EasyMotionRecorder]

Copyright (c) 2018 Duo.inc

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Entum
{
    /// <summary>
    /// モーションデータ記録クラス
    /// スクリプト実行順はVRIKの処理が終わった後の姿勢を取得したいので
    /// 最大値=32000を指定しています
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public class MotionDataRecorder : MonoBehaviour
    {
        [SerializeField]
        private KeyCode _recordStartKey = KeyCode.R;
        [SerializeField]
        private KeyCode _recordStopKey = KeyCode.X;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private bool _recording;
        [SerializeField]
        protected int FrameIndex;

        [SerializeField, Tooltip("普段はOBJECTROOTで問題ないです。特殊な機材の場合は変更してください")]
        private MotionDataSettings.Rootbonesystem _rootBoneSystem = MotionDataSettings.Rootbonesystem.Objectroot;
        [SerializeField, Tooltip("rootBoneSystemがOBJECTROOTの時は使われないパラメータです。")]
        private HumanBodyBones _targetRootBone = HumanBodyBones.Hips;

        protected HumanoidPoses Poses;
        protected float RecordedTime;

        private HumanPose _currentPose;
        private HumanPoseHandler _poseHandler;
        private Action _onRecordEnd;

        // Use this for initialization
        private void Awake()
        {
            if (_animator == null)
            {
                Debug.LogError("MotionDataRecorderにanimatorがセットされていません。MotionDataRecorderを削除します。");
                Destroy(this);
                return;
            }

            _poseHandler = new HumanPoseHandler(_animator.avatar, _animator.transform);
        }

        private void Update()
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
        private void LateUpdate()
        {
            if (!_recording)
            {
                return;
            }


            RecordedTime += Time.deltaTime;
            //現在のフレームのHumanoidの姿勢を取得
            _poseHandler.GetHumanPose(ref _currentPose);
            //posesに取得した姿勢を書き込む
            var serializedPose = new HumanoidPoses.SerializeHumanoidPose();

            switch (_rootBoneSystem)
            {
                case MotionDataSettings.Rootbonesystem.Objectroot:
                    serializedPose.BodyRootPosition = _animator.transform.localPosition;
                    serializedPose.BodyRootRotation = _animator.transform.localRotation;
                    break;

                case MotionDataSettings.Rootbonesystem.Hipbone:
                    serializedPose.BodyRootPosition = _animator.GetBoneTransform(_targetRootBone).position;
                    serializedPose.BodyRootRotation = _animator.GetBoneTransform(_targetRootBone).rotation;
                    Debug.LogWarning(_animator.GetBoneTransform(_targetRootBone).position);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            serializedPose.BodyPosition = _currentPose.bodyPosition;
            serializedPose.BodyRotation = _currentPose.bodyRotation;
            serializedPose.FrameCount = FrameIndex;
            serializedPose.Muscles = new float[_currentPose.muscles.Length];
            serializedPose.Time = RecordedTime;
            for (int i = 0; i < serializedPose.Muscles.Length; i++)
            {
                serializedPose.Muscles[i] = _currentPose.muscles[i];
            }

            SetHumanBoneTransformToHumanoidPoses(_animator, ref serializedPose);

            Poses.Poses.Add(serializedPose);
            FrameIndex++;
        }

        /// <summary>
        /// 録画開始
        /// </summary>
        private void RecordStart()
        {
            if (_recording)
            {
                return;
            }


            Poses = ScriptableObject.CreateInstance<HumanoidPoses>();
            RecordedTime = 0f;

            _onRecordEnd += WriteAnimationFile;
            FrameIndex = 0;
            _recording = true;
        }

        /// <summary>
        /// 録画終了
        /// </summary>
        private void RecordEnd()
        {
            if (!_recording)
            {
                return;
            }


            if (_onRecordEnd != null)
            {
                _onRecordEnd();
                _onRecordEnd = null;
            }

            _recording = false;
        }

        private static void SetHumanBoneTransformToHumanoidPoses(Animator animator, ref HumanoidPoses.SerializeHumanoidPose pose)
        {
            HumanBodyBones[] values = Enum.GetValues(typeof(HumanBodyBones)) as HumanBodyBones[];
            foreach (HumanBodyBones b in values)
            {
                if (b < 0 || b >= HumanBodyBones.LastBone)
                {
                    continue;
                }

                Transform t = animator.GetBoneTransform(b);
                if (t != null)
                {
                    var bone = new HumanoidPoses.SerializeHumanoidPose.HumanoidBone();
                    bone.Set(animator.transform, t);
                    pose.HumanoidBones.Add(bone);
                }
            }
        }

        protected virtual void WriteAnimationFile()
        {
#if UNITY_EDITOR
            SafeCreateDirectory("Assets/Resources");

            var path = string.Format("Assets/Resources/RecordMotion_{0}{1:yyyy_MM_dd_HH_mm_ss}.asset", _animator.name, DateTime.Now);
            var uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(Poses, uniqueAssetPath);
            AssetDatabase.Refresh();

            RecordedTime = 0f;
            FrameIndex = 0;
#endif
        }

        /// <summary>
        /// 指定したパスにディレクトリが存在しない場合
        /// すべてのディレクトリとサブディレクトリを作成します
        /// </summary>
        private static DirectoryInfo SafeCreateDirectory(string path)
        {
            return Directory.Exists(path) ? null : Directory.CreateDirectory(path);
        }
    }
}