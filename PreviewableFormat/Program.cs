using Force.Crc32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PNGTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var src = File.ReadAllBytes("000.bin");
            var raw = ReadRaw(src, 2256, 1178);
            var png = Raw2PNG(raw, 2256, 1178);
            var dst = UnionPngRaw(png, src);

            File.WriteAllBytes("out.raw.png", dst);
        }


        static int[] ReadRaw(byte[] src, int width, int height)
        {
            int[] dst = new int[width * height];
            for (int i = 0; i < dst.Length; i++)
            {
                dst[i] = BitConverter.ToInt32(src, i * 4);
            }
            return dst;
        }


        static byte[] Raw2PNG(int[] src, int width, int height)
        {
            byte[] dst = new byte[width * height];

            double ave = src.Average();
            ave = ave < 125 ? 125 : ave;

            for (int i = 0; i < src.Length; i++)
            {
                double hoge = src[i] / (ave * 2) * 255;
                dst[i] = hoge > 255 ? (byte)255 : hoge < 0 ? (byte)0 : (byte)hoge;
            }

            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), dst, width * 1, 0, 0);
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }


        static byte[] UnionPngRaw(byte[] src_png, byte[] src_raw)
        {
            var IEND = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };
            var raWd = new byte[] { 0x72, 0x61, 0x57, 0x64 };

            byte[] dst = new byte[src_png.Length + src_raw.Length + 12];

            //末尾チェック
            var IENDCheck = src_png.Skip(src_png.Length - 12).Take(12);
            if(!IENDCheck.SequenceEqual(IEND)) throw new Exception("PNG ERR");

            //ローカル関数
            void CopyArray(byte[] _src, byte[] _dst, ref int _count)
            {
                Array.Copy(_src, 0, _dst, _count, _src.Length);
                _count += _src.Length;
            }

            //PNG
            int count = src_png.Length - 12;
            Array.Copy(src_png, 0, dst, 0, src_png.Length - 12); //PNG

            //length ビックエンディアン
            var size = BitConverter.GetBytes((uint)src_raw.Length);
            Array.Reverse(size);
            CopyArray(size, dst, ref count);

            //raWd
            CopyArray(raWd, dst, ref count);

            //Data
            CopyArray(src_raw, dst, ref count);


            //CRC32
            uint crc = Crc32Algorithm.Compute(dst, count - src_raw.Length - raWd.Length, src_raw.Length + raWd.Length);
            var crc_array = BitConverter.GetBytes(crc);
            Array.Reverse(crc_array);
            CopyArray(crc_array, dst, ref count);

            //IEND
            CopyArray(IEND, dst, ref count);

            return dst;
        }
    }
}
