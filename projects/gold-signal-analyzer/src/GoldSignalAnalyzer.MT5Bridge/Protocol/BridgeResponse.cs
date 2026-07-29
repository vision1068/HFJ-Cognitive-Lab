using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoldSignalAnalyzer.MT5Bridge.Protocol;

/// <summary>
/// Discriminated reply from the bridge (commands.dispatch): either <c>{ok:true, data:...}</c> or
/// <c>{ok:false, error:&lt;code&gt;, detail?:...}</c>. <see cref="Data"/> is a <see cref="JsonElement"/>
/// because the payload shape varies by command (object for <c>symbol_info</c>, array for
/// <c>symbols_get</c>/<c>copy_rates_*</c>). A deserialized <see cref="JsonElement"/> member is a
/// standalone clone (independent of any <c>JsonDocument</c> scope), so it is safe to hold and
/// enumerate after the HTTP response is disposed.
///
/// No property name matches <c>SecretDenylist</c>.
/// </summary>
public sealed class BridgeResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }
}
