using System.Windows;

namespace GoldSignalAnalyzer.Desktop;

/// <summary>
/// Minimal shell window so the whole solution builds and runs green. Frontend replaces the body
/// with the navigation rail + ContentControl for the 17 screens (arch §7).
/// </summary>
public partial class ShellWindow : Window
{
    public ShellWindow() => InitializeComponent();
}
