using System.Windows;
using GoldSignalAnalyzer.Presentation.Setup;

namespace GoldSignalAnalyzer.Wpf;

/// <summary>
/// First-run setup wizard gate (FR-28). Thin shell: it binds a
/// <see cref="SetupWizardViewModel"/> and closes with a positive result only after the
/// view-model raises <see cref="SetupWizardViewModel.Completed"/> (i.e. a profile was
/// saved). Contains no logic — all step/validation/persistence lives in the net8.0
/// Presentation view-model.
/// </summary>
public partial class SetupWizardWindow : Window
{
    private readonly SetupWizardViewModel _vm;

    public SetupWizardWindow(SetupWizardViewModel vm)
    {
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        InitializeComponent();
        DataContext = _vm;
        _vm.Completed += OnCompleted;
    }

    private void OnCompleted(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
