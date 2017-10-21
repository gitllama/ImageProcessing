PythonOpenCV
===============================
PythonでRawデータを読んで画像にします

使い方
-------------------------------
* 読み込みファイル
手っ取り早い入手先としては、ITE(情報メディア学会)にて[超高精細・広色域標準静止画像][linkref]でサンプルが提供されています。
* 以上

処理順
-------------------------------

抜粋先忘れた

>1.基本回路
>1-1.色分離  
>1-2.色信号処理（マトリックス・肌色補正）  
>1-3.輝度信号処理  
 (ガンマ・ニー・マスキング・ディテール・フレア・シェーディング・ホワイトクリップ・ダーククリップ)
>2.高画質化処理  
>2-1. コントラスト  
>2-2.ダイナミックレンジ  
>2-3.デモザイキング  
>2-4.超解像  

ここからNTSC, YprPb/YUV出力される

たとえばこんな順番

>dark-frame subtraction  
>VFPN canceller  
>HOB clamper / HRN canceller  
>demosaic / debayer  
>Black ballance  
>R/B Gain / White ballance  
>Pedestal  
>Noise reducer  
>Matrix  
>color correction  
>image enhancer  
>gamma correction  
>knee  
>YPbPr  
>Chroma  
>LPF  

OBはラインメモリとの兼ね合いを考えつつ（Vの頭、Hの頭でリアルタイムに反映）  
事前の平均フレーム/リアルタイムの移動平均/シングルフレームを使い分けてFPN、ランダムノイズの  
補正かけます


[linkref]: http://www.ite.or.jp/content/chart/uhdtv/ "テストチャート・超高精細・広色域標準静止画像"
