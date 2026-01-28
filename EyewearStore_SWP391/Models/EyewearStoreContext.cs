using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Models;

public partial class EyewearStoreContext : DbContext
{
    public EyewearStoreContext()
    {
    }

    public EyewearStoreContext(DbContextOptions<EyewearStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Bundle> Bundles { get; set; }

    public virtual DbSet<BundleItem> BundleItems { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Frame> Frames { get; set; }

    public virtual DbSet<Lense> Lenses { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<PrescriptionProfile> PrescriptionProfiles { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<Return> Returns { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<ShipmentStatusHistory> ShipmentStatusHistories { get; set; }

    public virtual DbSet<StockNotification> StockNotifications { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=EyewearStore;Integrated Security=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__Addresse__26A111AD6A911C52");

            entity.HasIndex(e => new { e.UserId, e.IsDefault }, "IX_Addresses_IsDefault");

            entity.HasIndex(e => e.UserId, "IX_Addresses_UserId");

            entity.Property(e => e.AddressId).HasColumnName("addressId");
            entity.Property(e => e.AddressLine)
                .HasMaxLength(500)
                .HasColumnName("addressLine");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.IsDefault).HasColumnName("isDefault");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.ReceiverName)
                .HasMaxLength(100)
                .HasColumnName("receiverName");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Addresses_Users");
        });

        modelBuilder.Entity<Bundle>(entity =>
        {
            entity.HasKey(e => e.BundleId).HasName("PK__Bundles__2D34FD3178C6B74E");

            entity.HasIndex(e => e.CreatedBy, "IX_Bundles_CreatedBy");

            entity.HasIndex(e => e.IsActive, "IX_Bundles_IsActive");

            entity.Property(e => e.BundleId).HasColumnName("bundleId");
            entity.Property(e => e.BundlePrice)
                .HasColumnType("decimal(12, 0)")
                .HasColumnName("bundlePrice");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.CreatedBy).HasColumnName("createdBy");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("isActive");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Bundles)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Bundles_CreatedBy");
        });

        modelBuilder.Entity<BundleItem>(entity =>
        {
            entity.HasKey(e => e.BundleItemId).HasName("PK__BundleIt__123F8BA1EEEC8337");

            entity.HasIndex(e => e.BundleId, "IX_BundleItems_BundleId");

            entity.HasIndex(e => new { e.ItemType, e.ItemId }, "IX_BundleItems_ItemType");

            entity.Property(e => e.BundleItemId).HasColumnName("bundleItemId");
            entity.Property(e => e.BundleId).HasColumnName("bundleId");
            entity.Property(e => e.IsRequired)
                .HasDefaultValue(true)
                .HasColumnName("isRequired");
            entity.Property(e => e.ItemId).HasColumnName("itemId");
            entity.Property(e => e.ItemType)
                .HasMaxLength(20)
                .HasColumnName("itemType");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");

            entity.HasOne(d => d.Bundle).WithMany(p => p.BundleItems)
                .HasForeignKey(d => d.BundleId)
                .HasConstraintName("FK_BundleItems_Bundles");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Carts__415B03B862791906");

            entity.HasIndex(e => e.UserId, "IX_Carts_UserId");

            entity.Property(e => e.CartId).HasColumnName("cartId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Carts_Users");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__283983B6B2A39549");

            entity.HasIndex(e => e.CartId, "IX_CartItems_CartId");

            entity.Property(e => e.CartItemId).HasColumnName("cartItemId");
            entity.Property(e => e.BundleId).HasColumnName("bundleId");
            entity.Property(e => e.CartId).HasColumnName("cartId");
            entity.Property(e => e.FrameId).HasColumnName("frameId");
            entity.Property(e => e.IsBundle).HasColumnName("isBundle");
            entity.Property(e => e.LensId).HasColumnName("lensId");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.ServiceId).HasColumnName("serviceId");
            entity.Property(e => e.TempPrescriptionJson).HasColumnName("tempPrescriptionJson");

            entity.HasOne(d => d.Bundle).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.BundleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_CartItems_Bundles");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK_CartItems_Carts");

            entity.HasOne(d => d.Frame).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.FrameId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_CartItems_Frames");

            entity.HasOne(d => d.Lens).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.LensId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_CartItems_Lenses");

            entity.HasOne(d => d.Service).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_CartItems_Services");
        });

        modelBuilder.Entity<Frame>(entity =>
        {
            entity.HasKey(e => e.FrameId).HasName("PK__Frames__36094A25DF172786");

            entity.HasIndex(e => e.FrameType, "IX_Frames_FrameType");

            entity.HasIndex(e => e.Price, "IX_Frames_Price");

            entity.HasIndex(e => e.StockStatus, "IX_Frames_StockStatus");

            entity.Property(e => e.FrameId).HasColumnName("frameId");
            entity.Property(e => e.Color)
                .HasMaxLength(50)
                .HasColumnName("color");
            entity.Property(e => e.FrameType)
                .HasMaxLength(50)
                .HasColumnName("frameType");
            entity.Property(e => e.Material)
                .HasMaxLength(50)
                .HasColumnName("material");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(12, 0)")
                .HasColumnName("price");
            entity.Property(e => e.SizeBridge).HasColumnName("sizeBridge");
            entity.Property(e => e.SizeTemple).HasColumnName("sizeTemple");
            entity.Property(e => e.SizeWidth).HasColumnName("sizeWidth");
            entity.Property(e => e.StockQuantity).HasColumnName("stockQuantity");
            entity.Property(e => e.StockStatus)
                .HasMaxLength(20)
                .HasColumnName("stockStatus");
        });

        modelBuilder.Entity<Lense>(entity =>
        {
            entity.HasKey(e => e.LensId).HasName("PK__Lenses__BE1FA9A7E0AE8ED6");

            entity.HasIndex(e => e.LensType, "IX_Lenses_LensType");

            entity.HasIndex(e => e.StockStatus, "IX_Lenses_StockStatus");

            entity.Property(e => e.LensId).HasColumnName("lensId");
            entity.Property(e => e.Coating)
                .HasMaxLength(100)
                .HasColumnName("coating");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.IndexValue)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("indexValue");
            entity.Property(e => e.LensType)
                .HasMaxLength(50)
                .HasColumnName("lensType");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(12, 0)")
                .HasColumnName("price");
            entity.Property(e => e.StockStatus)
                .HasMaxLength(20)
                .HasColumnName("stockStatus");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__0809335DEAB30224");

            entity.HasIndex(e => e.CreatedAt, "IX_Orders_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ShippingStatus, "IX_Orders_ShippingStatus");

            entity.HasIndex(e => e.Status, "IX_Orders_Status");

            entity.HasIndex(e => e.UserId, "IX_Orders_UserId");

            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.AddressId).HasColumnName("addressId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.OrderType)
                .HasMaxLength(20)
                .HasColumnName("orderType");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("paymentMethod");
            entity.Property(e => e.ShippingPhone)
                .HasMaxLength(20)
                .HasColumnName("shippingPhone");
            entity.Property(e => e.ShippingReceiverName)
                .HasMaxLength(100)
                .HasColumnName("shippingReceiverName");
            entity.Property(e => e.ShippingStatus)
                .HasMaxLength(20)
                .HasColumnName("shippingStatus");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(12, 0)")
                .HasColumnName("totalAmount");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.Address).WithMany(p => p.Orders)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Addresses");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__3724BD526043464F");

            entity.HasIndex(e => e.OrderId, "IX_OrderItems_OrderId");

            entity.HasIndex(e => e.PrescriptionId, "IX_OrderItems_PrescriptionId");

            entity.Property(e => e.OrderItemId).HasColumnName("orderItemId");
            entity.Property(e => e.BundleId).HasColumnName("bundleId");
            entity.Property(e => e.BundleSnapshot).HasColumnName("bundleSnapshot");
            entity.Property(e => e.FrameId).HasColumnName("frameId");
            entity.Property(e => e.IsBundle).HasColumnName("isBundle");
            entity.Property(e => e.LensId).HasColumnName("lensId");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.PrescriptionId).HasColumnName("prescriptionId");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(12, 0)")
                .HasColumnName("price");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.ServiceId).HasColumnName("serviceId");

            entity.HasOne(d => d.Bundle).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.BundleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_OrderItems_Bundles");

            entity.HasOne(d => d.Frame).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.FrameId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_OrderItems_Frames");

            entity.HasOne(d => d.Lens).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.LensId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_OrderItems_Lenses");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Prescription).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.PrescriptionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_OrderItems_PrescriptionProfiles");

            entity.HasOne(d => d.Service).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_OrderItems_Services");
        });

        modelBuilder.Entity<PrescriptionProfile>(entity =>
        {
            entity.HasKey(e => e.PrescriptionId).HasName("PK__Prescrip__7920FC24625CC1E9");

            entity.HasIndex(e => new { e.UserId, e.IsActive }, "IX_PrescriptionProfiles_IsActive");

            entity.HasIndex(e => e.UserId, "IX_PrescriptionProfiles_UserId");

            entity.Property(e => e.PrescriptionId).HasColumnName("prescriptionId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.LeftAxis).HasColumnName("leftAxis");
            entity.Property(e => e.LeftCyl)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("leftCYL");
            entity.Property(e => e.LeftSph)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("leftSPH");
            entity.Property(e => e.ProfileName)
                .HasMaxLength(100)
                .HasColumnName("profileName");
            entity.Property(e => e.RightAxis).HasColumnName("rightAxis");
            entity.Property(e => e.RightCyl)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("rightCYL");
            entity.Property(e => e.RightSph)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("rightSPH");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.User).WithMany(p => p.PrescriptionProfiles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_PrescriptionProfiles_Users");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__336E9B559E9AFEEA");

            entity.HasIndex(e => new { e.ProductType, e.ProductId, e.IsPrimary }, "IX_ProductImages_IsPrimary");

            entity.HasIndex(e => new { e.ProductType, e.ProductId }, "IX_ProductImages_ProductType");

            entity.Property(e => e.ImageId).HasColumnName("imageId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("imageUrl");
            entity.Property(e => e.IsPrimary).HasColumnName("isPrimary");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.ProductType)
                .HasMaxLength(20)
                .HasColumnName("productType");
        });

        modelBuilder.Entity<Return>(entity =>
        {
            entity.HasKey(e => e.ReturnId).HasName("PK__Returns__EBA76319A38C3AE8");

            entity.HasIndex(e => e.OrderItemId, "IX_Returns_OrderItemId");

            entity.HasIndex(e => e.Status, "IX_Returns_Status");

            entity.Property(e => e.ReturnId).HasColumnName("returnId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.OrderItemId).HasColumnName("orderItemId");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.Returns)
                .HasForeignKey(d => d.OrderItemId)
                .HasConstraintName("FK_Returns_OrderItems");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Services__455070DF7B5523DC");

            entity.Property(e => e.ServiceId).HasColumnName("serviceId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(12, 0)")
                .HasColumnName("price");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.ShipmentId).HasName("PK__Shipment__4721780151A41410");

            entity.HasIndex(e => e.OrderId, "IX_Shipments_OrderId");

            entity.HasIndex(e => e.Status, "IX_Shipments_Status");

            entity.HasIndex(e => e.TrackingNumber, "IX_Shipments_TrackingNumber");

            entity.Property(e => e.ShipmentId).HasColumnName("shipmentId");
            entity.Property(e => e.Carrier)
                .HasMaxLength(100)
                .HasColumnName("carrier");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.DeliveredAt).HasColumnName("deliveredAt");
            entity.Property(e => e.Notes)
                .HasMaxLength(1000)
                .HasColumnName("notes");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.ShippedAt).HasColumnName("shippedAt");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .HasColumnName("trackingNumber");
            entity.Property(e => e.TrackingUrl)
                .HasMaxLength(500)
                .HasColumnName("trackingUrl");

            entity.HasOne(d => d.Order).WithMany(p => p.Shipments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Shipments_Orders");
        });

        modelBuilder.Entity<ShipmentStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Shipment__19BDBDD3791C9E92");

            entity.ToTable("ShipmentStatusHistory");

            entity.HasIndex(e => e.CreatedAt, "IX_ShipmentStatusHistory_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ShipmentId, "IX_ShipmentStatusHistory_ShipmentId");

            entity.Property(e => e.HistoryId).HasColumnName("historyId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.Location)
                .HasMaxLength(200)
                .HasColumnName("location");
            entity.Property(e => e.ShipmentId).HasColumnName("shipmentId");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.StatusMessage)
                .HasMaxLength(500)
                .HasColumnName("statusMessage");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.Shipment).WithMany(p => p.ShipmentStatusHistories)
                .HasForeignKey(d => d.ShipmentId)
                .HasConstraintName("FK_ShipmentStatusHistory_Shipments");
        });

        modelBuilder.Entity<StockNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__StockNot__4BA5CEA92937B5E1");

            entity.HasIndex(e => e.Email, "IX_StockNotifications_Email");

            entity.HasIndex(e => new { e.ProductType, e.ProductId }, "IX_StockNotifications_ProductType");

            entity.HasIndex(e => e.Status, "IX_StockNotifications_Status");

            entity.Property(e => e.NotificationId).HasColumnName("notificationId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.NotifiedAt).HasColumnName("notifiedAt");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.ProductType)
                .HasMaxLength(20)
                .HasColumnName("productType");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__CB9A1CFF8F989845");

            entity.HasIndex(e => e.Email, "IX_Users_Email");

            entity.HasIndex(e => e.Role, "IX_Users_Role");

            entity.HasIndex(e => e.Status, "IX_Users_Status");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164F0FCC889").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("fullName");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("passwordHash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
