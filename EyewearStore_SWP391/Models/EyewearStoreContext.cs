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

    // Core entities
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Address> Addresses { get; set; }

    // Product hierarchy (TPT inheritance)
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<Frame> Frames { get; set; }
    public virtual DbSet<Lens> Lenses { get; set; }
    public virtual DbSet<Bundle> Bundles { get; set; }
    public virtual DbSet<BundleItem> BundleItems { get; set; }

    // Services
    public virtual DbSet<Service> Services { get; set; }

    // Product Images
    public virtual DbSet<ProductImage> ProductImages { get; set; }

    // Cart
    public virtual DbSet<Cart> Carts { get; set; }
    public virtual DbSet<CartItem> CartItems { get; set; }

    // Prescriptions
    public virtual DbSet<PrescriptionProfile> PrescriptionProfiles { get; set; }

    // Orders
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderItem> OrderItems { get; set; }

    // Shipments
    public virtual DbSet<Shipment> Shipments { get; set; }
    public virtual DbSet<ShipmentStatusHistory> ShipmentStatusHistories { get; set; }

    // Returns
    public virtual DbSet<Return> Returns { get; set; }

    // Wishlist
    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=EyewearStore;Integrated Security=True;TrustServerCertificate=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // =========================
        // USERS
        // =========================
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UQ_users_email");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsRequired()
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsRequired()
                .HasColumnName("password_hash");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsRequired()
                .HasColumnName("full_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsRequired()
                .HasColumnName("role");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");
        });

        // =========================
        // ADDRESSES
        // =========================
        modelBuilder.Entity<Address>(entity =>
        {
            entity.ToTable("addresses");
            entity.HasKey(e => e.AddressId);

            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ReceiverName)
                .HasMaxLength(100)
                .IsRequired()
                .HasColumnName("receiver_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsRequired()
                .HasColumnName("phone");
            entity.Property(e => e.AddressLine)
                .HasMaxLength(500)
                .IsRequired()
                .HasColumnName("address_line");
            entity.Property(e => e.IsDefault)
                .HasDefaultValue(false)
                .HasColumnName("is_default");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_addresses_users");
        });

        // =========================
        // PRODUCT (BASE - TPT)
        // =========================
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("product");
            entity.HasKey(e => e.ProductId);

            entity.HasIndex(e => e.Sku).IsUnique().HasDatabaseName("UQ_product_sku");
            entity.HasIndex(e => e.ProductType).HasDatabaseName("IX_product_type");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsRequired()
                .HasColumnName("sku");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired()
                .HasColumnName("name");
            entity.Property(e => e.Description)
                .HasColumnName("description");
            entity.Property(e => e.ProductType)
                .HasMaxLength(20)
                .IsRequired()
                .HasColumnName("product_type");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired()
                .HasColumnName("price");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired()
                .HasColumnName("currency");
            entity.Property(e => e.InventoryQty)
                .HasColumnName("inventory_qty");
            entity.Property(e => e.Attributes)
                .HasColumnName("attributes");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("updated_at");
        });

        // =========================
        // FRAMES (TPT)
        // =========================
        modelBuilder.Entity<Frame>(entity =>
        {
            entity.ToTable("frames");

            entity.Property(e => e.FrameMaterial)
                .HasMaxLength(100)
                .HasColumnName("frame_material");
            entity.Property(e => e.FrameType)
                .HasMaxLength(50)
                .HasColumnName("frame_type");
            entity.Property(e => e.BridgeWidth)
                .HasColumnType("decimal(5,2)")
                .HasColumnName("bridge_width");
            entity.Property(e => e.TempleLength)
                .HasColumnType("decimal(5,2)")
                .HasColumnName("temple_length");
        });

        // =========================
        // LENSES (TPT)
        // =========================
        modelBuilder.Entity<Lens>(entity =>
        {
            entity.ToTable("lenses");

            entity.Property(e => e.LensType)
                .HasMaxLength(50)
                .HasColumnName("lens_type");
            entity.Property(e => e.LensIndex)
                .HasColumnType("decimal(4,2)")
                .HasColumnName("lens_index");
            entity.Property(e => e.IsPrescription)
                .IsRequired()
                .HasColumnName("is_prescription");
        });

        // =========================
        // BUNDLES (TPT)
        // =========================
        modelBuilder.Entity<Bundle>(entity =>
        {
            entity.ToTable("bundles");

            entity.Property(e => e.BundleNote)
                .HasMaxLength(255)
                .HasColumnName("bundle_note");
        });

        // =========================
        // BUNDLE ITEMS
        // =========================
        modelBuilder.Entity<BundleItem>(entity =>
        {
            entity.ToTable("bundle_items");
            entity.HasKey(e => e.BundleItemId);

            entity.HasIndex(e => new { e.BundleProductId, e.ProductId })
                .IsUnique()
                .HasDatabaseName("UQ_bundle_items");

            entity.Property(e => e.BundleItemId).HasColumnName("bundle_item_id");
            entity.Property(e => e.BundleProductId).HasColumnName("bundle_product_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");

            entity.HasOne(d => d.BundleProduct)
                .WithMany(p => p.BundleItems)
                .HasForeignKey(d => d.BundleProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_bundle_items_bundle");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.ContainedInBundles)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bundle_items_product");
        });

        // =========================
        // SERVICES
        // =========================
        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("services");
            entity.HasKey(e => e.ServiceId);

            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired()
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired()
                .HasColumnName("price");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");
        });

        // =========================
        // PRODUCT IMAGES
        // =========================
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images");
            entity.HasKey(e => e.ImageId);

            entity.HasIndex(e => e.ProductId).HasDatabaseName("IX_product_images_product");

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsRequired()
                .HasColumnName("image_url");
            entity.Property(e => e.AltText)
                .HasMaxLength(255)
                .HasColumnName("alt_text");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false)
                .HasColumnName("is_primary");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_product_images_product");
        });

        // =========================
        // CARTS
        // =========================
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("carts");
            entity.HasKey(e => e.CartId);

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_carts_user");

            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_carts_users");
        });

        // =========================
        // CART ITEMS
        // =========================
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("cart_items");
            entity.HasKey(e => e.CartItemId);

            entity.HasIndex(e => e.CartId).HasDatabaseName("IX_cart_items_cart");

            entity.Property(e => e.CartItemId).HasColumnName("cart_item_id");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.TempPrescriptionJson)
                .HasColumnName("temp_prescription_json");

            entity.HasOne(d => d.Cart)
                .WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_cart_items_cart");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_cart_items_product");

            entity.HasOne(d => d.Service)
                .WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_cart_items_service");
        });

        // =========================
        // PRESCRIPTION PROFILES
        // =========================
        modelBuilder.Entity<PrescriptionProfile>(entity =>
        {
            entity.ToTable("prescription_profiles");
            entity.HasKey(e => e.PrescriptionId);

            entity.Property(e => e.PrescriptionId).HasColumnName("prescription_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProfileName)
                .HasMaxLength(100)
                .HasColumnName("profile_name");
            entity.Property(e => e.LeftSph)
                .HasColumnType("decimal(5,2)")
                .HasColumnName("left_sph");
            entity.Property(e => e.LeftCyl)
                .HasColumnType("decimal(5,2)")
                .HasColumnName("left_cyl");
            entity.Property(e => e.LeftAxis)
                .HasColumnName("left_axis");
            entity.Property(e => e.RightSph)
                .HasColumnType("decimal(5,2)")
                .HasColumnName("right_sph");
            entity.Property(e => e.RightCyl)
                .HasColumnType("decimal(5,2)")
                .HasColumnName("right_cyl");
            entity.Property(e => e.RightAxis)
                .HasColumnName("right_axis");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(false)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User)
                .WithMany(p => p.PrescriptionProfiles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_prescription_profiles_users");
        });

        // =========================
        // ORDERS
        // =========================
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.OrderId);

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_orders_user");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsRequired()
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired()
                .HasColumnName("total_amount");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_orders_users");

            entity.HasOne(d => d.Address)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_orders_addresses");
        });

        // =========================
        // ORDER ITEMS
        // =========================
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(e => e.OrderItemId);

            entity.HasIndex(e => e.OrderId).HasDatabaseName("IX_order_items_order");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.PrescriptionId).HasColumnName("prescription_id");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired()
                .HasColumnName("unit_price");
            entity.Property(e => e.Quantity)
                .IsRequired()
                .HasColumnName("quantity");
            entity.Property(e => e.IsBundle)
                .HasDefaultValue(false)
                .HasColumnName("is_bundle");
            entity.Property(e => e.SnapshotJson)
                .HasColumnName("snapshot_json");

            entity.HasOne(d => d.Order)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_order_items_orders");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_order_items_product");

            entity.HasOne(d => d.Prescription)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.PrescriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_order_items_prescription");
        });

        // =========================
        // SHIPMENTS
        // =========================
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("shipments");
            entity.HasKey(e => e.ShipmentId);

            entity.Property(e => e.ShipmentId).HasColumnName("shipment_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Carrier)
                .HasMaxLength(100)
                .HasColumnName("carrier");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .HasColumnName("tracking_number");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.ShippedAt)
                .HasColumnName("shipped_at");
            entity.Property(e => e.DeliveredAt)
                .HasColumnName("delivered_at");

            entity.HasOne(d => d.Order)
                .WithMany(p => p.Shipments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_shipments_orders");
        });

        // =========================
        // SHIPMENT STATUS HISTORY
        // =========================
        modelBuilder.Entity<ShipmentStatusHistory>(entity =>
        {
            entity.ToTable("shipment_status_history");
            entity.HasKey(e => e.HistoryId);

            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.ShipmentId).HasColumnName("shipment_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Shipment)
                .WithMany(p => p.ShipmentStatusHistories)
                .HasForeignKey(d => d.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_shipment_history_shipments");
        });

        // =========================
        // RETURNS
        // =========================
        modelBuilder.Entity<Return>(entity =>
        {
            entity.ToTable("returns");
            entity.HasKey(e => e.ReturnId);

            entity.Property(e => e.ReturnId).HasColumnName("return_id");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.OrderItem)
                .WithMany(p => p.Returns)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_returns_order_items");
        });

        // =========================
        // WISHLIST
        // =========================
        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.ToTable("wishlist");
            entity.HasKey(e => e.WishlistId);

            entity.HasIndex(e => new { e.UserId, e.ProductId })
                .IsUnique()
                .HasDatabaseName("UQ_wishlist_user_product");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_wishlist_user");

            entity.Property(e => e.WishlistId).HasColumnName("wishlist_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.NotifyOnRestock)
                .HasDefaultValue(true)
                .HasColumnName("notify_on_restock");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_wishlist_users");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_wishlist_product");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
