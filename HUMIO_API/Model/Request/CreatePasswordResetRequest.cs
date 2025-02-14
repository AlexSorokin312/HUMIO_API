namespace HUMIO_API.Model.Request
{
    public class CreatePasswordResetRequest
    {
        public string Email { get; set; }
        public string ResetCode { get; set; }
    }
}
