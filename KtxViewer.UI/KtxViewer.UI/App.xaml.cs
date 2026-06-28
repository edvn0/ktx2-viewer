using KtxViewer.Application;
using KtxViewer.Core;
using KtxViewer.Infrastructure;
using KtxViewer.UI.ViewModels;
using System.IO;
using System.Linq;
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

        // Handle file association - open file(s) from command line
        if (e.Args.Length > 0)
        {
            var filePaths = e.Args.Where(File.Exists).ToArray();
            if (filePaths.Length > 0)
            {
                _ = viewModel.LoadFilesAsync(filePaths);
            }
        }
    }
}
