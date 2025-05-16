using DicomService.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace DicomService.API.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<DicomFileMetaData> DicomFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DicomFileMetaData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName)
                    .IsRequired();
                entity.Property(e => e.FilePath)
                    .IsRequired();
                entity.Property(e => e.UploadedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.PreviewPath)
                    .IsRequired(false);
            });
        }
    }
}
