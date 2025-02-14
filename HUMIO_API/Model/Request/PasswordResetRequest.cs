namespace HUMIO_API.Model.Request
{
    public class PasswordResetRequest
    {
        public string Email { get; set; }
        public string ResetCode { get; set; }
        public string NewPassword { get; set; }
    }
}
