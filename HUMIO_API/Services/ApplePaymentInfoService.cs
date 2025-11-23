using HUMIO_API.DBContext;
using HUMIO_API.Requests;
using Microsoft.EntityFrameworkCore;

namespace HUMIO_API.Services
{
    public interface IApplePaymentInfoService
    {
        Task<ApplePaymentInfo> CreateAsync(ApplePaymentInfo info);
        Task<ApplePaymentInfo?> GetByDeviceIdAsync(string deviceId);
        Task<ApplePaymentInfo> UpdateAsync(string deviceId, ApplePaymentInfo updated);
        Task<bool> DeleteAsync(string deviceId);
    }

    public class ApplePaymentInfoService : IApplePaymentInfoService
    {
        private readonly AppDbContext _context;

        public ApplePaymentInfoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApplePaymentInfo> CreateAsync(ApplePaymentInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            var device = await _context.DeviceIdentifiers.FirstOrDefaultAsync(d => d.DeviceId == info.DeviceId);
            if (device == null)
            {
                throw new ArgumentException($"Device with id {info.DeviceId} not found");
            }

            var existing = await _context.ApplePaymentInfos.FindAsync(info.DeviceId);
            if (existing != null)
            {
                throw new InvalidOperationException("ApplePaymentInfo already exists for this device");
            }

            _context.ApplePaymentInfos.Add(info);
            await _context.SaveChangesAsync();
            return info;
        }

        public async Task<ApplePaymentInfo?> GetByDeviceIdAsync(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) return null;

            return await _context.ApplePaymentInfos
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.DeviceId == deviceId);
        }

        public async Task<ApplePaymentInfo> UpdateAsync(string deviceId, ApplePaymentInfo updated)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("DeviceId is required", nameof(deviceId));
            if (updated == null) throw new ArgumentNullException(nameof(updated));

            var existing = await _context.ApplePaymentInfos.FirstOrDefaultAsync(a => a.DeviceId == deviceId);
            if (existing == null)
            {
                throw new KeyNotFoundException("ApplePaymentInfo not found for device");
            }

            var device = await _context.DeviceIdentifiers.FirstOrDefaultAsync(d => d.DeviceId == deviceId);
            if (device == null)
            {
                throw new ArgumentException($"Device with id {deviceId} not found");
            }

            var countryToUse = string.IsNullOrWhiteSpace(updated.Country) ? device.Country : updated.Country;

            existing.Country = countryToUse;
            existing.PaymentCount = updated.PaymentCount;
            existing.SubscriptionEndDate = updated.SubscriptionEndDate;
            existing.Revenue = updated.Revenue;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(string deviceId)
        {
            var existing = await _context.ApplePaymentInfos.FirstOrDefaultAsync(a => a.DeviceId == deviceId);
            if (existing == null) return false;

            _context.ApplePaymentInfos.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
