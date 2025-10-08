using AutoMapper;
using HUMIO_API.Requests;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore())
            .ForMember(dest => dest.UserData, opt => opt.MapFrom(src => src.UserData))
            .ForMember(dest => dest.Devices, opt => opt.MapFrom(src =>
                src.UserDevices != null ? src.UserDevices.Select(ud => ud.DeviceIdentifier) : new List<DeviceIdentifier>()
            ));

        CreateMap<UserData, UserDataDto>();
        CreateMap<DeviceIdentifier, DeviceIdentifierDto>();

        CreateMap<DeviceIdentifier, DeviceIdentifierResponce>();
    }
}
