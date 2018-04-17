/**
[EasyMotionRecorder]

Copyright (c) 2018 Duo.inc

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Entum
{
    [System.Serializable]
    public class MotionDataSettings
    {
        public enum ROOTBONESYSTEM
        {
            HIPBONE,
            OBJECTROOT
        }

        /// <summary>
        /// Humanoid用のMuscleマッピング
        /// </summary>
        public static Dictionary<string, string> TraitPropMap = new Dictionary<string, string>
        {
            {"Left Thumb 1 Stretched", "LeftHand.Thumb.1 Stretched"},
            {"Left Thumb Spread", "LeftHand.Thumb Spread"},
            {"Left Thumb 2 Stretched", "LeftHand.Thumb.2 Stretched"},
            {"Left Thumb 3 Stretched", "LeftHand.Thumb.3 Stretched"},
            {"Left Index 1 Stretched", "LeftHand.Index.1 Stretched"},
            {"Left Index Spread", "LeftHand.Index Spread"},
            {"Left Index 2 Stretched", "LeftHand.Index.2 Stretched"},
            {"Left Index 3 Stretched", "LeftHand.Index.3 Stretched"},
            {"Left Middle 1 Stretched", "LeftHand.Middle.1 Stretched"},
            {"Left Middle Spread", "LeftHand.Middle Spread"},
            {"Left Middle 2 Stretched", "LeftHand.Middle.2 Stretched"},
            {"Left Middle 3 Stretched", "LeftHand.Middle.3 Stretched"},
            {"Left Ring 1 Stretched", "LeftHand.Ring.1 Stretched"},
            {"Left Ring Spread", "LeftHand.Ring Spread"},
            {"Left Ring 2 Stretched", "LeftHand.Ring.2 Stretched"},
            {"Left Ring 3 Stretched", "LeftHand.Ring.3 Stretched"},
            {"Left Little 1 Stretched", "LeftHand.Little.1 Stretched"},
            {"Left Little Spread", "LeftHand.Little Spread"},
            {"Left Little 2 Stretched", "LeftHand.Little.2 Stretched"},
            {"Left Little 3 Stretched", "LeftHand.Little.3 Stretched"},
            {"Right Thumb 1 Stretched", "RightHand.Thumb.1 Stretched"},
            {"Right Thumb Spread", "RightHand.Thumb Spread"},
            {"Right Thumb 2 Stretched", "RightHand.Thumb.2 Stretched"},
            {"Right Thumb 3 Stretched", "RightHand.Thumb.3 Stretched"},
            {"Right Index 1 Stretched", "RightHand.Index.1 Stretched"},
            {"Right Index Spread", "RightHand.Index Spread"},
            {"Right Index 2 Stretched", "RightHand.Index.2 Stretched"},
            {"Right Index 3 Stretched", "RightHand.Index.3 Stretched"},
            {"Right Middle 1 Stretched", "RightHand.Middle.1 Stretched"},
            {"Right Middle Spread", "RightHand.Middle Spread"},
            {"Right Middle 2 Stretched", "RightHand.Middle.2 Stretched"},
            {"Right Middle 3 Stretched", "RightHand.Middle.3 Stretched"},
            {"Right Ring 1 Stretched", "RightHand.Ring.1 Stretched"},
            {"Right Ring Spread", "RightHand.Ring Spread"},
            {"Right Ring 2 Stretched", "RightHand.Ring.2 Stretched"},
            {"Right Ring 3 Stretched", "RightHand.Ring.3 Stretched"},
            {"Right Little 1 Stretched", "RightHand.Little.1 Stretched"},
            {"Right Little Spread", "RightHand.Little Spread"},
            {"Right Little 2 Stretched", "RightHand.Little.2 Stretched"},
            {"Right Little 3 Stretched", "RightHand.Little.3 Stretched"},
        };
    }

    /// <summary>
    /// モーションデータの中身
    /// </summary>
    public class HumanoidPoses : ScriptableObject
    {
        //Genericなanimファイルとして出力する。
        [ContextMenu("Export as generic animation clips")]
        public void ExportgenericAnim()
        {
            var clip = new AnimationClip {frameRate = 30,};
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings() {loopTime = false,});

            var bones = poses[0].humanoidBones;
            for (int i = 0; i < bones.Count; i++)
            {
                var positionCurveX = new AnimationCurve();
                var positionCurveY = new AnimationCurve();
                var positionCurveZ = new AnimationCurve();
                var rotationCurveX = new AnimationCurve();
                var rotationCurveY = new AnimationCurve();
                var rotationCurveZ = new AnimationCurve();
                var rotationCurveW = new AnimationCurve();

                foreach (var p in poses)
                {
                    positionCurveX.AddKey(p.time, p.humanoidBones[i].localPosition.x);
                    positionCurveY.AddKey(p.time, p.humanoidBones[i].localPosition.y);
                    positionCurveZ.AddKey(p.time, p.humanoidBones[i].localPosition.z);
                    rotationCurveX.AddKey(p.time, p.humanoidBones[i].localRotation.x);
                    rotationCurveY.AddKey(p.time, p.humanoidBones[i].localRotation.y);
                    rotationCurveZ.AddKey(p.time, p.humanoidBones[i].localRotation.z);
                    rotationCurveW.AddKey(p.time, p.humanoidBones[i].localRotation.w);
                }

                //pathは階層
                //http://mebiustos.hatenablog.com/entry/2015/09/16/230000
                AnimationUtility.SetEditorCurve(clip,
                    new EditorCurveBinding()
                    {
                        path = poses[0].humanoidBones[i].name,
                        type = typeof(Transform),
                        propertyName = "m_LocalPosition.x"
                    }, positionCurveX);
                AnimationUtility.SetEditorCurve(clip,
                    new EditorCurveBinding()
                    {
                        path = poses[0].humanoidBones[i].name,
                        type = typeof(Transform),
                        propertyName = "m_LocalPosition.y"
                    }, positionCurveY);
                AnimationUtility.SetEditorCurve(clip,
                    new EditorCurveBinding()
                    {
                        path = poses[0].humanoidBones[i].name,
                        type = typeof(Transform),
                        propertyName = "m_LocalPosition.z"
                    }, positionCurveZ);

                AnimationUtility.SetEditorCurve(clip,
                    new EditorCurveBinding()
                    {
                        path = poses[0].humanoidBones[i].name,
                        type = typeof(Transform),
                        propertyName = "m_LocalRotation.x"
                    }, rotationCurveX);
                AnimationUtility.SetEditorCurve(clip,
                    new EditorCurveBinding()
                    {
                        path = poses[0].humanoidBones[i].name,
                        type = typeof(Transform),
                        propertyName = "m_LocalRotation.y"
                    }, rotationCurveY);
                AnimationUtility.SetEditorCurve(clip,
                    new EditorCurveBinding()
                    {
                        path = poses[0].humanoidBones[i].name,
                        type = typeof(Transform),
                        propertyName = "m_LocalRotation.z"
                    }, rotationCurveZ);
                AnimationUtility.SetEditorCurve(clip,
                    new EditorCurveBinding()
                    {
                        path = poses[0].humanoidBones[i].name,
                        type = typeof(Transform),
                        propertyName = "m_LocalRotation.w"
                    }, rotationCurveW);
            }

            clip.EnsureQuaternionContinuity();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Resources/RecordMotion_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_Generic.anim");

            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();
            return;
        }

        //Humanoidなanimファイルとして出力する。
        [ContextMenu("Export as Humanoid animation clips")]
        public void ExportHumanoidAnim()
        {
            var clip = new AnimationClip {frameRate = 30,};
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings() {loopTime = false,});


            // position
            {
                var curveX = new AnimationCurve();
                var curveY = new AnimationCurve();
                var curveZ = new AnimationCurve();
                foreach (var item in poses)
                {
                    curveX.AddKey(item.time, item.bodyPosition.x);
                    curveY.AddKey(item.time, item.bodyPosition.y);
                    curveZ.AddKey(item.time, item.bodyPosition.z);
                }

                const string muscleX = "RootT.x";
                clip.SetCurve(null, typeof(Animator), muscleX, curveX);
                const string muscleY = "RootT.y";
                clip.SetCurve(null, typeof(Animator), muscleY, curveY);
                const string muscleZ = "RootT.z";
                clip.SetCurve(null, typeof(Animator), muscleZ, curveZ);
            }

            // rotation
            {
                var curveX = new AnimationCurve();
                var curveY = new AnimationCurve();
                var curveZ = new AnimationCurve();
                var curveW = new AnimationCurve();
                foreach (var item in poses)
                {
                    curveX.AddKey(item.time, item.bodyRotation.x);
                    curveY.AddKey(item.time, item.bodyRotation.y);
                    curveZ.AddKey(item.time, item.bodyRotation.z);
                    curveW.AddKey(item.time, item.bodyRotation.w);
                }

                const string muscleX = "RootQ.x";
                clip.SetCurve(null, typeof(Animator), muscleX, curveX);
                const string muscleY = "RootQ.y";
                clip.SetCurve(null, typeof(Animator), muscleY, curveY);
                const string muscleZ = "RootQ.z";
                clip.SetCurve(null, typeof(Animator), muscleZ, curveZ);
                const string muscleW = "RootQ.w";
                clip.SetCurve(null, typeof(Animator), muscleW, curveW);
            }

            // muscles
            for (int i = 0; i < HumanTrait.MuscleCount; ++i)
            {
                var curve = new AnimationCurve();
                foreach (var item in poses)
                {
                    curve.AddKey(item.time, item.muscles[i]);
                }

                var muscle = HumanTrait.MuscleName[i];
                if (MotionDataSettings.TraitPropMap.ContainsKey(muscle))
                {
                    muscle = MotionDataSettings.TraitPropMap[muscle];
                }

                clip.SetCurve(null, typeof(Animator), muscle, curve);
            }

            clip.EnsureQuaternionContinuity();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Resources/RecordMotion_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_Humanoid.anim");

            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();

            return;
        }


        [System.SerializableAttribute]
        public class SerializeHumanoidPose
        {
            public Vector3 bodyRootPosition;
            public Quaternion bodyRootRotation;

            public Vector3 bodyPosition;
            public Quaternion bodyRotation;

            public float[] muscles;

            //フレーム数
            public int frameCount;

            //記録開始後の経過時間。処理落ち対策
            public float time;

            [System.Serializable]
            public class HumanoidBone
            {
                public string name;
                public Vector3 localPosition;
                public Quaternion localRotation;

                private static string BuildRelativePath(Transform root, Transform target)
                {
                    var path = "";
                    var current = target;
                    while (true)
                    {
                        if (current == null) throw new System.Exception(target.name + "は" + root.name + "の子ではありません");
                        if (current == root) break;

                        path = (path == "") ? current.name : current.name + "/" + path;

                        current = current.parent;
                    }

                    return path;
                }

                public void Set(Transform root, Transform t)
                {
                    name = BuildRelativePath(root, t);

                    localPosition = t.localPosition;
                    localRotation = t.localRotation;
                }
            }

            public List<HumanoidBone> humanoidBones = new List<HumanoidBone>();
        }

        public List<SerializeHumanoidPose> poses = new List<SerializeHumanoidPose>();
    }
}