using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

//CSVに吐かれたモーションデータを再生する
namespace Entum
{
    public class MotionDataPlayerCSV : MotionDataPlayer
    {
        public string RecordedDirectory;//スラッシュで終わる形で
        public string RecordedFileName;//拡張子も

        // Use this for initialization
        void Start()
        {
            if (string.IsNullOrEmpty(RecordedDirectory))
            {
                RecordedDirectory = Application.streamingAssetsPath + "/";
            }
            string motionCSVPath = RecordedDirectory + RecordedFileName;
            LoadCSVData(motionCSVPath);
        }

        //CSVから_recordedMotionDataを作る
        public void LoadCSVData(string motionDataPath)
        {
            //ファイルが存在しなければ終了
            if (!File.Exists(motionDataPath))
            {
                return;
            }

            _recordedMotionData = new HumanoidPoses();

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
                    HumanoidPoses.SerializeHumanoidPose seriHumanPose = new HumanoidPoses.SerializeHumanoidPose();
                    if (line != "")
                    {
                        seriHumanPose.DeserializeCSV(line);
                        _recordedMotionData.Poses.Add(seriHumanPose);
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
            { sr.Close(); }
            if (fs != null)
            { fs.Close(); }
        }
    }

    
}