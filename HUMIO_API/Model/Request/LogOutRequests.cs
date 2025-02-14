namespace HUMIO_API.Model.Request
{
    public class LogoutRequest
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}
