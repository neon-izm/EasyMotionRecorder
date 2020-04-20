/**
[EasyMotionRecorder]

Copyright (c) 2018 Duo.inc

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System;
using System.IO;
using System.Reflection;
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
        [SerializeField]
        private HumanBodyBones IK_LeftFootBone = HumanBodyBones.LeftFoot;
        [SerializeField]
        private HumanBodyBones IK_RightFootBone = HumanBodyBones.RightFoot;

        protected HumanoidPoses Poses;
        protected float RecordedTime;
        protected float StartTime;

        private HumanPose _currentPose;
        private HumanPoseHandler _poseHandler;
        public Action OnRecordStart;
        public Action OnRecordEnd;

        [Tooltip("記録するFPS。0で制限しない。UpdateのFPSは超えられません。")]
        public float TargetFPS = 60.0f;


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


            RecordedTime = Time.time - StartTime;

            if (TargetFPS != 0.0f)
            {
                var nextTime = (1.0f * (FrameIndex + 1)) / TargetFPS;
                if (nextTime > RecordedTime)
                {
                    return;
                }
                if (FrameIndex % TargetFPS == 0)
                {
                    print("Motion_FPS=" + 1 / (RecordedTime / FrameIndex));
                }
            }
            else
            {
                if (Time.frameCount % Application.targetFrameRate == 0)
                {
                    print("Motion_FPS=" + 1 / Time.deltaTime);
                }
            }


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
            var bodyTQ = new TQ(_currentPose.bodyPosition, _currentPose.bodyRotation);
            var LeftFootTQ = new TQ(_animator.GetBoneTransform(IK_LeftFootBone).position, _animator.GetBoneTransform(IK_LeftFootBone).rotation);
            var RightFootTQ = new TQ(_animator.GetBoneTransform(IK_RightFootBone).position, _animator.GetBoneTransform(IK_RightFootBone).rotation);
            LeftFootTQ = AvatarUtility.GetIKGoalTQ(_animator.avatar, _animator.humanScale, AvatarIKGoal.LeftFoot, bodyTQ, LeftFootTQ);
            RightFootTQ = AvatarUtility.GetIKGoalTQ(_animator.avatar, _animator.humanScale, AvatarIKGoal.RightFoot, bodyTQ, RightFootTQ);

            serializedPose.BodyPosition = bodyTQ.t;
            serializedPose.BodyRotation = bodyTQ.q;
            serializedPose.LeftfootIK_Pos = LeftFootTQ.t;
            serializedPose.LeftfootIK_Rot = LeftFootTQ.q;
            serializedPose.RightfootIK_Pos = RightFootTQ.t;
            serializedPose.RightfootIK_Rot = RightFootTQ.q;



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

            if (OnRecordStart != null)
            {
                OnRecordStart();
            }

            OnRecordEnd += WriteAnimationFile;
            _recording = true;
            RecordedTime = 0f;
            StartTime = Time.time;
            FrameIndex = 0;
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


            if (OnRecordEnd != null)
            {
                OnRecordEnd();
            }

            OnRecordEnd -= WriteAnimationFile;
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
            StartTime = Time.time;
            RecordedTime = 0f;
            FrameIndex = 0;
#endif
        }

        /// <summary>
        /// 指定したパスにディレクトリが存在しない場合
        /// すべてのディレクトリとサブディレクトリを作成します
        /// </summary>
        public static DirectoryInfo SafeCreateDirectory(string path)
        {
            return Directory.Exists(path) ? null : Directory.CreateDirectory(path);
        }
        public Animator CharacterAnimator
        {
            get { return _animator; }
        }

        public class TQ
        {
            public TQ(Vector3 translation, Quaternion rotation)
            {
                t = translation;
                q = rotation;
            }
            public Vector3 t;
            public Quaternion q;
            // Scale should always be 1,1,1
        }
        public class AvatarUtility
        {
            static public TQ GetIKGoalTQ(Avatar avatar, float humanScale, AvatarIKGoal avatarIKGoal, TQ animatorBodyPositionRotation, TQ skeletonTQ)
            {
                int humanId = (int)HumanIDFromAvatarIKGoal(avatarIKGoal);
                if (humanId == (int)HumanBodyBones.LastBone)
                    throw new InvalidOperationException("Invalid human id.");
                MethodInfo methodGetAxisLength = typeof(Avatar).GetMethod("GetAxisLength", BindingFlags.Instance | BindingFlags.NonPublic);
                if (methodGetAxisLength == null)
                    throw new InvalidOperationException("Cannot find GetAxisLength method.");
                MethodInfo methodGetPostRotation = typeof(Avatar).GetMethod("GetPostRotation", BindingFlags.Instance | BindingFlags.NonPublic);
                if (methodGetPostRotation == null)
                    throw new InvalidOperationException("Cannot find GetPostRotation method.");
                Quaternion postRotation = (Quaternion)methodGetPostRotation.Invoke(avatar, new object[] { humanId });
                var goalTQ = new TQ(skeletonTQ.t, skeletonTQ.q * postRotation);
                if (avatarIKGoal == AvatarIKGoal.LeftFoot || avatarIKGoal == AvatarIKGoal.RightFoot)
                {
                    // Here you could use animator.leftFeetBottomHeight or animator.rightFeetBottomHeight rather than GetAxisLenght
                    // Both are equivalent but GetAxisLength is the generic way and work for all human bone
                    float axislength = (float)methodGetAxisLength.Invoke(avatar, new object[] { humanId });
                    Vector3 footBottom = new Vector3(axislength, 0, 0);
                    goalTQ.t += (goalTQ.q * footBottom);
                }
                // IK goal are in avatar body local space
                Quaternion invRootQ = Quaternion.Inverse(animatorBodyPositionRotation.q);
                goalTQ.t = invRootQ * (goalTQ.t - animatorBodyPositionRotation.t);
                goalTQ.q = invRootQ * goalTQ.q;
                goalTQ.t /= humanScale;

                return goalTQ;
            }
            static public HumanBodyBones HumanIDFromAvatarIKGoal(AvatarIKGoal avatarIKGoal)
            {
                HumanBodyBones humanId = HumanBodyBones.LastBone;
                switch (avatarIKGoal)
                {
                    case AvatarIKGoal.LeftFoot: humanId = HumanBodyBones.LeftFoot; break;
                    case AvatarIKGoal.RightFoot: humanId = HumanBodyBones.RightFoot; break;
                    case AvatarIKGoal.LeftHand: humanId = HumanBodyBones.LeftHand; break;
                    case AvatarIKGoal.RightHand: humanId = HumanBodyBones.RightHand; break;
                }
                return humanId;
            }
        }
    }
}
