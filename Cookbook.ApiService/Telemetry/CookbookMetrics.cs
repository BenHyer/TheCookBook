using System.Diagnostics.Metrics;

namespace Cookbook.ApiService.Telemetry;

public static class CookbookMetrics
{
    public const string MeterName = "Cookbook.CustomMetrics";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static Counter<long> BoardUploadAttempts { get; } = Meter.CreateCounter<long>(
        name: "cookbook.board_upload_attempts",
        unit: "{attempt}",
        description: "Total number of board creation attempts.");

    public static Counter<long> BoardUploadSuccesses { get; } = Meter.CreateCounter<long>(
        name: "cookbook.board_upload_successes",
        unit: "{upload}",
        description: "Total number of successfully created boards.");

    public static Counter<long> ApiErrors { get; } = Meter.CreateCounter<long>(
        name: "cookbook.api_errors",
        unit: "{error}",
        description: "Total API errors by version and status code.");

    public static Counter<long> DbErrors { get; } = Meter.CreateCounter<long>(
        name: "cookbook.db_errors",
        unit: "{error}",
        description: "Total database errors by operation type.");

    public static Histogram<double> ApiRequestDuration { get; } = Meter.CreateHistogram<double>(
        name: "cookbook.api_request_duration",
        unit: "ms",
        description: "Distribution of API request durations in milliseconds.");

    public static Histogram<long> RequestBodySize { get; } = Meter.CreateHistogram<long>(
        name: "cookbook.request_body_size",
        unit: "By",
        description: "Size of incoming HTTP request bodies in bytes.");

    public static Histogram<long> ResponseBodySize { get; } = Meter.CreateHistogram<long>(
        name: "cookbook.response_body_size",
        unit: "By",
        description: "Size of outgoing HTTP response bodies in bytes.");

    public static void TrackUploadAttempt() =>
        BoardUploadAttempts.Add(1);

    public static void TrackUploadSuccess() =>
        BoardUploadSuccesses.Add(1);

    public static void TrackApiError(string version, int statusCode) =>
        ApiErrors.Add(1,
            new KeyValuePair<string, object?>("version", version),
            new KeyValuePair<string, object?>("status_code", statusCode));

    public static void TrackDbError(string operation) =>
        DbErrors.Add(1, new KeyValuePair<string, object?>("operation", operation));
}
