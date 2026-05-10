# BSCustomKeyEvents

## 概要
Custom Avatar, Custom Saber でコントローラーのボタンを押したりトリガーを引いたときに処理を行わせるための Plugin です。  
[PureDark 様のソース](https://github.com/PureDark/BSCustomKeyEvents)をBeatSaber 1.15.0 で動くように少しいじっただけのものです。  

## 注意
- この Plugin 単体で何かができるわけではなく、Custom Avatar/Custom Saber 側に事前に設定が必要です。
- 機器と時間の都合上、私自身は Custom Avatar/Custom Saber (Saber Factory ではなく)でしか動作確認していません。確認した機器については Releases ページを参照してください。
- PureDark 様のオリジナル版は Custom Platform でも使えたそうですが、Custom Platform を使ったことがないので動くかどうか不明です。

## 使用方法

### 前提
- 以下の手順で Custom Avatar を作成されているものとします。  
https://bsmg.wiki/models/avatars-guide.html
    - 上記手順に記載されている CustomKeyEvents がこの Plugin の元作者 PureDark 様の Plugin です。1.12.2以降(厳密には1.6.0以降)ではそれが動かないので、代わりに当ページの Plugin を使用します。
- 表情変更のアニメーションはモデルオブジェクトの Body の Animator コンポーネントに設定されているものとします。
    - 上記手順でいえば [Hierarchy] ウィンドウの [Rico] - [Rico] - [Body]。  
	  ※[Armature] と同じ階層にある [Body] です。
    - 下記の動作確認用に Animator コンポーネントには Smile という trigger が設定されており、On になると笑顔のアニメーションが行われるものとします。


### Custom Avatar の設定 (Unity で設定)
1. Release ページの CustomKeyEvents_For_Unity_v*.zip を展開して CustomKeyEvents.dll を取り出しておきます。
2. [Project] ウィンドウの Assets フォルダの下に上記 CustomKeyEvents.dll を配置します。
3. [Hierarchy] ウィンドウでモデルの親オブジェクト (Avatar Descriptor や Event Manager を設定してあるオブジェクト)を選択し [Inspector] ウィンドウで [Add Component]ボタンを押下します。
4. Custom Key Event が選択肢に出てくるので選択します。
5. Custom Key Event (Script) が [Inspector] ウィンドウに追加されます。
6. Rift S のAボタンにイベントを割り当てる場合、[Oculus Trigger Button] で [A] を選択します。
7. Aボタンを長押ししたときに Avatar の Animation を起動したい場合は [Long Click Events ()] の右下の [+] をクリックし Event Manager 同様に設定します。
    - [None (Object)] に [Hierarchy] ウィンドウからモデルオブジェクトの [Body] をドラッグ&ドロップします。
    - [No Function] のドロップダウンから [Animator] - [SetTrigger (string)] を選択します。
    - 空欄のテキストボックスが現れるので Smile と入力します。
8. [Play] ボタンを押すか、[File] - [Build And Run] から起動して動作確認してください。
    - HMD が接続されていない場合、[WMR Trigger Button] で [A] を指定しておくと、[Play] ボタンでの動作確認時、キーボードの A を押すことで上記で設定したイベントが動くか確認できます。
9. 動作確認出来たら Avatar Exporter で *.avatar ファイルを作成してください。

### Beat Saber 側の設定
1. 事前に ModAssistant を使用して BSIPA, SongCore などの Core MOD と CustomAvatar, DynamicOpenVR, BSML (Beat Saber Markup Language) などをインストールしておいてください。
2. Release ページの CustomKeyEvents_v*.zip を展開して CustomKeyEvents.dll を取り出しておきます。
    - CustomKeyEvents_For_Unity_v*.zip と間違えないように注意。
3. Beat Saber が起動していたらいったん終了します。
4. [Beat Saber インストールディレクトリ](https://bsmg.wiki/faq/install-folder.html#default-location)にある CustomAvatars ディレクトリに、上記で作成した *.avatar ファイルを配置します。  
※この手順はほかの avatar ファイルと違いはありません。
5. [Beat Saber インストールディレクトリ](https://bsmg.wiki/faq/install-folder.html#default-location)の Plugins ディレクトリに 2. の CustomKeyEvents.dll を配置します。
6. Beat Saber を起動します。
7. Custom Avatar の設定画面で 4. の Avatar に切り替え、コントローラーの Aボタンを長押ししてみて Avatar の表情が笑顔になれば成功です。

## 設定UIの説明
以下は、`Custom Key Events` の設定画面を上から順番に説明したものです。  

<img width="815" height="363" alt="image" src="https://github.com/user-attachments/assets/225426f8-e521-4115-8cbf-106f90f08ef1" />

### 1. Target
- 現在選択中ターゲットの概要を 1 行で表示します。  
  表示形式: `#ComponentOrdinal HierarchyPath@ActiveDuration(sec)`
- `#ComponentOrdinal`: 同一オブジェクトに複数の CustomKeyEvent がある場合の識別番号です。
- `HierarchyPath`: 対象オブジェクトの階層パスです。
- `ActiveDuration(sec)`: MOD起動中にそのターゲットがアクティブだった累計秒数です。未ロード時は `unloaded` と表示されます。

### 2. Target Component（ドロップダウン）
- 設定対象にする CustomKeyEvent コンポーネントを選択します。
- 候補は、現在ロード中のものに加えて、MOD起動中に検出済みのターゲット（プレイシーンのみのモデルなど、アンロード済み含む）も表示されます。

### 3. REFRESH
- ターゲット一覧を再取得し、表示を更新します。
- 特に、プレイ中にのみ出現するモデルを選択したい場合は、いったんそのモデルを表示させてから `REFRESH` を押してください。

### 4. Include Hierarchy Path In Identity（トグル）
- 設定保存時の識別キーに HierarchyPath を含めるかを切り替えます（既定: OFF）。
- ON にすると、同一モデルでも配置先の階層が違う場合に別ターゲットとして扱います。
- OFF にすると、階層変化の影響を受けにくくなります。

### 5. Default Settings
- 選択中ターゲットの初期設定サマリーを表示します。
- `Unityエディタ` 側で設定したボタン割り当ての確認用です。

<img width="863" height="207" alt="image" src="https://github.com/user-attachments/assets/0a59763c-4ada-42ff-b680-fb6e8c0e73fd" />

### 6. Live Event Monitor
- `Last`: 直近にトリガーしたイベントを表示します。
- `Recent`: 最近トリガーしたイベント履歴を新しい順で表示します。
- ここで表示される時刻は、UIを開いた時点を 0 秒とした経過時間です。

### 7. RESET TO DEFAULTS / DELETE STORED SETTINGS（ボタン）
- ライブ対象（またはセッション中に有効だった対象）では `RESET TO DEFAULTS` と表示され、現在の上書き設定を初期値に戻します。
- 完全にアンロード済みのモデルで設定ファイルのみある対象では `DELETE STORED SETTINGS` と表示され、保存済み設定だけを削除します。

<img width="907" height="352" alt="image" src="https://github.com/user-attachments/assets/fa06f59f-2f45-4a5c-ab5f-480b3cc20450" />

### 8. Trigger Buttons
- `Index Trigger Button`
- `Vive Trigger Button`
- `Oculus Trigger Button`
- `WMR Trigger Button`  ※Index, Vive, Oculus以外の新デバイスはWMRになります
- 各デバイスの「主ボタン」を選択します。ここで選んだ入力がイベント判定のベースになります。

<img width="919" height="413" alt="image" src="https://github.com/user-attachments/assets/3ceb0b3d-741a-4a96-a8b9-dff48ef65ae4" />

### 9. Chord Buttons
- `Enable Chord Press`: 同時押し（Chord）判定を有効化します。
- `Index / Vive / Oculus / WMR Chord Button`: 各デバイスの同時押し用ボタンを指定します。
- 同時押しを使わない場合は `Enable Chord Press` を OFF のままにします。

<img width="889" height="387" alt="image" src="https://github.com/user-attachments/assets/1a715c1d-d9bf-456d-8b95-8cc0b41cd98a" />

### 10. Events Change（登録イベントがある項目のみ表示）
- 対象コンポーネントで実際にイベント登録されている行だけ表示されます。
- 例:
  - `Click Events Change`
  - `Double Click Events Change`
  - `Long Click Events Change`
  - `Press Events Change`
  - `Hold Events Change`
  - `Release Events Change`
  - `Release After Long Click Events Change`
- 各行では「そのイベントに登録済みの処理一式」を、別イベント側へまとめて変更します。

### 11. Click Timing
- `Double Click Interval (s)`: ダブルクリック判定に使う間隔秒数です。
- `Long Click Interval (s)`: ロングクリック判定に使う長押し秒数です。
- いずれもターゲットごとに個別保存されます。

## ボタンのイベント発生条件について

<img width="2200" height="852" alt="01_press_hold_release" src="https://github.com/user-attachments/assets/f3970142-2a2b-49d7-b05f-81b11d8f7081" />
<img width="2200" height="852" alt="02_click" src="https://github.com/user-attachments/assets/f8624b4d-d00d-4b8a-b6cf-a0c9835fd5e6" />
<img width="2200" height="852" alt="03_double_click" src="https://github.com/user-attachments/assets/2537cab9-3e96-406f-90e0-8b0556cf25fe" />
<img width="2200" height="852" alt="04_long_click_release_after_long_click" src="https://github.com/user-attachments/assets/4a875d30-4d16-4775-a587-d0c9aa2bfd8a" />

