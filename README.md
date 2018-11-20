# EasyMotionRecorder
Unityエディタ上でVRIKなどのモーションキャプチャをしたHumanoidキャラクターのモーションを記録, 再生をするスクリプトです。

例えばVTuberの人が1テイク目でキャラの動きだけを撮り、2テイク目以降で1テイク目のモーションを再生しながら音声と表情を付ける、という使い方を想定しています。

## 使い方
[releases](https://github.com/duo-inc/EasyMotionRecorder/releases)からEasyMotionRecorder.unitypackageをダウンロードしてプロジェクトにインポートしてください

### Setup手順
0. モーションキャプチャ対象のキャラクターをHumanoidにしておく。OculusTouchやViveコントローラ、あるいはAxisNeuronやKinectの動きがエディタ上で反映されているシーンをセットアップする。

1. シーン上に`/Assets/EasyMotionRecorder/Prefabs/EasyMotionRecorder.prefab`を配置する。

2. 1.でシーン上に配置したEasyMotionRecorderにアタッチされているMotionDataPlayerコンポーネントおよびMotionDataRecoderコンポーネントのAnimatorにモーションキャプチャ対象のキャラクターをアタッチする。

### モーション記録
1. Unityエディタ上で実行して、Rキーを押したタイミングからモーションキャプチャデータを記録、Xキーでファイル書き出しをして記録を終了します。

2. /Assets/Resources/の中にRecordMotion_2018~~~ ファイルが生成されていればモーション記録が成功しています。

### モーション再生
1. エディタ実行前にモーション録画で生成したファイル（RecordMotion_2018~~~ ）をシーン上に存在するEasyMotionRecorderゲームオブジェクトのMotionDataPlayerコンポーネントのRecordedMotionDataプロパティにアタッチします。

2. Unityエディタ上で実行して、Sキーでモーションデータの再生開始が行えます。モーションデータの最後に到達するか、Tキーでモーションデータ再生が終了します。

## おすすめの使い方(.animファイル書き出し)
モーション記録して生成されたRecordMotion_2018~~~ ファイルを選択、インスペクタ上で右クリックして「Export as Humanoid animation clips」を選択するとAnimationClipに変換されます。

変換後のAnimationClipはHumanoid準拠のモーションとしてMecanimAnimatorやUnityTimeline上で扱うことが出来ます。

もし床にキャラクターが沈んでしまう場合はHumanoidのAnimationClipにあるRoot Transform Position(Y)をBased Upon：Originalに変更してください。  
![export_gif](https://github.com/duo-inc/EasyMotionRecorder/blob/readme_images/Images/emrec_export_humanoid.gif)

## FAQ

### 使っているとUnityが重い
申し訳ありません、録画中の処理負荷は多少増えます。

### 使うショートカットキーを変えたい
インスペクタ上のEasyMotionRecorder内、MotionDataRecoderとMotionDataPlayerでキーを選べるようになっています。

### 長時間記録しているとUnityが落ちた
素朴な実装を行っているため記録中は常にメモリを食い続けます。

もし長時間の記録が必要であればメモリの増設をお勧めします、10分程度でしたら問題ありません。

### モーションを再生しているときにスカートや髪が揺れない
スクリプトの実行順を変更してください。

SpringBone, DynamicBone, BulletPhysicsImplなど、揺れ物アセットの[Script Execution Order](https://docs.unity3d.com/jp/530/Manual/class-ScriptExecution.html)を20000以上に設定してください。

### 再生開始フレームを指定したい
MotionDataPlayer内のstartFrameで再生開始フレーム指定が可能です。

### セットアップの説明が分かりづらい
申し訳ありません、[セットアップ手順の動画](https://twitter.com/entum_info/status/986823609329926146)がありますのでご参照ください。

ビルドしたバイナリ上でモーションを記録したい場合は[MotionDataPlayerCSV.csなど](https://github.com/duo-inc/EasyMotionRecorder/tree/master/EasyMotionRecorder/Assets/EasyMotionRecorder/Scripts/ForRuntime)をご参照ください。

## Known Issues
### VRIK使用時にキャラクターの立ち位置がズレることがある
VRIKの処理順による問題です、再生開始時の位置を変更することで暫定対処可能です。

## 動作環境
Unity 5.6.5p2 64bit (Windows)のエディタ上で動作確認をしています。

Unity5.6~Unity2018.2.14f1 での動作実績があります。

公式にはVRIKをサポートしますが、Kinectや各種モーションキャプチャーシステムでの動作も（無保証ですが）動作実績があります。

## ライセンス
This software is released under the [MIT License, see LICENSE.txt](https://github.com/duo-inc/EasyMotionRecorder/blob/master/LICENSE.txt).

(このソフトウェアは、[MITライセンス](https://github.com/duo-inc/EasyMotionRecorder/blob/master/LICENSE.txt)のもとで公開されています。)
