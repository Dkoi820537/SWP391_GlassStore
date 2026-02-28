// Services/ICartService.cs
using System.Threading.Tasks;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Services
{
    public interface ICartService
    {
        Task<Cart?> GetCartByUserIdAsync(int userId);

        /// <summary>Đơn thường — giữ nguyên signature cũ</summary>
        Task AddToCartAsync(int userId, int productId, int quantity = 1,
            int? serviceId = null, string? tempPrescriptionJson = null,
            int? prescriptionId = null);

        /// <summary>
        /// Đơn gia công — Frame + Lens + Service.
        /// LensProductId được encode vào TempPrescriptionJson dưới key "lensProductId".
        /// </summary>
        Task AddServiceOrderAsync(int userId, int frameProductId, int lensProductId,
            int serviceId, int quantity = 1);

        Task UpdateQuantityAsync(int cartItemId, int newQuantity);
        Task UpdateItemPrescriptionAsync(int cartItemId, string? tempPrescriptionJson);
        Task UpdateItemPrescriptionByIdAsync(int cartItemId, int? prescriptionId, int userId);
        Task RemoveItemAsync(int cartItemId);
        Task ClearCartAsync(int userId);
        Task<decimal> CalculateCartTotalAsync(int userId);
        Task<(decimal SubtotalBase, decimal PrescriptionFeesTotal, decimal GrandTotal)> GetCartTotalsBreakdownAsync(int userId);
    }
}