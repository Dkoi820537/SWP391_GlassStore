using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EyewearStore_SWP391.Models
{
    public partial class EyeExamAppointment
    {
        [Key]
        public int AppointmentId { get; set; }

        public int? UserId { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = null!;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = null!;

        [MaxLength(200)]
        public string? Email { get; set; }

        [Required]
        public DateOnly AppointmentDate { get; set; }

        [Required, MaxLength(10)]
        public string TimeSlot { get; set; } = null!;   // e.g. "09:00"

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Pending | Confirmed | Completed | Cancelled
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}