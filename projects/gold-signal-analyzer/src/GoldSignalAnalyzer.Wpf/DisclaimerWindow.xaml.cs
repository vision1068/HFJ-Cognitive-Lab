using System.Windows;
using GoldSignalAnalyzer.Presentation;

namespace GoldSignalAnalyzer.Wpf;

/// <summary>
/// First-run disclaimer gate (FR-35). Thin shell: it binds a
/// <see cref="DisclaimerViewModel"/> and closes with a positive result only after
/// the view-model raises <see cref="DisclaimerViewModel.Acknowledged"/>.
/// </summary>
public partial class DisclaimerWindow : Window
{
    private readonly DisclaimerViewModel _vm;

    public DisclaimerWindow(DisclaimerViewModel vm)
    {
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        InitializeComponent();
        DataContext = _vm;
        _vm.Acknowledged += OnAcknowledged;
    }

    private void OnAcknowledged(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
