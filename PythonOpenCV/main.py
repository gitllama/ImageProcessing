# -*- coding: utf-8 -*-

import os
from module import cvmodule
#or import module.cvmodule

print('てすと')

#各種フォルダの取得
desktop_path = os.getenv("HOMEDRIVE") + os.getenv("HOMEPATH") + "\\Desktop"
mydocument_path = os.getenv("HOMEDRIVE") + os.getenv("HOMEPATH") + "\\Documents"
mypicrure_path = os.getenv("HOMEDRIVE") + os.getenv("HOMEPATH") + "\\Pictures"
myvideo_path = os.getenv("HOMEDRIVE") + os.getenv("HOMEPATH") + "\\Videos"
mymusic_path = os.getenv("HOMEDRIVE") + os.getenv("HOMEPATH") + "\\Music"

#画像の読み込み
matuint = cvmodule.ReadRaw200(mydocument_path +  "\\000.bin")
matdark = cvmodule.ReadRaw200ave(mydocument_path +  "\\ave.bin")
#mat = ReadRaw200("D:\\OneDrive\\画像\\raw\\u10_Ship_4K.b")

#デジタル処理
matuint = matuint - matdark                     #ダーク減算
matuint = matuint >> 10                         #ビットシフト
matfloat = cvmodule.HOB(matuint)                #HOB処理
matfloat = matfloat + 0                         #オフセット
matfloat = cvmodule.Stagger(matfloat,"L")       #スタッガー

#先にトリム
image = cvmodule.Demosaic(matfloat[], 1.4, 2.2)   #デモザイク　+　色ゲイン

#画像の表示
cvmodule.Show(image)

#保存
