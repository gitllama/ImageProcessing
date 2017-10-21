using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;

namespace PrismAutofacVTK.Models
{
    public class Model : BindableBase
    {

        public vtkActor Data = vtkActor.New();

        private int _width = 1;
        public int Width
        {
            get { return _width; }
            set { SetProperty(ref _width, value, () => Set()); }
        }

        private int _height = 1;
        public int Height
        {
            get { return _height; }
            set { SetProperty(ref _height, value, () => Set()); }
        }

        private int _depth = 1;
        public int Depth
        {
            get { return _depth; }
            set { SetProperty(ref _depth, value, () => Set()); }
        }

        public void Set()
        {
            // Create a cube.  
            using (var cubeSource = vtkCubeSource.New())
            using (var mapper = vtkPolyDataMapper.New())
            {
                cubeSource.SetXLength(Width);
                cubeSource.SetYLength(Height);
                cubeSource.SetZLength(Depth);

                mapper.SetInputConnection(cubeSource.GetOutputPort());
                Data.SetMapper(mapper);
                RaisePropertyChanged("Data");
            }
        }

        public void Render(string com, vtkProp actor)
        {

            //using (var renderer = RenderControl.RenderWindow.GetRenderers().GetFirstRenderer())
            //{
            //    switch (com)
            //    {
            //        case "add":
            //            renderer.AddActor(actor);
            //            //RenderControl.RenderWindow.Render();
            //            break;
            //        case "remove":
            //            renderer.RemoveActor(actor);
            //            break;
            //        case "render":
            //            RenderControl.RenderWindow.Render();
            //            break;
            //        default:
            //            break;
            //    }
            //}
        }
    }
}
