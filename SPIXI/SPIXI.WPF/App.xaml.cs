using IXICore.Meta;
using System;
using System.Threading;
using System.Windows;

namespace SPIXI.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            IxianHandler.shutdown();
            while (IxianHandler.status != NodeStatus.stopped)
            {
                Thread.Sleep(10);
            }
        }
    }
}
