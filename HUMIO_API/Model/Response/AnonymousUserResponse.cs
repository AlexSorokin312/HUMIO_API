namespace HUMIO_API.Requests
{
    public class AnonymousUserResponse
    {
        public int Id { get; set; }
        public string Country { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public string DeviceIdentifier { get; set; }
        public string Platform { get; set; }
    }
}