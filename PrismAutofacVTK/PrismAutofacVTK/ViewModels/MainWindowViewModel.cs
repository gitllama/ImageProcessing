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

            LoadedCommand = new ReactiveCommand();
            LoadedCommand.Subscribe(_ =>
            {
                Messenger.Instance.GetEvent<PubSubEvent<Action<Kitware.VTK.vtkRenderWindow>>>().Publish((render) =>
                {
                    model.Set();
                    render.GetRenderers().GetFirstRenderer().AddActor(model.Data);
                });
            });

            model.ObserveProperty(x => x.Data).Subscribe(_=>
            {
                Messenger.Instance.GetEvent<PubSubEvent<Action<Kitware.VTK.vtkRenderWindow>>>().Publish((render) =>
                {
                    render.Render();
                });
            });
        }
    }
}
