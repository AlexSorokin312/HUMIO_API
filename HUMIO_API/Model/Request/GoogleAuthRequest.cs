namespace Humio.Requests
{
    public class GoogleTokenRequest
    {
        public string AccessToken { get; set; }
        public string Country { get; set; }
        public string DeviceIdentifier { get; set; }
        public string Platform { get; set; }
    }
}