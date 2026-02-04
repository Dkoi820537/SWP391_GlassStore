using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Pages.Staff.Orders
{
    [Authorize(Roles = "staff,admin,Administrator")]
    public class DetailsModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public DetailsModel(EyewearStoreContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int id { get; set; }

        public Order? Order { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.Address)
                .Include(o => o.Shipments).ThenInclude(s => s.ShipmentStatusHistories)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (Order == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdatePrescriptionWorkflowAsync(int id, string WorkflowStep)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            if (!order.OrderItems.Any(oi => oi.PrescriptionId != null))
            {
                TempData["Error"] = "This is not a prescription order.";
                return RedirectToPage(new { id });
            }

            var validSteps = new[]
            {
                "Confirmed",
                "Processing - Lens Ordered",
                "Processing - Lens Received",
                "Processing - Fitting",
                "Processing - QC",
                "Processing - Packed"
            };

            if (!validSteps.Contains(WorkflowStep))
            {
                TempData["Error"] = "Invalid workflow step.";
                return RedirectToPage(new { id });
            }

            order.Status = WorkflowStep;
            // Không ghi UpdatedAt (không thay DB)

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Workflow updated to: {WorkflowStep}";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string NewStatus)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound();

            if (string.IsNullOrWhiteSpace(NewStatus))
            {
                TempData["Error"] = "Status is required!";
                return RedirectToPage(new { id });
            }

            var valids = new[] { "Confirmed", "Processing", "Shipped", "Delivered", "Completed", "Cancelled" };
            if (!valids.Contains(NewStatus))
            {
                TempData["Error"] = "Invalid status!";
                return RedirectToPage(new { id });
            }

            var prev = order.Status;
            order.Status = NewStatus;

            if (NewStatus == "Cancelled" && prev != "Cancelled")
            {
                await RestoreInventoryAsync(order);
            }

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order status updated to: {NewStatus}";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostCreateShipmentAsync(int id, string TrackingNumber, string Carrier)
        {
            var order = await _context.Orders.Include(o => o.Shipments).FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound();

            if (string.IsNullOrWhiteSpace(TrackingNumber))
            {
                TempData["Error"] = "Tracking number is required!";
                return RedirectToPage(new { id });
            }
            if (string.IsNullOrWhiteSpace(Carrier))
            {
                TempData["Error"] = "Carrier is required!";
                return RedirectToPage(new { id });
            }

            var existing = order.Shipments?.FirstOrDefault();
            if (existing == null)
            {
                var shipment = new Shipment
                {
                    OrderId = order.OrderId,
                    TrackingNumber = TrackingNumber.Trim(),
                    Carrier = Carrier.Trim(),
                    Status = "Shipped",
                    ShippedAt = DateTime.UtcNow
                };
                _context.Shipments.Add(shipment);
                await _context.SaveChangesAsync();

                _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
                {
                    ShipmentId = shipment.ShipmentId,
                    Status = "Shipped",
                    CreatedAt = DateTime.UtcNow
                });

                TempData["Success"] = "Shipment created successfully!";
            }
            else
            {
                existing.TrackingNumber = TrackingNumber.Trim();
                existing.Carrier = Carrier.Trim();
                existing.Status = "Shipped";
                existing.ShippedAt = DateTime.UtcNow;

                _context.Shipments.Update(existing);
                _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
                {
                    ShipmentId = existing.ShipmentId,
                    Status = "Shipped",
                    CreatedAt = DateTime.UtcNow
                });

                TempData["Success"] = "Shipment updated successfully!";
            }

            if (order.Status != "Shipped")
            {
                order.Status = "Shipped";
                _context.Orders.Update(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        // Add note: store into TempData (temporary; not persisted)
        public async Task<IActionResult> OnPostAddNoteAsync(int id, string Note)
        {
            if (string.IsNullOrWhiteSpace(Note))
            {
                TempData["Error"] = "Note cannot be empty!";
                return RedirectToPage(new { id });
            }

            var key = $"InternalNotes_{id}";
            var existing = TempData.ContainsKey(key) ? TempData[key] as string ?? string.Empty : string.Empty;
            var userName = User.Identity?.Name ?? "Staff";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
            var newNote = $"{existing}[{timestamp}] {userName}:\n{Note.Trim()}\n\n";

            // Put back to TempData (survives one redirect)
            TempData[key] = newNote;

            TempData["Success"] = "Note added (temporary).";
            return RedirectToPage(new { id });
        }

        private async Task RestoreInventoryAsync(Order order)
        {
            var orderItems = await _context.OrderItems.Include(oi => oi.Product)
                .Where(oi => oi.OrderId == order.OrderId).ToListAsync();

            foreach (var item in orderItems)
            {
                if (item.Product != null)
                {
                    item.Product.InventoryQty = (item.Product.InventoryQty ?? 0) + item.Quantity;
                    _context.Products.Update(item.Product);
                }
            }

            await _context.SaveChangesAsync();
        }

        // Validate prescription values (use PrescriptionProfile fields that exist)
        private bool ValidatePrescriptionValues(PrescriptionProfile prescription)
        {
            if (prescription == null) return false;

            // SPH and CYL fields are decimal? — check value ranges if present
            if (prescription.RightSph.HasValue && (prescription.RightSph < -20.00m || prescription.RightSph > 20.00m))
                return false;
            if (prescription.LeftSph.HasValue && (prescription.LeftSph < -20.00m || prescription.LeftSph > 20.00m))
                return false;
            if (prescription.RightCyl.HasValue && (prescription.RightCyl < -6.00m || prescription.RightCyl > 6.00m))
                return false;
            if (prescription.LeftCyl.HasValue && (prescription.LeftCyl < -6.00m || prescription.LeftCyl > 6.00m))
                return false;
            if (prescription.RightAxis.HasValue && (prescription.RightAxis < 0 || prescription.RightAxis > 180))
                return false;
            if (prescription.LeftAxis.HasValue && (prescription.LeftAxis < 0 || prescription.LeftAxis > 180))
                return false;

            // PD / ADD fields are not present in your PrescriptionProfile model, so we skip those checks.

            return true;
        }
    }
}