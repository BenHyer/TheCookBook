using System.Text.Json;

var command = args[0];
var currentUris = JsonSerializer.Deserialize<List<string>>(args[1]) ?? [];

List<string> result = command switch
{
    "merge" => currentUris.Concat(args[2..]).Distinct().Order().ToList(),
    "filter" => currentUris.Where(u => !u.Contains($"pr-{args[2]}.")).ToList(),
    _ => throw new ArgumentException($"Unknown command: {command}")
};

Console.WriteLine(JsonSerializer.Serialize(new { web = new { redirectUris = result } }));
