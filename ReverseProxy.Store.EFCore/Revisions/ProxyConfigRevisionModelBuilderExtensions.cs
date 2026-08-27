namespace ReverseProxy.Store.EFCore.Revisions;

public static class ProxyConfigRevisionModelBuilderExtensions
{
    public static ModelBuilder ConfigureProxyConfigRevisionStore(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<ProxyConfigRevisionEntity>(revision =>
        {
            revision.ToTable("config_revisions");
            revision.HasKey(entity => entity.Id);
            revision.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
            revision.Property(entity => entity.RevisionNumber).HasColumnName("revision_number");
            revision.Property(entity => entity.SchemaVersion).HasColumnName("schema_version");
            revision.Property(entity => entity.ContentHash).HasColumnName("content_hash").HasMaxLength(64).IsFixedLength();
            revision.Property(entity => entity.DocumentJson).HasColumnName("document").IsRequired();
            revision.Property(entity => entity.PublishedBy).HasColumnName("published_by").HasMaxLength(256);
            revision.Property(entity => entity.PublishedAt).HasColumnName("published_at");
            revision.HasIndex(entity => entity.RevisionNumber).IsUnique();
        });

        modelBuilder.Entity<RuntimeConfigStateEntity>(state =>
        {
            state.ToTable(
                "runtime_config_state",
                table => table.HasCheckConstraint("ck_runtime_config_state_singleton", "state_key = 1"));
            state.HasKey(entity => entity.StateKey);
            state.Property(entity => entity.StateKey).HasColumnName("state_key").ValueGeneratedNever();
            state.Property(entity => entity.ActiveRevisionId).HasColumnName("active_revision_id");
            state.Property(entity => entity.ActiveRevisionNumber).HasColumnName("active_revision_number");
            state.Property(entity => entity.ActiveContentHash).HasColumnName("active_content_hash").HasMaxLength(64).IsFixedLength();
            state.Property(entity => entity.UpdatedAt).HasColumnName("updated_at");
            state.HasOne(entity => entity.ActiveRevision)
                .WithOne(entity => entity.RuntimeState)
                .HasForeignKey<RuntimeConfigStateEntity>(entity => entity.ActiveRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        return modelBuilder;
    }
}