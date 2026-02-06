using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Pages.Support.Orders
{
    [Authorize(Roles = "support,sales,admin")]
    public class CustomerHistoryModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public CustomerHistoryModel(EyewearStoreContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int userId { get; set; }

        public string CustomerName { get; set; } = "";
        public List<OrderListItem> Orders { get; set; } = new();

        public class OrderListItem
        {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public int ItemCount { get; set; }
        }

        public async Task OnGetAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                CustomerName = "Unknown user";
                Orders = new List<OrderListItem>();
                return;
            }

            CustomerName = user.FullName ?? user.Email ?? "Customer";

            Orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderListItem
                {
                    OrderId = o.OrderId,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.OrderItems.Count()
                })
                .ToListAsync();
        }
    }
}