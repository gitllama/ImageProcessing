# -*- coding: utf-8 -*-

import numpy as np
import cv2

#[関数]画像の読み込み1
def ReadRaw200(filepath):
    width = 2256
    height = 1178
    data = np.fromfile(filepath,np.int32)
    return data.reshape((height, width))
    #mat = data.reshape((width, height)).T
   
#[関数]画像の読み込み2 
def ReadRaw4K(filepath):
    width = 3840
    height = 2160

    #           バイトオーダ/サイズ/アラインメント
    #@ or 略    native/native/native
    #=          native/standard/none
    #<          リトルエンディアン	/standard/none
    #>          ビッグエンディアン/standard/none
    #!          ネットワーク (= ビッグエンディアン)/standard/none
    
    #       Cの型/Pythonの型/標準のサイズ
    #x	pad byte/no value/	 
    #c	char/長さ1の文字列/1	 
    #b	signed char/integer/1
    #B	unsigned char/integer/1
    #?	_Bool/真偽値型(bool)/1
    #h	short/integer/1
    #H	unsigned short/integer/1
    #i	int/integer/4
    #I	unsigned int/integer/4
    #l	long/integer/4
    #L	unsigned long/integer/4
    #q	long long/integer/8
    #Q	unsigned long long/integer/8
    #f	float/float/4
    #d	double/float/8
    #s	char[]/string/
    #p	char[]/string/ 
    #P	void */integer/
    
    #np.int8, uini8, int32, uint32, float32, float64 
    data = np.fromfile(filepath,'>h')   
    return data.reshape((height, width))
    
#[関数]画像の読み込み3 
def ReadRawASCII(filepath):
    width = 3840
    height = 2160
    bytesize = 4
    f=open(filepath,"rb")
    data = f.read(bytesize*width*height)
    #ASCIIで読んでしまうので変換面倒くさい
    #NumPyで読んだ方が手っ取り早い

#[関数]HOB処理
def HOB(src):
    
    w = src.shape[1]

    #切り出し　X[start:end:step, start:end:step]
    #平均 mean(axis), axis : 1 = 2次元方向 0 = 1次元方向
    hob_l = src[:,12:128+12].mean(1).reshape(-1,1)
    hob_r = src[:,w-12-128:w-12].mean(1).reshape(-1,1)
    
    #四則演算(ブロードキャスト)
    return src - ((hob_l + hob_r) / 2)

#[関数]スタッガー処理
def Stagger(src, flag):
    
    #高さの取得
    h = src.shape[0]
    w = src.shape[1]
    #マスクの作成
    mask_odd = 1 - np.arange(h).reshape(h,1) % 2
    mask_even = np.arange(h).reshape(h,1) % 2

    #マスク
    mat_odd = src * mask_odd
    mat_even = src * mask_even
    
    #スタッガ
    #スタッガ
    spacer = np.zeros(h).reshape(h,1)
    if flag in {'R', 'r'}:
        mat_even = np.hstack((spacer, mat_even))
        mat_even = np.delete(mat_even, w, 1)
    elif flag in {'L', 'l'}:
        mat_even = np.hstack((mat_even, spacer))
        mat_even = np.delete(mat_even, 0, 1)
    ##a3 = np.r_[a1,a2]

    return mat_odd + mat_even
    
#[関数]デモザイク
def Demosaic(src, RGain, BGain):
    
    #クリップ/16bit化
    src[src>65535] = 65535
    src[src<0] = 0
    mat16 = src.astype(np.uint16)
    #mat8 = matfloat.astype(np.uint8)
    #matf = matfloat.astype(np.float32)
    
    #デモザイク
    #depth == CV_8U || depth == CV_16U
    pic = cv2.cvtColor(mat16, cv2.COLOR_BAYER_GR2BGR)
    
    #色ゲイン/8bit化
    gain = np.array([ BGain,  1,  RGain]).reshape(1,1,3)
    pic = pic * gain
    pic[pic>255] = 255
    return pic.astype(np.uint8)

#[関数]画像の表示 
def Show(pic):
    #Matplotlibつかってもよいかも
    # cv2.WINDOW_NORMAL：ウィンドウのサイズを変更可能にする
    cv2.namedWindow('image', cv2.WINDOW_NORMAL)
    cv2.imshow('image', pic)
    cv2.waitKey(0)
    cv2.destroyAllWindows()
    
#このモジュールが(他のプログラムから呼び出されたのではなく)自身が実行された場合
if __name__ == "__main__":
    #print("{0}".format(fibo(100)))
    print("null")