using System.Text.Json.Serialization;

public class UserRecord
{
    [JsonPropertyName("country")]
    public string Country { get; set; }

    [JsonPropertyName("deviceIdentifiers")]
    public List<string> DeviceIdentifiers { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("platform")]
    public string Platform { get; set; }

}
