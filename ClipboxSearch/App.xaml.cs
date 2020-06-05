using System.Windows;

namespace ClipboxSearch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private SearchHandler _searchHandler;
        protected override void OnStartup(StartupEventArgs e)
        {
            _searchHandler = new SearchHandler();
            _searchHandler.StartUp();

            Application.Current.Dispatcher.ShutdownStarted += (s, args)
                => _searchHandler.ShutDown();
        }

    }
}
