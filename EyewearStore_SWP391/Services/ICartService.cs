using System.Threading.Tasks;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Services
{
    public interface ICartService
    {
        Task<Cart?> GetCartByUserIdAsync(int userId);
        Task AddToCartAsync(int userId, string productType, int productId, int quantity = 1, string? tempPrescriptionJson = null);
        Task UpdateQuantityAsync(int cartItemId, int newQuantity);
        Task UpdateItemPrescriptionAsync(int cartItemId, string? tempPrescriptionJson);
        Task RemoveItemAsync(int cartItemId);
        Task ClearCartAsync(int userId);
        Task<decimal> CalculateCartTotalAsync(int userId);
    }
}