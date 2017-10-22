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

    public class Model : BindableBase
    {

        public vtkActor Data = vtkActor.New();
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

        private float _pointSize = 2;
        public float PointSize
        {
            get { return _pointSize; }
            set { SetProperty(ref _pointSize, value, () => SetData(null)); }
        }



        public void Init()
        {
            lut.SetHueRange(0.667, 0.0); /* 青-> 赤*/
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
        }

        public void SetAxes()
        {
            switch (plane)
            {
                case Plane.Curve:
                    //SetCurveData(src);
                    break;
                case Plane.Flat:
                    SetAxesFlat();
                    break;
            }
        }


        public void InitFlat()
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

        public void SetFlatData(float[] src)
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

                var transform = vtkTransform.New();
                transform.RotateX(180);
                var transformFilter = vtkTransformPolyDataFilter.New();
                transformFilter.SetTransform(transform);
                transformFilter.SetInputConnection(warp.GetOutputPort());

                //mapper.SetInput(transformFilter.GetOutput());
                mapper.SetInput(warp.GetPolyDataOutput());

                mapper.SetLookupTable(lut);
                //mapper.SetColorModeToMapScalars();
                mapper.SetScalarRange(0.0, Depth);



                DataActor.SetMapper(mapper);
                DataActor.GetProperty().SetPointSize(PointSize);
            }
            RaisePropertyChanged(nameof(DataActor));
        }

        //本来は平面ではないので
        public void InitCurve()
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

        public void SetCurveData(float[] src)
        {
            using (var warp = vtkWarpVector.New())
            using (var mapper = vtkPolyDataMapper.New())
            using (var actor = vtkActor.New())
            using (var transform = vtkTransform.New())
            {
                //IntPtr pIds = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(float)) * Size);
                //Marshal.Copy(src, 0, pIds, Size);
                //var da = vtkDoubleArray.New();
                //da.SetArray(pIds, Size, 1);
                if (src != null)
                {
                    var dav = vtkFloatArray.New();
                    dav.SetNumberOfComponents(3);

                    for (var y = 0; y < Height; y++)
                        for (var x = 0; x < Width; x++)
                        {
                            var count = (x + y * Width);

                            var Pos = new Vector3D(x - Width / 2, y - Height / 2, Depth);
                            Pos.Normalize();

                            dav.InsertNextTuple3(Pos.X * src[count], Pos.Y * src[count], Pos.Z * src[count]);
                        }
                    pointPoly.GetPointData().SetVectors(dav);
                }
                pointPoly.Update();

                //transform.RotateWXYZ(20, 0, 1, 0);
                transform.RotateY(90);
                var transformFilter = vtkTransformPolyDataFilter.New();
                transformFilter.SetTransform(transform);


                warp.SetInput(pointPoly);
                warp.SetScaleFactor(Scale);
                warp.Update();

                transformFilter.SetInputConnection(warp.GetOutputPort());

                mapper.SetInput(transformFilter.GetOutput());
                //mapper.SetInput(pointPoly);

                DataActor.SetMapper(mapper);
                DataActor.GetProperty().SetPointSize(PointSize);

                
            }
            RaisePropertyChanged(nameof(DataActor));
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
                cubeSource.SetZLength(Depth);
                //cubeSource.SetCenter(0, 0, Depth / 2);
                cubeSource.SetCenter(Width / 2, Height / 2, Depth / 2);

                cubeSource.Update();

                filter.SetInputConnection(cubeSource.GetOutputPort());

                mapper.SetInputConnection(filter.GetOutputPort());

                Data.SetMapper(mapper);

                CubeAxesActor.RotateX(90);
                CubeAxesActor.SetBounds(0, Width, 0, Height, 0, Depth);
                //CubeAxesActor.SetZAxisTickVisibility(0);
                CubeAxesActor.DrawZGridlinesOn();
                CubeAxesActor.SetZAxisTickVisibility(0);
                //Data.SetScale(Width, Height, Depth);
                CubeAxesActor.SetYAxisRange(0, Height);
                
                RaisePropertyChanged(nameof(Data));
            }
        }


    }
}
