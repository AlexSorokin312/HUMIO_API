using System.ComponentModel.DataAnnotations;

public class DeviceRequest
{
    [Required]
    public string DeviceIdentifier { get; set; }

    public string Country { get; set; }

    public string Platform { get; set; }

}
