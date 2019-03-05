using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;
using UnityEngine;
using UniRx.Async;
using UnityEditor;

namespace Entum
{
    [DefaultExecutionOrder(32000)]
    public class MotionDataRecorder_RunTime : MonoBehaviour
    {
        public int FlameVisualize;//forDebug
        [SerializeField]
        private Animator _animator;

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
        [Tooltip("スラッシュで終わる形で")]
        public string _outputDirectory;
        [Tooltip("拡張子も")]
        public string _outputFileName;

        protected HumanoidPoses Poses;
        protected float RecordedTime;

        private HumanPose _currentPose;
        private HumanPoseHandler _poseHandler;
        public Action OnRecordStart;
        public Action OnRecordEnd;

        const int bufferSize = 512;
        HumanoidPoses.SerializeHumanoidPose[] poseBuffer=new HumanoidPoses.SerializeHumanoidPose[bufferSize];
        int bufferWritingPosition;
        int fileWritingPosition;
        ulong bufferWriteLoop;
        ulong fileWriteLoop;

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
            FlameVisualize = FrameIndex;
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

            poseBuffer[bufferWritingPosition] = serializedPose;
            bufferWritingPosition++;
            bufferWriteLoop++;
            if (bufferWritingPosition>=bufferSize)
            {
                bufferWritingPosition = 0;
            }
            FrameIndex++;
        }

        /// <summary>
        /// 録画開始
        /// </summary>
        public void RecordStart()
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

            _recording = true;
            RecordedTime = 0f;
            FrameIndex = 0;
            fileWritingPosition = 0;
            bufferWritingPosition = 0;
            fileWriteLoop = 0;
            bufferWriteLoop = 0;
            UniTask.Run(WriteAnimationFileAsync);
        }

        /// <summary>
        /// 録画終了
        /// </summary>
        public void RecordEnd()
        {
            if (!_recording)
            {
                return;
            }


            if (OnRecordEnd != null)
            {
                OnRecordEnd();
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

        async UniTask WriteAnimationFileAsync()
        {
            //ファイルオープン
            string directoryStr = _outputDirectory;
            if (directoryStr == "")
            {
                //自動設定ディレクトリ
                directoryStr = Application.streamingAssetsPath + "/";

                if (!Directory.Exists(directoryStr))
                {
                    Directory.CreateDirectory(directoryStr);
                }
            }

            string fileNameStr = _outputFileName;
            if (fileNameStr == "")
            {
                //自動設定ファイル名
                fileNameStr = string.Format("motion_{0:yyyy_MM_dd_HH_mm_ss}.csv", DateTime.Now);
            }

            FileStream fs = new FileStream(directoryStr + fileNameStr, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            //ファイルへの書き込み
            do
            {
                await UniTask.WaitUntil(()=>fileWriteLoop<bufferWriteLoop);
                string seriStr = poseBuffer[fileWritingPosition].SerializeCSV();
                sw.WriteLine(seriStr);
                sw.Flush();
                fs.Flush();
                Debug.Log("書き込んだ");
                fileWritingPosition++;
                fileWriteLoop++;
                if(fileWritingPosition>=bufferSize)
                {
                    fileWritingPosition = 0;
                }
            }
            while (_recording);

            try
            {
                sw.Close();
                fs.Close();
                sw = null;
                fs = null;
            }
            catch(Exception e)
            {
                Debug.LogError("ファイル書き出し失敗！" + e.Message + e.StackTrace);
            }
            finally
            {
                sw.Close();
                fs.Close();
            }
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            RecordedTime = 0f;
            FrameIndex = 0;
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
