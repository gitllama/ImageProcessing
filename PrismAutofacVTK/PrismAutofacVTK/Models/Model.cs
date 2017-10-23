using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Media3D;

namespace PrismAutofacVTK.Models
{
    public static class ModelBilder
    {
        public static Model Build()
        {
            var path = System.AppDomain.CurrentDomain.BaseDirectory;
            using (var sr = new System.IO.StreamReader(System.IO.Path.Combine(path, "config.yaml")))
            {
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                return deserializer.Deserialize<Model>(sr);
            }
        }
    }


    //鏡面処理はRotでやる？（原点左上化）

    public class Model : BindableBase
    {

        public vtkActor AreaActor = vtkActor.New();
        public vtkCubeAxesActor CubeAxesActor = vtkCubeAxesActor.New();
        public vtkActor DataActor = vtkActor.New();
        vtkPolyData pointPoly = vtkPolyData.New();
        vtkLookupTable lut = vtkLookupTable.New();

        IntPtr data;
        
        /*プロパティ*/

        public enum Plane
        {
            Flat,
            Curve
        }

        [ReadOnly(true)]
        public Plane plane { get; set; } = Plane.Flat;

        private int _width = 1;
        [ReadOnly(true)]
        public int Width
        {
            get { return _width; }
            set { SetProperty(ref _width, value, () => SetAxes()); }
        }

        private int _height = 1;
        [ReadOnly(true)]
        public int Height
        {
            get { return _height; }
            set { SetProperty(ref _height, value, () => SetAxes()); }
        }

        public int Size { get { return Width * Height; } }

        private int _depth = 1;
        [ReadOnly(true)]
        public int Depth
        {
            get { return _depth; }
            set { SetProperty(ref _depth, value, () => SetAxes()); }
        }

        private double _scale = 1;
        public double Scale
        {
            get { return _scale; }
            set { SetProperty(ref _scale, value, () => { SetAxes(); SetData(null); }); }
        }


        private double _scalarRangeMin = 0;
        public double ScalarRangeMin
        {
            get { return _scalarRangeMin; }
            set { SetProperty(ref _scalarRangeMin, value, () => { SetAxes(); SetData(null); }); }
        }
        private double _scalarRangeMax = 1;
        public double ScalarRangeMax
        {
            get { return _scalarRangeMax; }
            set { SetProperty(ref _scalarRangeMax, value, () => { SetAxes(); SetData(null); }); }
        }


        private float _pointSize = 2;
        public float PointSize
        {
            get { return _pointSize; }
            set { SetProperty(ref _pointSize, value, () => SetData(null)); }
        }



        public void Init()
        {
            lut.SetHueRange(0.0, 0.667); /* 赤-> 青*/
            lut.Build();

            switch (plane)
            {
                case Plane.Curve:
                    InitCurve();
                    break;
                case Plane.Flat:
                    InitFlat();
                    break;
            }
        }

        public void SetData(float[] src)
        {
            switch (plane)
            {
                case Plane.Curve:
                    SetCurveData(src);
                    break;
                case Plane.Flat:
                    SetFlatData(src);
                    break;
            }
            RaisePropertyChanged(nameof(DataActor));
        }

        public void SetAxes()
        {
            switch (plane)
            {
                case Plane.Curve:
                    SetAxesCurve();
                    break;
                case Plane.Flat:
                    SetAxesFlat();
                    break;
            }
            RaisePropertyChanged(nameof(AreaActor));
        }



        private void InitFlat()
        {
            using (var points = vtkPoints.New())
            using (var vertices = vtkCellArray.New())
            {
                // Create topology of the points (a vertex per point)
                int[] ids = new int[Size];
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        //ids[x + y * Width] = points.InsertNextPoint(x - Width/2, Height/2 - y, 0);
                        ids[x + y * Width] = points.InsertNextPoint(x, y, 0);

                IntPtr pIds = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * Size);
                Marshal.Copy(ids, 0, pIds, Size);
                vertices.InsertNextCell(Size, pIds);
                Marshal.FreeHGlobal(pIds);

                // Create a polydata object
                // Set the points and vertices we created as the geometry and topology of the polydata
                pointPoly.SetPoints(points);
                pointPoly.SetVerts(vertices);
            }
        }

        //本来は平面ではないので
        private void InitCurve()
        {
            using (var points = vtkPoints.New())
            using (var vertices = vtkCellArray.New())
            {
                // Create topology of the points (a vertex per point)
                int[] ids = new int[Size];
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        ids[x + y * Width] = points.InsertNextPoint(0, 0, 0);

                IntPtr pIds = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * Size);
                Marshal.Copy(ids, 0, pIds, Size);
                vertices.InsertNextCell(Size, pIds);
                Marshal.FreeHGlobal(pIds);

                // Create a polydata object
                // Set the points and vertices we created as the geometry and topology of the polydata
                pointPoly.SetPoints(points);
                pointPoly.SetVerts(vertices);
            }
        }


        private void SetFlatData(float[] src)
        {
            using (var warp = vtkWarpScalar.New())
            using (var mapper = vtkPolyDataMapper.New())
            using (var actor = vtkActor.New())
            {
                if(src != null)
                {
                    IntPtr pIds = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(float)) * Size);
                    Marshal.Copy(src, 0, pIds, Size);

                    var da = vtkFloatArray.New();
                    da.SetArray(pIds, Size, 1);

                    pointPoly.GetPointData().SetScalars(da);
                }
                warp.SetInput(pointPoly);
                warp.SetScaleFactor(Scale);

                //var transform = vtkTransform.New();
                //transform.RotateX(180);
                //var transformFilter = vtkTransformPolyDataFilter.New();
                //transformFilter.SetTransform(transform);
                //transformFilter.SetInputConnection(warp.GetOutputPort());

                //mapper.SetInput(transformFilter.GetOutput());
                mapper.SetInput(warp.GetPolyDataOutput());

                mapper.SetLookupTable(lut);
                //mapper.SetColorModeToMapScalars();
                mapper.SetScalarRange(ScalarRangeMin, ScalarRangeMax);

                DataActor.SetMapper(mapper);
                DataActor.GetProperty().SetPointSize(PointSize);
                //DataActor.SetOrientation(90, 0, 0);
            }
        }


        float[] buf;
        private void SetCurveData(float[] src)
        {
            if (src != null) //(src != null)
            {
                buf = src;
            }
            else if(buf == null)
            {
                return;
            }
            using (var da = vtkFloatArray.New())
            using (var dav = vtkFloatArray.New())
            using (var warp = vtkWarpVector.New())
            using (var mapper = vtkPolyDataMapper.New())
            using (var actor = vtkActor.New())
            using (var transform = vtkTransform.New())
            {
                //IntPtr pIds = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(float)) * Size);
                //Marshal.Copy(src, 0, pIds, Size);
                //var da = vtkDoubleArray.New();
                //da.SetArray(pIds, Size, 1);

                //Scalarsの設定
                IntPtr pIds = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(float)) * Size);
                Marshal.Copy(buf, 0, pIds, Size);
                da.SetArray(pIds, Size, 1);
                pointPoly.GetPointData().SetScalars(da);

                //Vectorsの設定
                dav.SetNumberOfComponents(3);
                for (var y = 0; y < Height; y++)
                    for (var x = 0; x < Width; x++)
                    {
                        var s = buf[x + y * Width];
                        var Pos = new Vector3D(x - Width / 2, y - Height / 2, Depth * Scale);
                        Pos.Normalize();
                        dav.InsertNextTuple3(Pos.X * s, Pos.Y * s, Pos.Z * s);
                    }
                pointPoly.GetPointData().SetVectors(dav);
                pointPoly.Update();

                //transform.RotateWXYZ(20, 0, 1, 0);
                //transform.RotateY(90);
                //var transformFilter = vtkTransformPolyDataFilter.New();
                //transformFilter.SetTransform(transform);

                warp.SetInput(pointPoly);
                //warp.SetScaleFactor(Scale); //効いてる

                
                warp.Update();

                //transformFilter.SetInputConnection(warp.GetOutputPort());
                //mapper.SetInput(transformFilter.GetOutput());

                mapper.SetInput(warp.GetPolyDataOutput());
                mapper.SetLookupTable(lut);
                mapper.SetScalarRange(ScalarRangeMin, ScalarRangeMax);

                DataActor.SetMapper(mapper);
                DataActor.GetProperty().SetPointSize(PointSize);
            }
        }


        public void SetAxesFlat()
        {
            // Create a cube.  
            using (var cubeSource = vtkCubeSource.New())
            using (var mapper = vtkPolyDataMapper.New())
            using (var filter = vtkOutlineFilter.New())
            {
                cubeSource.SetXLength(Width);
                cubeSource.SetYLength(Height);
                cubeSource.SetZLength(Depth * Scale);
                cubeSource.SetCenter(Width / 2, Height / 2, Depth * Scale / 2);

                cubeSource.Update();

                filter.SetInputConnection(cubeSource.GetOutputPort());

                mapper.SetInputConnection(filter.GetOutputPort());

                AreaActor.SetMapper(mapper);

                //CubeAxesActor.RotateX(90);
                //CubeAxesActor.SetBounds(0, Width, 0, Height, 0, Depth);
                ////CubeAxesActor.SetZAxisTickVisibility(0);
                //CubeAxesActor.DrawZGridlinesOn();
                //CubeAxesActor.SetZAxisTickVisibility(0);
                ////Data.SetScale(Width, Height, Depth);
                //CubeAxesActor.SetYAxisRange(0, Height);
            }
        }

        public void SetAxesCurve()
        {
            using (var pts = vtkPoints.New())
            using (var colors = vtkUnsignedCharArray.New())
            using (var lines = vtkCellArray.New())
            using (var linesPolyData = vtkPolyData.New())
            using (var mapper = vtkPolyDataMapper.New())
            {
                //原点0の四角錐
                //面サイズどこにするかは後で（というか角度入力の方が良い
                pts.InsertNextPoint(0.0, 0.0, 0.0);
                pts.InsertNextPoint(- (Width / 2) , - (Height / 2) , Depth * Scale);
                pts.InsertNextPoint(+ (Width / 2) , - (Height / 2) , Depth * Scale);
                pts.InsertNextPoint(+ (Width / 2) , + (Height / 2) , Depth * Scale);
                pts.InsertNextPoint(- (Width / 2) , + (Height / 2) , Depth * Scale);

                int[,] l = {
                    { 0, 1 }, { 0, 2 }, { 0, 3 }, { 0, 4 },
                    { 1, 2 }, { 2, 3 }, { 3, 4 }, { 4, 1 }
                };
                for (int i = 0; i < l.GetLength(0); i++)
                {
                    vtkLine line = vtkLine.New();
                    line.GetPointIds().SetId(0, l[i, 0]);
                    line.GetPointIds().SetId(1, l[i, 1]);
                    lines.InsertNextCell(line);
                }

                linesPolyData.SetPoints(pts);
                linesPolyData.SetLines(lines);

                mapper.SetInput(linesPolyData);

                AreaActor.SetMapper(mapper);

            }
            //using (var colors = vtkUnsignedCharArray.New())
            //colors.SetNumberOfComponents(3);
            //colors.SetName("Colors");
            //colors.InsertNextValue(255);
            //colors.InsertNextValue(0);
            //colors.InsertNextValue(0);

            //linesPolyData.GetCellData().SetScalars(colors);
        }

    }


    public static class RawDataReader
    {
        public static void ReadRawInt32(this Model m, string filename, Func<int, float> func)
        {
            byte[] bytes = System.IO.File.ReadAllBytes(filename);
            int size = m.Size;
            float[] data = new float[size];

            for (int i = 0; i < size; i++)
                data[i] = func( BitConverter.ToInt32(bytes, i * 4));
           
            m.SetData(data);
        }

        public static void ReadRawPGM(this Model m, string filename, Func<int, float> func)
        {
            //横着
            var readText = System.IO.File.ReadAllText(filename).Split(new string[] { Environment.NewLine, ",", " " }, StringSplitOptions.RemoveEmptyEntries);
            int size = m.Size;
            float[] data = new float[size];
            foreach (var (item, index) in readText.Skip(4).Select((item, index) => (item, index)))
            {
                var buf = Int32.Parse(item);
                data[index] = buf;
            }

            m.SetData(data);
        }

        public static void ReadRawFloat(this Model m, string filename)
        {
            byte[] bytes = System.IO.File.ReadAllBytes(filename);
            int size = m.Size;
            float[] data = new float[size];

            for (int i = 0; i < size; i++)
                data[i] = BitConverter.ToSingle(bytes, i * 4);

            m.SetData(data);
        }
    }


    /***************************/

    public class VTK : BindableBase
    {
        int[] data;
        IntPtr pIds;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Depth { get; private set; }
        public int Size { get { return Width * Height; } }

        private double _s = 0;
        public double test
        {
            get { return _s; }
            set
            {
                _s = value;
                SetActor(value);
                RaisePropertyChanged();
            }
        }

        private double _Scale = 64;
        public double Scale
        {
            get { return _Scale; }
            set { SetProperty(ref _Scale, value); }
        }

        int maxdepth;
        int mindepth;

        //public vtkActor DataActor { get; set; } = new vtkActor();

        vtkPolyData pointPoly = vtkPolyData.New();
        vtkLookupTable lut = vtkLookupTable.New();

        public vtkActor SpaceActor = new vtkActor();
        public vtkActor DataActor = new vtkActor();

        public VTK(int width, int height, int depth)
        {
            Width = width;
            Height = height;
            Depth = depth;

            data = new int[Size];
            pIds = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * Size);

            //lutの生成
            lut.SetHueRange(0.667, 0.0);
            lut.Build();

            //セルの生成
            using (var points = vtkPoints.New())
            using (var vertices = vtkCellArray.New())
            {
                IntPtr pIds = Marshal.AllocHGlobal(Size * Marshal.SizeOf(typeof(int)));
                int[] ids = new int[Size];
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        ids[x + y * Width] = points.InsertNextPoint(x, -y, 0);
                    }
                }
                Marshal.Copy(ids, 0, pIds, Size);
                vertices.InsertNextCell(Size, pIds);
                Marshal.FreeHGlobal(pIds);
                // = vertex.GetPointIds().SetId(0, 0);

                //
                pointPoly.SetPoints(points);
                pointPoly.SetVerts(vertices);
            }
        }

        public void SetRaw(string filename)
        {
            byte[] bytes = System.IO.File.ReadAllBytes(filename);
            int count = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var i = BitConverter.ToInt32(bytes, (x + y * Width) * 4) / Scale;
                    data[count] = (int)(i > Depth ? Depth : i < 0 ? 0 : i);
                    count++;
                }
            }
        }

        public void SetActor(double warpScale)
        {
            //IntPtr pIds = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * data.Length);

            //スカラ量の設定
            using (var da = vtkIntArray.New())
            using (var warp = vtkWarpScalar.New())
            using (var mapper = vtkPolyDataMapper.New())
            {
                Marshal.Copy(data, 0, pIds, Size);
                da.SetArray((IntPtr)pIds, Size, 1);
                pointPoly.GetPointData().SetScalars(da);
                //Marshal.FreeHGlobal(pIds); //解放したらアカン

                warp.SetInput(pointPoly);
                warp.SetScaleFactor(warpScale);

                mapper.SetInput(warp.GetPolyDataOutput());
                //mapper.SetInput(pointPoly);
                mapper.SetLookupTable(lut);
                mapper.SetScalarRange(0.0, Depth);
                mapper.ScalarVisibilityOn();

                DataActor.SetMapper(mapper);
                DataActor.GetProperty().SetPointSize(2);

                RaisePropertyChanged("render");
            }

        }

        public void SetSpace()
        {
            using (var src = vtkCubeSource.New())
            using (var mapper = vtkPolyDataMapper.New())
            using (var outline = vtkOutlineFilter.New())
            {
                src.SetXLength(Width);
                src.SetYLength(Height);
                src.SetZLength(Depth);
                src.SetCenter(Width / 2, -1 * Height / 2, Depth / 2);

                outline.SetInput(src.GetOutput());

                mapper.SetInputConnection(outline.GetOutputPort());
                SpaceActor.SetMapper(mapper);
            }
        }
    }





    public class VTKModel
    {
        public static vtkActor ReadPLY(string filename)
        {
            using (var reader = new Kitware.VTK.vtkPLYReader())
            using (var mapper = new Kitware.VTK.vtkCompositePolyDataMapper())
            {
                var actor = new Kitware.VTK.vtkActor();

                reader.SetFileName(filename);
                mapper.SetInputConnection(reader.GetOutputPort());
                actor.SetMapper(mapper);
                return actor;
            }
        }

        public static void Point(vtkActor actor, int[] src, int w, int h)
        {
            using (var lut = vtkLookupTable.New())
            using (var points = vtkPoints.New())
            using (var vertices = vtkCellArray.New())
            using (var pointPoly = vtkPolyData.New())
            using (var mapper = vtkPolyDataMapper.New())
            {
                lut.SetHueRange(0.667, 0.0);
                lut.Build();

                int[] ids = new int[w * h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        ids[x + w * y] = points.InsertNextPoint(x, y, src[x + w * y]);
                    }
                }


                int size = Marshal.SizeOf(typeof(int)) * w * h;
                IntPtr pIds = Marshal.AllocHGlobal(size);
                Marshal.Copy(ids, 0, pIds, w * h);
                vertices.InsertNextCell(w * h, pIds);
                Marshal.FreeHGlobal(pIds);

                pointPoly.SetPoints(points);
                pointPoly.SetVerts(vertices);

                //fixed (int* pa = data)
                //{
                //    var da = vtkIntArray.New();
                //    da.SetArray((IntPtr)pa, w * h, 1);

                //    pointPoly.GetPointData().SetScalars(da);
                //}

                mapper.SetInput(pointPoly);

                mapper.SetLookupTable(lut);
                mapper.SetScalarRange(0.0, 255.0);

                actor.SetMapper(mapper);
                actor.GetProperty().SetPointSize(2);
            }
        }
    }

    public class VTKPoint
    {
        int width;
        int height;
        int depth;

        double scale = 1;

        int maxdepth;
        int mindepth;


        int[] data;
        IntPtr pIds;
        vtkPolyData pointPoly = vtkPolyData.New();
        vtkLookupTable lut = vtkLookupTable.New();


        public VTKPoint(int w, int h, int d)
        {
            width = w;
            height = h;
            depth = h;
            data = new int[w * h];
            pIds = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * data.Length);

            //lut
            lut.SetHueRange(0.667, 0.0);
            lut.Build();

            //セルの生成
            using (var points = vtkPoints.New())
            using (var vertices = vtkCellArray.New())
            {
                int[] ids = new int[w * h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        ids[x + w * y] = points.InsertNextPoint(x, -y, 0);
                    }
                }
                int size = Marshal.SizeOf(typeof(int)) * w * h;
                IntPtr pIds = Marshal.AllocHGlobal(size);
                Marshal.Copy(ids, 0, pIds, w * h);
                vertices.InsertNextCell(w * h, pIds);
                Marshal.FreeHGlobal(pIds);

                pointPoly.SetPoints(points);
                pointPoly.SetVerts(vertices);
            }
        }

        public void SetScale(double val)
        {
            scale = val;
        }

        public void SetRaw(string filename)
        {
            byte[] bytes = System.IO.File.ReadAllBytes(filename);
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var i = BitConverter.ToInt32(bytes, (x + y * width) * 4) / scale;
                    data[count] = (int)(i > depth ? depth : i < 0 ? 0 : i);
                    count++;
                }
            }

            //スカラ量の設定
            using (var da = vtkIntArray.New())
            {
                //IntPtr pIds = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * data.Length);
                Marshal.Copy(data, 0, pIds, data.Length);
                da.SetArray((IntPtr)pIds, data.Length, 1);
                pointPoly.GetPointData().SetScalars(da);
                //Marshal.FreeHGlobal(pIds); //解放したらアカン
            }
        }



        public vtkActor GetActor()
        {
            using (var warp = vtkWarpScalar.New())
            using (var mapper = vtkPolyDataMapper.New())
            {
                warp.SetInput(pointPoly);
                warp.SetScaleFactor(1);

                mapper.SetInput(warp.GetPolyDataOutput());
                //mapper.SetInput(pointPoly);
                mapper.SetLookupTable(lut);
                mapper.SetScalarRange(0.0, depth);
                var actor = new Kitware.VTK.vtkActor();
                actor.SetMapper(mapper);
                actor.GetProperty().SetPointSize(2);

                return actor;
            }

        }


        public vtkAxesActor Axes()
        {
            var axes = vtkAxesActor.New();
            using (var transform = vtkTransform.New())
            {
                transform.Translate(0.0, -1 * height, 0.0);
                transform.Scale(0.5, 0.5, 0.5);
                axes.SetUserTransform(transform);
                axes.SetTotalLength(width, height, depth);
                axes.SetNormalizedShaftLength(1, 1, 1);
                axes.SetNormalizedTipLength(0.05, 0.05, 0.05);

                //axes.SetOrigin(0.0, -1 * height, 0.0);
                //axes.Set

                //axes.SetZAxisLabelText("Depth");

                axes.SetAxisLabels(1);//1 on 0 off

                return axes;
            }
        }

        public vtkProp Space()
        {
            var actor = vtkActor.New();
            using (var pts = vtkPoints.New())
            using (var colors = vtkUnsignedCharArray.New())
            using (var lines = vtkCellArray.New())
            using (var linesPolyData = vtkPolyData.New())
            using (var mapper = vtkPolyDataMapper.New())
            {
                pts.InsertNextPoint(0.0, 0.0, depth);
                pts.InsertNextPoint(0.0, -1 * height, depth);
                pts.InsertNextPoint(width, -1 * height, depth);
                pts.InsertNextPoint(width, 0.0, depth);

                pts.InsertNextPoint(0.0, 0.0, 0.0);
                pts.InsertNextPoint(0.0, -1 * height, 0.0);
                pts.InsertNextPoint(width, -1 * height, 0.0);
                pts.InsertNextPoint(width, 0.0, 0.0);

                int[,] l = {
                    { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
                    { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
                    { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
                };
                for (int i = 0; i < l.GetLength(0); i++)
                {
                    vtkLine line = vtkLine.New();
                    line.GetPointIds().SetId(0, l[i, 0]);
                    line.GetPointIds().SetId(1, l[i, 1]);
                    lines.InsertNextCell(line);
                }

                linesPolyData.SetPoints(pts);
                linesPolyData.SetLines(lines);
                mapper.SetInput(linesPolyData);
                actor.SetMapper(mapper);
                return actor;
            }
            //using (var colors = vtkUnsignedCharArray.New())
            //colors.SetNumberOfComponents(3);
            //colors.SetName("Colors");
            //colors.InsertNextValue(255);
            //colors.InsertNextValue(0);
            //colors.InsertNextValue(0);

            //linesPolyData.GetCellData().SetScalars(colors);

        }


        public static vtkProp Grid(int x, int y, int z)
        {
            var actor = vtkActor.New();
            using (var pts = vtkPoints.New())
            using (var colors = vtkUnsignedCharArray.New())
            using (var lines = vtkCellArray.New())
            using (var linesPolyData = vtkPolyData.New())
            using (var mapper = vtkPolyDataMapper.New())
            {
                int count = 0;
                for (var _z = 0; _z < 10; _z++)
                {
                    for (var _x = 0; _x < x; _x += x / 20)
                    {
                        pts.InsertNextPoint(_x, 0.0, _z * z);
                        pts.InsertNextPoint(_x, -1 * y, _z * z);

                        vtkLine line = vtkLine.New();
                        line.GetPointIds().SetId(0, count++);
                        line.GetPointIds().SetId(1, count++);
                        lines.InsertNextCell(line);
                    }
                    for (var _y = 0; _y < y; _y += y / 20)
                    {
                        pts.InsertNextPoint(0.0, -1 * _y, _z * z);
                        pts.InsertNextPoint(x, -1 * _y, _z * z);

                        vtkLine line = vtkLine.New();
                        line.GetPointIds().SetId(0, count++);
                        line.GetPointIds().SetId(1, count++);
                        lines.InsertNextCell(line);
                    }
                }

                linesPolyData.SetPoints(pts);
                linesPolyData.SetLines(lines);
                //linesPolyData.GetCellData().SetScalars(colors);

                mapper.SetInput(linesPolyData);
                actor.SetMapper(mapper);
                return actor;
            }
        }


        public static vtkProp Plane(int x, int y, int z)
        {
            var actor = vtkActor.New();
            using (var planeSource = vtkPlaneSource.New())
            using (var mapper = vtkPolyDataMapper.New())
            {
                //planeSource.SetCenter(0.0, 0.0, 100.0);

                planeSource.SetOrigin(0.0, 0.0, 500);

                planeSource.SetPoint1(2256, 0.0, 500);
                planeSource.SetPoint2(0.0, -1178.0, 500);

                //planeSource.SetNormal(1.0, 0.0, 1.0);
                planeSource.Update();

                mapper.SetInputConnection(planeSource.GetOutputPort());
                actor.SetMapper(mapper);
                actor.GetProperty().SetOpacity(0.4);
                return actor;
            }
        }


        public vtkProp ScaleTest()
        {
            var axes = vtkCubeAxesActor.New();
            using (var transform = vtkTransform.New())
            {
                transform.Translate(0.0, -1 * height, 0.0);
                //transform.Scale(size, size, size);
                axes.SetUserTransform(transform);

                axes.SetDrawZGridlines(1);


                return axes;
            }
        }

    }
}


/*
    Timer timer = new Timer();


    int size = 0;
    public void OnTick_FormsTimer(object sender, EventArgs e)
    {
        renderer.RemoveAllViewProps();
        roop();
        size++;
        if (size > 255) size = 0;

        RenderControl.RenderWindow.Render();
    }

    public void Sphere(vtkRenderer renderer, int x, int y, int z)
    {
        void hoge(vtkRenderer _renderer)
        {
            using (var src = vtkSphereSource.New())
            using (var mapper = vtkPolyDataMapper.New())
            using (var actor = new vtkActor())
            {
                src.SetPhiResolution(5);
                src.SetThetaResolution(5);

                src.SetCenter(x, y, z * 3);
                src.SetRadius(20);
                mapper.SetInputConnection(src.GetOutputPort());
                actor.SetMapper(mapper);
                actor.GetProperty().SetColor(z / 256.0, (255.0 - z) / 256.0, 0);
                _renderer.AddActor(actor);
            }
        }

        hoge(renderer);
    }

    public void Cube(vtkRenderer renderer, int x, int y, int z)
    {
        void hoge(vtkRenderer _renderer)
        {
            using (var src = vtkCubeSource.New())
            using (var mapper = vtkPolyDataMapper.New())
            using (var actor = new vtkActor())
            {
                src.SetXLength(20);
                src.SetYLength(20);
                src.SetZLength(20);
                src.SetCenter(x, y, z * 3);

                mapper.SetInputConnection(src.GetOutputPort());
                actor.SetMapper(mapper);
                actor.GetProperty().SetColor(z / 256.0, (255.0 - z) / 256.0, 0);
                _renderer.AddActor(actor);
            }
        }

        hoge(renderer);
    }

    public void Map(vtkRenderer renderer, int[] src, int width, int height)
    {
        using (var mapper = vtkPolyDataMapper.New())
        using (var actor = new vtkActor())
        using (var da = vtkIntArray.New())
        using (var _src = vtkImageData.New())
        using (var lut = vtkLookupTable.New())
        using (var igf = vtkImageDataGeometryFilter.New())
        using (var warp = vtkWarpScalar.New())
        {
            //fixed (int* pa = src)
            //{
            //    da.SetArray((IntPtr)pa, width * height, 1);

            //    _src.SetScalarTypeToInt();
            //    _src.SetDimensions(width, height, 1);
            //    _src.SetSpacing(0.1, 0.1, 0);
            //    _src.GetPointData().SetScalars(da);

            //    lut.SetHueRange(0.667, 0.0);
            //    lut.Build();

            //    igf.SetInput(_src);

            //    warp.SetInput(igf.GetOutput());
            //    warp.SetScaleFactor(1.0 / 255);

            //    mapper.SetInput(warp.GetPolyDataOutput());
            //    mapper.SetLookupTable(lut);
            //    mapper.SetColorModeToMapScalars();
            //    mapper.SetScalarRange(0.0, 255.0);

            //    actor.SetMapper(mapper);
            //    renderer.AddActor(actor);
            //}
        }

    }

    public void Map2(vtkRenderer renderer, int[] src, int width, int height)
    {
        using (var mapper = vtkOpenGLPolyDataMapper.New())
        using (var actor = vtkOpenGLActor.New())
        using (var da = vtkIntArray.New())
        using (var _src = vtkImageData.New())
        using (var lut = vtkLookupTable.New())
        using (var igf = vtkImageDataGeometryFilter.New())
        using (var warp = vtkWarpScalar.New())
        {
            //fixed (int* pa = src)
            //{
            //    da.SetArray((IntPtr)pa, width * height, 1);

            //    _src.SetScalarTypeToInt();
            //    _src.SetDimensions(width, height, 1);
            //    _src.SetSpacing(1, 1, 1);
            //    _src.GetPointData().SetScalars(da);

            //    lut.SetHueRange(0.667, 0.0);
            //    lut.Build();

            //    igf.SetInput(_src);

            //    warp.SetInput(igf.GetOutput());
            //    warp.SetScaleFactor(1.0 / 1);

            //    mapper.SetInput(warp.GetPolyDataOutput());
            //    mapper.SetLookupTable(lut);
            //    mapper.SetColorModeToMapScalars();
            //    mapper.SetScalarRange(0.0, 255.0);

            //    actor.SetMapper(mapper);
            //    renderer.AddActor(actor);
            //}
        }

    }

var sphere = vtkSphereSource.New();
sphere.SetThetaResolution(8);
sphere.SetPhiResolution(16);

var shrink = vtkShrinkPolyData.New();
shrink.SetInputConnection(sphere.GetOutputPort());
shrink.SetShrinkFactor(0.9);

var move = vtkTransform.New();
move.Translate(_random.NextDouble(), _random.NextDouble(), _random.NextDouble());
var moveFilter = vtkTransformPolyDataFilter.New();
moveFilter.SetTransform(move);

moveFilter.SetInputConnection(shrink.GetOutputPort());

var mapper = vtkPolyDataMapper.New();
mapper.SetInputConnection(moveFilter.GetOutputPort());

// The actor links the data pipeline to the rendering subsystem 
actor.SetMapper(mapper);
actor.GetProperty().SetColor(1, 0, 0);

*/

/*
    glyphs = vtk.vtkGlyph3D() 
glyphs.SetScaleFactor(0.1); # ベクトルの大きさを調整
glyphs.SetScaleModeToScaleByVector() # ベクトルの大きさでスケール調整
glyphs.SetSource(vector.GetOutput()) # ベクトルに使用する形を設定
glyphs.SetInput(grid) # データを指定 
*/
