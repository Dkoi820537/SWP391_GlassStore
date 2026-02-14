using System.Threading.Tasks;
using EyewearStore_SWP391.Models;

namespace EyewearStore_SWP391.Services;

public interface ICartService
{
    Task<Cart?> GetCartByUserIdAsync(int userId);
    Task AddToCartAsync(int userId, int productId, int quantity = 1, int? serviceId = null, string? tempPrescriptionJson = null, int? prescriptionId = null);
    Task UpdateQuantityAsync(int cartItemId, int newQuantity);
    Task UpdateItemPrescriptionAsync(int cartItemId, string? tempPrescriptionJson);
    Task UpdateItemPrescriptionByIdAsync(int cartItemId, int? prescriptionId, int userId);
    Task RemoveItemAsync(int cartItemId);
    Task ClearCartAsync(int userId);
    Task<decimal> CalculateCartTotalAsync(int userId);
    Task<(decimal SubtotalBase, decimal PrescriptionFeesTotal, decimal GrandTotal)> GetCartTotalsBreakdownAsync(int userId);
}