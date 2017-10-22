using Autofac;
using Prism.Events;
using Prism.Mvvm;
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

            float[] buf = new float[10000];
            for (int y = 0; y < 100; y++)
                for (int x = 0; x < 100; x++)
                    buf[x + y * 100] = 100;
            for (int y = 20; y < 80; y++)
                for (int x = 20; x < 80; x++)
                    buf[x + y * 100] = 120;
            for (int y = 40; y < 80; y++)
                for (int x = 40; x < 60; x++)
                    buf[x + y * 100] = 100;

            LoadedCommand = new ReactiveCommand();
            LoadedCommand.Subscribe(_ =>
            {
                Messenger.Instance.GetEvent<PubSubEvent<Action<Kitware.VTK.vtkRenderWindow>>>().Publish((render) =>
                {

                    model.Init();
                    model.SetAxes();
                    model.SetData(buf);

                    model.CubeAxesActor.SetCamera(render.GetRenderers().GetFirstRenderer().GetActiveCamera());

                    //render.GetRenderers().GetFirstRenderer().AddActor(model.Data);
                    render.GetRenderers().GetFirstRenderer().AddActor(model.CubeAxesActor);
                    render.GetRenderers().GetFirstRenderer().AddActor(model.DataActor);

                    var camera = Kitware.VTK.vtkCamera.New();
                    camera.Azimuth(180);
                    //camera.SetViewUp(1, 1, 1);
                    //camera.SetPosition(0, 0, 0);
                    render.GetRenderers().GetFirstRenderer().SetActiveCamera(camera);
                });
            });

            model.ObserveProperty(x => x.Data).Subscribe(_=>
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