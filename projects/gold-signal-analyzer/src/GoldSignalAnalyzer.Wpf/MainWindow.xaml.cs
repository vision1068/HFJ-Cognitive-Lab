using System.Windows;

namespace GoldSignalAnalyzer.Wpf;

/// <summary>
/// Dashboard shell (FR-26). Thin: it only hosts the bound view-models set by the
/// composition root. No logic, no trading surface.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
