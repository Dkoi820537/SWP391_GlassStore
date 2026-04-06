using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Controllers
{
    /// <summary>
    /// Secured API for financial/revenue data.
    /// Locked to the "admin" role — any other role receives 403 Forbidden.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class FinancialController : ControllerBase
    {
        private readonly EyewearStoreContext _db;

        public FinancialController(EyewearStoreContext db) => _db = db;

        /// <summary>
        /// Returns a financial summary (revenue, order counts).
        /// Only accessible to users with the "admin" role.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(FinancialSummaryDto), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetSummary()
        {
            var today = DateTime.Today;

            var orders = _db.Orders.AsQueryable();

            var totalOrders = await orders.CountAsync();
            var ordersToday = await orders.CountAsync(o => o.CreatedAt.Date == today);

            var revenue = await orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var revenueToday = await orders
                .Where(o => o.CreatedAt.Date == today && o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var completedRevenue = await orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            return Ok(new FinancialSummaryDto
            {
                TotalOrders = totalOrders,
                OrdersToday = ordersToday,
                Revenue = revenue,
                RevenueToday = revenueToday,
                CompletedRevenue = completedRevenue
            });
        }
    }

    /// <summary>
    /// Data-transfer object for financial summary endpoint.
    /// </summary>
    public class FinancialSummaryDto
    {
        public int TotalOrders { get; set; }
        public int OrdersToday { get; set; }
        public decimal Revenue { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal CompletedRevenue { get; set; }
    }
}
