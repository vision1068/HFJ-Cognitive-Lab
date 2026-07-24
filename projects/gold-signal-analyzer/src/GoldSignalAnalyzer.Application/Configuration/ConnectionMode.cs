namespace GoldSignalAnalyzer.Application.Configuration;

/// <summary>MT5 connection mode (spec §5). Both modes are read-only; neither requests a trading password.</summary>
public enum ConnectionMode
{
    /// <summary>Attach to an already-running terminal session.</summary>
    ModeA,

    /// <summary>Configured, read-only connection.</summary>
    ModeB
}
