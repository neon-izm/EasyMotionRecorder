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
        public string OutputDirectory;//スラッシュで終わる形で
        public string OutputFileName;//拡張子も

        protected override void WriteAnimationFile()
        {

            //ファイルオープン
            string directoryStr = OutputDirectory;
            if (directoryStr == "")
            {
                //自動設定ディレクトリ
                directoryStr = Application.streamingAssetsPath + "/";

                if (!Directory.Exists(directoryStr))
                {
                    Directory.CreateDirectory(directoryStr);
                }
            }
            string fileNameStr = OutputFileName;
            if (fileNameStr == "")
            {
                //自動設定ファイル名
                fileNameStr = "motion_" + System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".csv";
            }
            FileStream fs = new FileStream(directoryStr + fileNameStr, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            foreach (var pose in _poses.Poses)
            {
                string seriStr = pose.SerializeCSV();
                sw.WriteLine(seriStr);
            }

            //ファイルクローズ
            try
            {
                if (sw != null)
                {
                    sw.Close();
                }

                if (fs != null)
                {
                    fs.Close();
                }
                sw = null;
                fs = null;
            }
            catch (System.Exception e)
            {
                Debug.LogError("ファイル書き出し失敗！" + e.Message + e.StackTrace);
            }
            if (sw != null)
            { sw.Close(); }
            if (fs != null)
            { fs.Close(); }

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            _frameIndex = 0;
            _recordedTime = 0;
        }
    }
}