using KtxViewer.Application;
using KtxViewer.Core;
using KtxViewer.Infrastructure;
using KtxViewer.UI.ViewModels;
using System.IO;
using System.Windows;

namespace KtxViewer.UI;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IKtxLoader loader = new CompositeKtxLoader();
        var useCase = new LoadImageUseCase(loader);
        var viewModel = new MainViewModel(useCase);
        var mainWindow = new MainWindow(viewModel);

        mainWindow.Show();

        // Handle file association - open file from command line
        if (e.Args.Length > 0)
        {
            var filePath = e.Args[0];
            if (File.Exists(filePath))
            {
                _ = viewModel.LoadFileAsync(filePath);
            }
        }
    }
}
