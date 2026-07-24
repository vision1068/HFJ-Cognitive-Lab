using GoldSignalAnalyzer.Application.Security;
using Serilog.Core;
using Serilog.Events;

namespace GoldSignalAnalyzer.Infrastructure.Logging;

/// <summary>
/// Global Serilog enricher (arch §5, NFR-7, A09). Replaces the value of any property whose NAME
/// matches the <see cref="SecretDenylist"/> with <c>***MASKED***</c>.
///
/// Matching is EXACT and case-insensitive (NOT substring) so non-secret operational values such
/// as Mt5TerminalPath / ServerName are never over-masked (gate QA-F12). It RECURSES into
/// StructureValue / SequenceValue / DictionaryValue so a destructured (<c>{@obj}</c>) secret is
/// masked at any depth (gate Aud-F4). Registered globally, so no sink can receive an un-masked value.
/// </summary>
public sealed class SensitiveDataMaskingEnricher : ILogEventEnricher
{
    public const string Mask = "***MASKED***";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Snapshot to avoid mutating while enumerating.
        foreach (var property in logEvent.Properties.ToArray())
        {
            var masked = MaskNamed(property.Key, property.Value);
            if (!ReferenceEquals(masked, property.Value))
            {
                logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, masked));
            }
        }
    }

    private static LogEventPropertyValue MaskNamed(string name, LogEventPropertyValue value)
        => SecretDenylist.IsSecret(name) ? new ScalarValue(Mask) : MaskValue(value);

    private static LogEventPropertyValue MaskValue(LogEventPropertyValue value)
    {
        switch (value)
        {
            case StructureValue structure:
            {
                var props = new List<LogEventProperty>(structure.Properties.Count);
                var changed = false;
                foreach (var p in structure.Properties)
                {
                    var nv = MaskNamed(p.Name, p.Value);
                    if (!ReferenceEquals(nv, p.Value)) changed = true;
                    props.Add(new LogEventProperty(p.Name, nv));
                }
                return changed ? new StructureValue(props, structure.TypeTag) : structure;
            }
            case SequenceValue sequence:
            {
                var elements = new List<LogEventPropertyValue>(sequence.Elements.Count);
                var changed = false;
                foreach (var e in sequence.Elements)
                {
                    var nv = MaskValue(e);
                    if (!ReferenceEquals(nv, e)) changed = true;
                    elements.Add(nv);
                }
                return changed ? new SequenceValue(elements) : sequence;
            }
            case DictionaryValue dictionary:
            {
                var pairs = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>(dictionary.Elements.Count);
                var changed = false;
                foreach (var kv in dictionary.Elements)
                {
                    var keyName = kv.Key.Value?.ToString();
                    var nv = keyName is not null && SecretDenylist.IsSecret(keyName)
                        ? new ScalarValue(Mask)
                        : MaskValue(kv.Value);
                    if (!ReferenceEquals(nv, kv.Value)) changed = true;
                    pairs.Add(new KeyValuePair<ScalarValue, LogEventPropertyValue>(kv.Key, nv));
                }
                return changed ? new DictionaryValue(pairs) : dictionary;
            }
            default:
                return value;
        }
    }
}
