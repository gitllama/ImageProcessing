using Autofac;
using Prism.Events;
using Prism.Mvvm;
using PrismAutofacVTK.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using static PrismAutofacVTK.Views.MainWindow;

namespace PrismAutofacVTK.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Autofac Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public ReactiveProperty<object> obj { get; private set; }
        public ReactiveCommand<object> LoadedCommand { get; private set; }

        public ReactiveCommand ValidateCommand { get; private set; }

        public MainWindowViewModel()
        {
            var model = App.modelcontainer.Resolve<Models.Model>();

            obj = new ReactiveProperty<object>((object)model);


            LoadedCommand = new ReactiveCommand();
            LoadedCommand.Subscribe(_ =>
            {
                Messenger.Instance.GetEvent<PubSubEvent<Action<Kitware.VTK.vtkRenderWindow>>>().Publish((render) =>
                {
                    using (var _render = render.GetRenderers().GetFirstRenderer())
                    {
                        model.Init();
                        _render.AddActor(model.AreaActor);
                        render.Render();

                        //model.SetData(buf);
                        //model.ReadRawInt32("000.bin", (i) =>1 -  (float)(i / 100000.0));
                        model.ReadRawPGM("test.pgm", (i) => 1-(float)(i / 255));

                        _render.AddActor(model.DataActor);

                    }

                    //model.SetAxes();

                    //model.CubeAxesActor.SetCamera(render.GetRenderers().GetFirstRenderer().GetActiveCamera());


                    //render.GetRenderers().GetFirstRenderer().AddActor(model.CubeAxesActor);
                    //render.GetRenderers().GetFirstRenderer().AddActor(model.DataActor);


                    //var camera = Kitware.VTK.vtkCamera.New();
                    //camera.Azimuth(180);
                    //camera.SetViewUp(1, 1, 1);
                    //camera.SetPosition(0, 0, 0);
                    //render.GetRenderers().GetFirstRenderer().SetActiveCamera(camera);
                });
            });

            model.ObserveProperty(x => x.AreaActor).Subscribe(_=>
            {
                Messenger.Instance.GetEvent<PubSubEvent<Action<Kitware.VTK.vtkRenderWindow>>>().Publish((render) =>
                {
                    render.Render();
                });
            });
            model.ObserveProperty(x => x.DataActor).Subscribe(_ =>
            {
                Messenger.Instance.GetEvent<PubSubEvent<Action<Kitware.VTK.vtkRenderWindow>>>().Publish((render) =>
                {
                    render.Render();
                });
            });
        }
    }
}

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



