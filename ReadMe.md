# FlatsPlus

# 概要
Flats(Flat, FlatAnniversary, Comodo, Fel, Heon, Kewf)に対応したアバターギミックセットです

# 実装済み

## Link
- FlatsPlus:Link利用者同士でパラメータの通信を行う

## Sync
- アンチカリング

## Carry (ABTから学んだ内容が含まれます)
- 他者を移動
  - Gate(テレポータ)設置(自動強制同期)
  - Link:他者にGateを出させる
  - 他者をだっこ
  - 他者をおんぶ

## Ico
- 頭の上にアイコンを出す
  - Link:Resonance … 他者にアイコンを出させる
  - Link:VerView … 他者にFlatsPlusのバージョンを出させる

## Move
- 色々な移動ツール
  - 浮遊
  - 超高速ダッシュ (速度調整)
  - 連続ダッシュ (高度調整)
  - おんぶ(Carry呼び出し)
- NDMF
  - (乗り物変更)

## FlatEmo
- そのまま再配布

## Pen
- 空中に文字を書く
  - (左利きモード)
  
## MeshSetting
- 全体的な明るさ等の自動調整

  
## Light (Discordから学んだ内容が含まれます)
- 懐中電灯
- ワールドライト
- ONOFFに課題あり


## Tail
  - サイズ変更
  - ゆらす
  - PB判定の適正化
  - (表情連携)
  - 重力変更（安定しなかったので中止）

## Onaka
- おなかをぽよぽよに
  - 判定深度

## Explorer (ABTから学んだ内容が含まれます)
- 軌跡表示
- 位置表示（自動強制同期）


## Emo
- Gestureに対する表情を設定
  - anim/shapekeyとは別の抽象化された名称による設定
  - 変更時間の設定
  
## MakeEmo 
- 任意の表情を作ってロック

## Sleep
- 3点睡眠

## PoseClipper
- そのまま再配布

## DanceController
- Dance時表情が動く
- ついでにFxのアバターマスクを削除

## HopePoyo
- ほっぺたをぽよぽよに
  - 触ると揺れる
  - 引っ張ると膨らむ
  - その状態で固定
  - 一部の表情時に自動で無効化

## NadePo
- 触るとほっぺたを赤くする
  - 撫でられ範囲の設定

## 距離クリッピングキャンセラーを自動で設定するやつ

## WDONで統一するやつ

## Manager
- 各機能のざっくりしたONOFFおよび全Inspectorの表示
- 状態保存
- (状態エクスポート)

## Localize
- 言語対応　ja, en, ko, zh-CN, zh-TW

## FxMinimize
- デフォルト配布のFxレイヤを最適化

# 依存
- liltoon
- Modular Avatar
- PandraVase

# Thanks
- [Display Number](https://github.com/noriben327/DisplayNumber) … Commの開発・デバッグにあたって活用させて頂きました
- [cgtrader Teddy bear](https://www.cgtrader.com/3d-models/animals/mammal/teddy-bear-dc0f9bd6-2d21-4c9b-b3fc-9c8d7d9c1c93) … Moverの位置確認用キャラクターとして使用しています
- [tabler icons](https://tabler.io/icons) … 電球、星、ミュートアイコン
- [SVG REPO](https://www.svgrepo.com/) … 各種アイコン
- [手描きMatcap集](https://booth.pm/ja/items/3809629) … ピン画像素材
