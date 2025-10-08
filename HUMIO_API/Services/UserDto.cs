public class UserDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
    public List<string> Roles { get; set; }

    public UserDataDto UserData { get; set; }
    public List<DeviceIdentifierDto> Devices { get; set; }
}

public class UserDataDto
{
    public string Country { get; set; }
    public string Platform { get; set; }
    public int PaymentCount { get; set; }
    public decimal Revenue { get; set; }
    public string UserName { get; set; }
    public DateTime TrialEndDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
}

public class DeviceIdentifierDto
{
    public int Id { get; set; }
    public string DeviceId { get; set; }
    public DateTime? TrialEndDate { get; set; }
}

