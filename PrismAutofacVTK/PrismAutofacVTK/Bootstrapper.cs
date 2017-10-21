using Autofac;
using Prism.Autofac;
using PrismAutofacVTK.Views;
using System.Windows;

namespace PrismAutofacVTK
{
    class Bootstrapper : AutofacBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            Application.Current.MainWindow.Show();
        }
    }
}
