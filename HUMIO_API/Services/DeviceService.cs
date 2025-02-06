using HUMIO_API.DBContext;
using HUMIO_API.Requests;
using Microsoft.EntityFrameworkCore;

namespace HUMIO_API.Services
{
    public interface IDeviceService
    {
        /// <summary>
        /// Возвращает запись DeviceIdentifier для устройства, если она существует, иначе создаёт новую.
        /// </summary>
        /// <param name="request">Данные устройства.</param>
        /// <returns>Объект DeviceIdentifier.</returns>
        Task<DeviceIdentifier> GetOrCreateDeviceAsync(DeviceRequest request);

        /// <summary>
        /// Удаляет запись DeviceIdentifier, если она не связана с зарегистрированными пользователями.
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства (DeviceId).</param>
        /// <returns>true, если удаление выполнено; false, если запись связана с зарегистрированными пользователями или не найдена.</returns>
        Task<bool> DeleteAnonymousDeviceAsync(string deviceId);
    }

        public class DeviceService : IDeviceService
        {
            private readonly AppDbContext _context;

            public DeviceService(AppDbContext context)
            {
                _context = context;
            }

            public async Task<DeviceIdentifier> GetOrCreateDeviceAsync(DeviceRequest request)
            {
                // Проверяем, существует ли уже запись с таким DeviceId.
                var existingDevice = await _context.DeviceIdentifiers
                    .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceIdentifier);

                if (existingDevice != null)
                {
                    // Если запись уже есть, просто возвращаем её.
                    return existingDevice;
                }

                // Если записи нет, создаём новую.
                var newDevice = new DeviceIdentifier
                {
                    DeviceId = request.DeviceIdentifier,
                    TrialEndDate = DateTime.UtcNow.AddDays(3)
                };

                _context.DeviceIdentifiers.Add(newDevice);
                await _context.SaveChangesAsync();

                return newDevice;
            }

            public async Task<bool> DeleteAnonymousDeviceAsync(string deviceId)
            {
                // Находим запись по DeviceId
                var device = await _context.DeviceIdentifiers
                    .Include(d => d.UserDevices)
                    .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

                if (device == null)
                {
                    // Запись не найдена.
                    return false;
                }

                // Если устройство связано с каким-либо зарегистрированным пользователем, то удалять нельзя.
                if (device.UserDevices != null && device.UserDevices.Any())
                {
                    return false;
                }

                _context.DeviceIdentifiers.Remove(device);
                await _context.SaveChangesAsync();
                return true;
            }
        }
}
