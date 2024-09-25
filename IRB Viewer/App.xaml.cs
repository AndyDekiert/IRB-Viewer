using Microsoft.Maui.Controls;

namespace IRB_Viewer;

public partial class App : Application {
    public App() {
        InitializeComponent();

        MainPage = new AppShell();
    }
}