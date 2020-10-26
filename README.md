# TTSController
各種 Text-to-Speech エンジンを統一的に操作するライブラリです。VOICEROIDなどを自動化する簡易Webサーバもあります。

## 対応プラットフォーム
- Windows 10 (64bit)

## 対応音声合成ライブラリ
- VOICEROID+ 各種
- VOICEROID2 各種
- 音街ウナTalkEx
- SAPI5 (Windows10標準の音声合成機能。スタートメニュー>設定>時刻と言語>音声認識>音声の管理>音声の追加から各国語の音声が追加できます。API仕様により追加しても列挙されない音声があります。)

## ブラウザで音声合成する
この実装は簡易実装であり、音声合成ライブラリと同一のPC上で実行することを想定しています。インターネット上への公開は、セキュリティ上のリスクや音声合成ライブラリのライセンス上の問題がある可能性があります。

- [ビルド済みバイナリ(v0,0.1beta)](https://github.com/ksasao/TTSController/releases/download/v0.0.1beta/SpeechWebServer_v0.0.1beta.zip)

### 準備
- SpeechWebServer のプロジェクトを Visual Studio 2019 でビルドして ```SpeechWebServer.exe``` を実行します(管理者権限が必要です)

### 利用方法
- ブラウザで http://localhost:1000/ を開くと現在の時刻を発話します
- ブラウザで http://localhost:1000/?text=こんにちは を開くと「こんにちは」と発話します。「こんにちは」の部分は任意の文字列を指定できます
- VOICEROID+ 東北きりたんがインストールされている場合、http://localhost:1000/?name=東北きりたん&text=こんばんは を開くと東北きりたんの声で発話します。他の VOICEROID を利用する場合は、アプリ起動時に表示される「インストール済み音声合成ライブラリ」の表記を参考に、適宜 name の引数を変更してください。

## TODO

### 対応(予定) の TTS

- [x] VOICEROID+ EX
- [x] VOICEROID2
- [ ] CeVIO
- [x] SAPI5 (おまけ程度)

### 制御機能
- [x] 話者の一覧取得
- [x] 話者に応じたTTS切り替え

### 音声コントロール
- [x] 再生
- [x] 音量の取得・変更
- [x] 話速の取得・変更
- [x] ピッチの取得・変更
- [x] 抑揚の取得・変更
- [x] 発話中の音声停止
- [ ] 連続して文字列が入力されたときの対応
- [ ] 音声合成対象の文字列の途中に .wav ファイルを差し込み
- [ ] 音声合成対象の文字列の途中に音声コントロールを埋め込み
- [ ] 音声出力デバイス選択
