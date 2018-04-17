/**
[EasyMotionRecorder]

Copyright (c) 2018 Duo.inc

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System;
using UnityEditor;
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
        [SerializeField] KeyCode recordStartKey = KeyCode.R;

        [SerializeField] KeyCode recordStopKey = KeyCode.X;

        /// <summary>
        /// 対象のアニメーター
        /// </summary>
        [SerializeField] Animator animator;

        [SerializeField] bool Recording = false;
        [SerializeField] int frameIndex = 0;

        [SerializeField] [Tooltip("普段はOBJECTROOTで問題ないです。特殊な機材の場合は変更してください")]
        MotionDataSettings.ROOTBONESYSTEM rootBoneSystem = MotionDataSettings.ROOTBONESYSTEM.OBJECTROOT;

        [SerializeField] [Tooltip("rootBoneSystemがOBJECTROOTの時は使われないパラメータです。")]
        HumanBodyBones targetRootBone = HumanBodyBones.Hips;


        float recordedTime = 0;

        HumanPose currentPose;

        HumanPoseHandler poseHandler = null;
        HumanoidPoses poses = null;
        Action OnRecordEnd;

     

        void SetHumanBoneTransformToHumanoidPoses(Animator animator, ref HumanoidPoses.SerializeHumanoidPose pose)
        {
            HumanBodyBones[] values = HumanBodyBones.GetValues(typeof(HumanBodyBones)) as HumanBodyBones[];
            foreach (HumanBodyBones b in values)
            {
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
            if (animator == null)
            {
                Debug.LogError("MotionDataRecorderにanimatorがセットされていません。MotionDataRecorderを削除します。");
                Destroy(this);
            }

            poseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
        }

        // <summary>
        /// 録画開始
        /// </summary>
        public void RecordStart()
        {
            if (Recording == false)
            {
                frameIndex = 0;
                recordedTime = 0;
                poses = ScriptableObject.CreateInstance<HumanoidPoses>();

                OnRecordEnd += WriteAnimationFile;
                Recording = true;
            }
        }

        void WriteAnimationFile()
        {
            SafeCreateDirectory("Assets/Resources");

            string path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Resources/RecordMotion_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".asset");
            AssetDatabase.CreateAsset(poses, path);
            AssetDatabase.Refresh();
            frameIndex = 0;
            recordedTime = 0;
        }

        /// <summary>
        /// 録画終了
        /// </summary>
        public void RecordEnd()
        {
            if (Recording)
            {
                if (OnRecordEnd != null)
                {
                    OnRecordEnd();
                    OnRecordEnd = null;
                }

                Recording = false;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(recordStartKey))
            {
                RecordStart();
            }

            if (Input.GetKeyDown(recordStopKey))
            {
                RecordEnd();
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (Recording)
            {
                recordedTime += Time.deltaTime;
                //現在のフレームのHumanoidの姿勢を取得
                poseHandler.GetHumanPose(ref currentPose);
                //posesに取得した姿勢を書き込む
                var serializedPose = new HumanoidPoses.SerializeHumanoidPose();

                if (rootBoneSystem == MotionDataSettings.ROOTBONESYSTEM.OBJECTROOT)
                {
                    serializedPose.bodyRootPosition = animator.transform.localPosition;
                    serializedPose.bodyRootRotation = animator.transform.localRotation;
                }
                else if (rootBoneSystem == MotionDataSettings.ROOTBONESYSTEM.HIPBONE)
                {
                    serializedPose.bodyRootPosition = animator.GetBoneTransform(targetRootBone).position;
                    serializedPose.bodyRootRotation = animator.GetBoneTransform(targetRootBone).rotation;
                    Debug.LogWarning(animator.GetBoneTransform(targetRootBone).position);
                }
                else
                {
                    Debug.LogWarning("enum not set");
                }

                serializedPose.bodyPosition = currentPose.bodyPosition;
                serializedPose.bodyRotation = currentPose.bodyRotation;
                serializedPose.frameCount = frameIndex;
                serializedPose.muscles = new float[currentPose.muscles.Length];
                serializedPose.frameCount = frameIndex;
                serializedPose.time = recordedTime;
                for (int i = 0; i < serializedPose.muscles.Length; i++)
                {
                    serializedPose.muscles[i] = currentPose.muscles[i];
                }

                
                SetHumanBoneTransformToHumanoidPoses(animator, ref serializedPose);
                

                poses.poses.Add(serializedPose);
                frameIndex++;
            }
        }
    }
}