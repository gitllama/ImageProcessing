using Prism.Events;
using System;
using System.Windows;

namespace PrismAutofacVTK.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Messenger : EventAggregator
        {
            private static Messenger _instance;
            public static Messenger Instance { get => _instance ?? (_instance = new Messenger()); }
        }

        public MainWindow()
        {
            InitializeComponent();
            Messenger.Instance.GetEvent<PubSubEvent<Action<Kitware.VTK.vtkRenderWindow>>>().Subscribe(m => 
            {
                m(RenderControl.RenderWindow);
            });
        }
    }
}
