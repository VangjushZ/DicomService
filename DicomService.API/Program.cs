using DicomService.API.Data;
using DicomService.API.Infrastructure;
using DicomService.API.Interfaces;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.EntityFrameworkCore;

namespace DicomService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IDicomParser, FoDicomParser>();
            builder.Services.AddScoped<IFileStore, LocalFileStore>();


            builder.Services
                .AddFellowOakDicom()
                .AddImageManager<ImageSharpImageManager>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }

            DicomSetupBuilder.UseServiceProvider(app.Services);

            app.UseSwagger();
            app.UseSwaggerUI();
        
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
