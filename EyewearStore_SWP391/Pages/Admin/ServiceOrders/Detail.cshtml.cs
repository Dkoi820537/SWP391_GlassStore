using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static EyewearStore_SWP391.Pages.Admin.ServiceOrders.IndexModel;

namespace EyewearStore_SWP391.Pages.Admin.ServiceOrders
{
    [Authorize(Roles = "admin,manager")]
    public class DetailModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public DetailModel(EyewearStoreContext context) => _context = context;

        // ── Route ─────────────────────────────────────────────────────────────
        [BindProperty(SupportsGet = true)] public int OrderItemId { get; set; }

        // ── Loaded data ───────────────────────────────────────────────────────
        public Order? Order { get; set; }
        public OrderItem? Item { get; set; }
        public ServiceSnapshot? Snap { get; set; }

        // ── Form binds ────────────────────────────────────────────────────────
        [BindProperty] public string NewStatus { get; set; } = "Pending";
        [BindProperty] public string? AssignedTo { get; set; }
        [BindProperty] public string? InternalNote { get; set; }

        public static readonly string[] Statuses = { "Pending", "Processing", "Ready", "Done", "Cancelled" };

        public async Task<IActionResult> OnGetAsync()
        {
            Item = await _context.OrderItems
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.User)
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.Address)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == OrderItemId);

            if (Item == null) return NotFound();
            Order = Item.Order;

            if (!string.IsNullOrEmpty(Item.SnapshotJson))
            {
                try
                {
                    Snap = JsonSerializer.Deserialize<ServiceSnapshot>(Item.SnapshotJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch { Snap = new ServiceSnapshot(); }
            }

            if (Snap?.IsServiceOrder != true) return NotFound(); // not a service order

            NewStatus = Snap.ServiceStatus ?? "Pending";
            AssignedTo = Snap.AssignedTo;
            InternalNote = Snap.InternalNote;

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            Item = await _context.OrderItems
                .FirstOrDefaultAsync(oi => oi.OrderItemId == OrderItemId);

            if (Item == null) return NotFound();

            // Parse existing snapshot
            ServiceSnapshot snap;
            try
            {
                snap = string.IsNullOrEmpty(Item.SnapshotJson)
                    ? new ServiceSnapshot()
                    : JsonSerializer.Deserialize<ServiceSnapshot>(Item.SnapshotJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? new ServiceSnapshot();
            }
            catch { snap = new ServiceSnapshot(); }

            // Patch only the service-management fields
            snap.ServiceStatus = NewStatus;
            snap.AssignedTo = AssignedTo?.Trim();
            snap.InternalNote = InternalNote?.Trim();

            Item.SnapshotJson = JsonSerializer.Serialize(snap,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Service order #{Item.OrderItemId} updated to {NewStatus}.";
            return RedirectToPage(new { orderItemId = OrderItemId });
        }
    }
}