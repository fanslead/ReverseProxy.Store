using ReverseProxy.Store.EFCore.Revisions;

namespace ReverseProxy.Store.EFCore.ControlPlane;

public static class ProxyConfigControlPlaneModelBuilderExtensions
{
    public static ModelBuilder ConfigureProxyConfigControlPlane(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ConfigureProxyConfigRevisionStore();

        modelBuilder.Entity<ProxyConfigDraftEntity>(draft =>
        {
            draft.ToTable("config_drafts");
            draft.HasKey(entity => entity.Id);
            draft.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
            draft.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            draft.Property(entity => entity.DocumentJson).HasColumnName("document").IsRequired();
            draft.Property(entity => entity.SchemaVersion).HasColumnName("schema_version");
            draft.Property(entity => entity.ContentHash).HasColumnName("content_hash").HasMaxLength(64).IsFixedLength();
            draft.Property(entity => entity.BaseRevisionId).HasColumnName("base_revision_id");
            draft.Property(entity => entity.BaseRevisionNumber).HasColumnName("base_revision_number");
            draft.Property(entity => entity.RowVersion).HasColumnName("row_version").IsConcurrencyToken();
            draft.Property(entity => entity.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
            draft.Property(entity => entity.CreatedAt).HasColumnName("created_at");
            draft.Property(entity => entity.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
            draft.Property(entity => entity.UpdatedAt).HasColumnName("updated_at");
            draft.HasIndex(entity => entity.Name).IsUnique();
            draft.HasOne(entity => entity.BaseRevision)
                .WithMany()
                .HasForeignKey(entity => entity.BaseRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConfigOperationEntity>(operation =>
        {
            operation.ToTable("config_operations");
            operation.HasKey(entity => entity.Id);
            operation.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
            operation.Property(entity => entity.Type).HasColumnName("operation_type").HasConversion<string>().HasMaxLength(32);
            operation.Property(entity => entity.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
            operation.Property(entity => entity.RevisionId).HasColumnName("revision_id");
            operation.Property(entity => entity.RevisionNumber).HasColumnName("revision_number");
            operation.Property(entity => entity.RequestedBy).HasColumnName("requested_by").HasMaxLength(256);
            operation.Property(entity => entity.CreatedAt).HasColumnName("created_at");
            operation.Property(entity => entity.StartedAt).HasColumnName("started_at");
            operation.Property(entity => entity.CompletedAt).HasColumnName("completed_at");
            operation.Property(entity => entity.ErrorCode).HasColumnName("error_code").HasMaxLength(128);
            operation.Property(entity => entity.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            operation.HasOne(entity => entity.Revision)
                .WithMany()
                .HasForeignKey(entity => entity.RevisionId)
                .OnDelete(DeleteBehavior.Restrict);
            operation.HasIndex(entity => entity.CreatedAt);
        });

        modelBuilder.Entity<ConfigOutboxMessageEntity>(message =>
        {
            message.ToTable("config_outbox");
            message.HasKey(entity => entity.Id);
            message.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
            message.Property(entity => entity.EventType).HasColumnName("event_type").HasMaxLength(128);
            message.Property(entity => entity.RevisionId).HasColumnName("revision_id");
            message.Property(entity => entity.RevisionNumber).HasColumnName("revision_number");
            message.Property(entity => entity.PayloadJson).HasColumnName("payload").IsRequired();
            message.Property(entity => entity.OccurredAt).HasColumnName("occurred_at");
            message.Property(entity => entity.PublishedAt).HasColumnName("published_at");
            message.Property(entity => entity.Attempts).HasColumnName("attempts");
            message.Property(entity => entity.LastError).HasColumnName("last_error").HasMaxLength(2000);
            message.HasIndex(entity => new { entity.EventType, entity.RevisionId }).IsUnique();
            message.HasIndex(entity => new { entity.PublishedAt, entity.OccurredAt });
        });

        modelBuilder.Entity<AuditEntryEntity>(audit =>
        {
            audit.ToTable("audit_entries");
            audit.HasKey(entity => entity.Id);
            audit.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
            audit.Property(entity => entity.Action).HasColumnName("action").HasMaxLength(128);
            audit.Property(entity => entity.ResourceType).HasColumnName("resource_type").HasMaxLength(128);
            audit.Property(entity => entity.ResourceId).HasColumnName("resource_id").HasMaxLength(256);
            audit.Property(entity => entity.RevisionId).HasColumnName("revision_id");
            audit.Property(entity => entity.ActorId).HasColumnName("actor_id").HasMaxLength(256);
            audit.Property(entity => entity.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
            audit.Property(entity => entity.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
            audit.Property(entity => entity.DetailsJson).HasColumnName("details").IsRequired();
            audit.Property(entity => entity.OccurredAt).HasColumnName("occurred_at");
            audit.HasIndex(entity => entity.OccurredAt);
            audit.HasIndex(entity => entity.RevisionId);
        });

        modelBuilder.Entity<RuntimeNodeEntity>(node =>
        {
            node.ToTable("runtime_nodes");
            node.HasKey(entity => entity.NodeId);
            node.Property(entity => entity.NodeId).HasColumnName("node_id").HasMaxLength(256);
            node.Property(entity => entity.AppliedRevisionNumber).HasColumnName("applied_revision_number");
            node.Property(entity => entity.AppliedContentHash).HasColumnName("applied_content_hash").HasMaxLength(64).IsFixedLength();
            node.Property(entity => entity.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
            node.Property(entity => entity.LastError).HasColumnName("last_error").HasMaxLength(2000);
            node.Property(entity => entity.LastSeenAt).HasColumnName("last_seen_at");
            node.HasIndex(entity => entity.LastSeenAt);
        });

        return modelBuilder;
    }
}