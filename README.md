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
