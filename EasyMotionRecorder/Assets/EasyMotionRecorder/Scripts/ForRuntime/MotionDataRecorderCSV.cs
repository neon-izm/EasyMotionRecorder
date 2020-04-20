/**
[EasyMotionRecorder]

Copyright (c) 2018 Duo.inc

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using System;
using System.IO;

namespace Entum
{
    /// <summary>
    /// モーションデータをCSVに記録するクラス
    /// ランタイムでも記録できる
    /// </summary>
    [DefaultExecutionOrder(31000)]
    public class MotionDataRecorderCSV : MotionDataRecorder
    {
        [SerializeField, Tooltip("スラッシュで終わる形で")]
        private string _outputDirectory;

        [SerializeField, Tooltip("拡張子も")]
        private string _outputFileName;

        protected override void WriteAnimationFile()
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

            foreach (var pose in Poses.Poses)
            {
                string seriStr = pose.SerializeCSV();
                sw.WriteLine(seriStr);
            }

            //ファイルクローズ
            try
            {
                sw.Close();
                fs.Close();
                sw = null;
                fs = null;
            }
            catch (Exception e)
            {
                Debug.LogError("ファイル書き出し失敗！" + e.Message + e.StackTrace);
            }

            if (sw != null)
            {
                sw.Close();
            }

            if (fs != null)
            {
                fs.Close();
            }

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            RecordedTime = 0f;
            StartTime = Time.time;
            FrameIndex = 0;
        }
    }
}