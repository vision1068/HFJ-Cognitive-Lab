using System.Threading;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Runs an action on a dedicated STA thread and re-throws any failure on the caller. WPF visuals
/// (instantiating the placeholder Views for gate R3) must be created on an STA thread.
/// </summary>
internal static class StaExecutor
{
    public static void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                // A WPF Application registers the pack:// scheme and gives UserControl
                // InitializeComponent a resource context. One per process is enough.
                if (System.Windows.Application.Current is null)
                    _ = new System.Windows.Application();

                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        if (captured is not null)
            throw new InvalidOperationException("STA action failed: " + captured.Message, captured);
    }
}
