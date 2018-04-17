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
        [SerializeField] KeyCode playStartKey = KeyCode.S;
        [SerializeField] KeyCode playStopKey = KeyCode.T;

        [SerializeField] HumanoidPoses recordedMotionData;

        [SerializeField] Animator animator;

        HumanPoseHandler poseHandler;

        System.Action OnPlayFinish;

        [SerializeField] [Tooltip("再生開始フレームを指定します。0だとファイル先頭から開始です")]
        int startFrame = 0;

        [SerializeField] bool Playing = false;
        [SerializeField] int frameIndex = 0;

        [SerializeField] [Tooltip("普段はOBJECTROOTで問題ないです。特殊な機材の場合は変更してください")]
        MotionDataSettings.ROOTBONESYSTEM rootBoneSystem = MotionDataSettings.ROOTBONESYSTEM.OBJECTROOT;

        [SerializeField] [Tooltip("rootBoneSystemがOBJECTROOTの時は使われないパラメータです。")]
        HumanBodyBones targetRootBone = HumanBodyBones.Hips;


        float playingTime = 0;

        /// <summary>
        /// モーションデータ再生開始
        /// </summary>
        public void PlayMotion()
        {
            if (Playing == false)
            {
                if (recordedMotionData == null)
                {
                    Debug.LogError("録画済みモーションデータが指定されていません。再生を行いません。");
                }
                else
                {
                    playingTime = startFrame * (Time.deltaTime / 1f);
                    frameIndex = startFrame;
                    Playing = true;
                }
            }
        }

        /// <summary>
        /// モーションデータ再生終了。フレーム数が最後になっても自動で呼ばれる
        /// </summary>
        public void StopMotion()
        {
            if (Playing)
            {
                playingTime = 0;
                frameIndex = startFrame;
                Playing = false;
            }
        }


        void SetHumanPose(int frame)
        {
            var pose = new HumanPose();
            pose.muscles = recordedMotionData.poses[frameIndex].muscles;
            poseHandler.SetHumanPose(ref pose);
            pose.bodyPosition = recordedMotionData.poses[frameIndex].bodyPosition;
            pose.bodyRotation = recordedMotionData.poses[frameIndex].bodyRotation;

            if (rootBoneSystem == MotionDataSettings.ROOTBONESYSTEM.OBJECTROOT)
            {
                //animator.transform.localPosition = recordedMotionData.poses[frameIndex].bodyRootPosition;
                //animator.transform.localRotation = recordedMotionData.poses[frameIndex].bodyRootRotation;
            }
            else if (rootBoneSystem == MotionDataSettings.ROOTBONESYSTEM.HIPBONE)
            {
                pose.bodyPosition = recordedMotionData.poses[frameIndex].bodyPosition;
                pose.bodyRotation = recordedMotionData.poses[frameIndex].bodyRotation;

                animator.GetBoneTransform(targetRootBone).position =
                    recordedMotionData.poses[frameIndex].bodyRootPosition;
                animator.GetBoneTransform(targetRootBone).rotation =
                    recordedMotionData.poses[frameIndex].bodyRootRotation;
            }

            //処理落ちしたモーションデータの再生速度調整
            if (playingTime > recordedMotionData.poses[frameIndex].time)
            {
                frameIndex++;
            }

            if (frameIndex == recordedMotionData.poses.Count - 1)
            {
                if (OnPlayFinish != null)
                {
                    OnPlayFinish();
                }
            }
        }


        void Awake()
        {
            if (animator == null)
            {
                Debug.LogError("MotionDataPlayerにanimatorがセットされていません。MotionDataPlayerを削除します。");
                Destroy(this);
            }

            poseHandler = new HumanPoseHandler(animator.avatar, (animator.transform));
            OnPlayFinish += StopMotion;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(playStartKey))
            {
                PlayMotion();
            }

            if (Input.GetKeyDown(playStopKey))
            {
                StopMotion();
            }
        }

        private void LateUpdate()
        {
            if (!Playing) return;
            playingTime += Time.deltaTime;
            SetHumanPose(frameIndex);
        }
    }
}