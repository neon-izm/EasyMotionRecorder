/**
[EasyMotionRecorder]

Copyright (c) 2018 Duo.inc

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System.IO;

namespace Entum
{
    /// <summary>
    /// CSVに吐かれたモーションデータを再生する
    /// </summary>
    public class MotionDataPlayerCSV : MotionDataPlayer
    {
        [SerializeField, Tooltip("スラッシュで終わる形で")]
        private string _recordedDirectory;

        [SerializeField, Tooltip("拡張子も")]
        private string _recordedFileName;

        // Use this for initialization
        private void Start()
        {
            if (string.IsNullOrEmpty(_recordedDirectory))
            {
                _recordedDirectory = Application.streamingAssetsPath + "/";
            }

            string motionCSVPath = _recordedDirectory + _recordedFileName;
            LoadCSVData(motionCSVPath);
        }

        //CSVから_recordedMotionDataを作る
        private void LoadCSVData(string motionDataPath)
        {
            //ファイルが存在しなければ終了
            if (!File.Exists(motionDataPath))
            {
                return;
            }


            RecordedMotionData = ScriptableObject.CreateInstance<HumanoidPoses>();

            FileStream fs = null;
            StreamReader sr = null;

            //ファイル読み込み
            try
            {
                fs = new FileStream(motionDataPath, FileMode.Open);
                sr = new StreamReader(fs);

                while (sr.Peek() > -1)
                {
                    string line = sr.ReadLine();
                    var seriHumanPose = new HumanoidPoses.SerializeHumanoidPose();
                    if (line != "")
                    {
                        seriHumanPose.DeserializeCSV(line);
                        RecordedMotionData.Poses.Add(seriHumanPose);
                    }
                }
                sr.Close();
                fs.Close();
                sr = null;
                fs = null;
            }
            catch (System.Exception e)
            {
                Debug.LogError("ファイル読み込み失敗！" + e.Message + e.StackTrace);
            }

            if (sr != null)
            {
                sr.Close();
            }

            if (fs != null)
            {
                fs.Close();
            }
        }
    }
}