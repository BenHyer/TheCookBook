using System.Diagnostics.Metrics;

namespace CookbookMauiBlazor.Web.Telemetry;

public static class CookbookWebMetrics
{
    public const string MeterName = "Cookbook.WebMetrics";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static Counter<long> LoginAttempts { get; } = Meter.CreateCounter<long>(
        name: "cookbook.login_attempts",
        unit: "{attempt}",
        description: "Total login attempts broken down by result.");

    public static void TrackLoginSuccess() =>
        LoginAttempts.Add(1, new KeyValuePair<string, object?>("result", "success"));

    public static void TrackLoginFailure() =>
        LoginAttempts.Add(1, new KeyValuePair<string, object?>("result", "failure"));
}
