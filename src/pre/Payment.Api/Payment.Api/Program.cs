
using Hangfire;
using Payment.Api.Services;
using Payment.Application.Feartures.Commands;
using Payment.Application.Interface;
using Payment.Persistence.Persist;
using Payment.Service.VnPay.Config;
using System.Reflection;

namespace Payment.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo()
                {
                    Version = "v1",
                    Title = "Payment Service Api",
                    Description = "Sample .NET Payment Api",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact()
                    {
                        Name = "Dong Hai",
                    }
                });
                var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var path = Path.Combine(AppContext.BaseDirectory, xmlFileName);
                options.IncludeXmlComments(path);
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ISqlService, SqlService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IConnectionService, ConnectionService>();

            builder.Services.AddMediatR(r =>
            {
                r.RegisterServicesFromAssembly(typeof(CreateMerchant).Assembly);
            });
            builder.Services.Configure<VnpayConfig>(
    builder.Configuration.GetSection(VnpayConfig.ConfigName));
            
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("Database"),
                new Hangfire.SqlServer.SqlServerStorageOptions()
                {
                    //TODO: Change hangfire sql server option
                }));
            builder.Services.AddHangfireServer();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseHangfireDashboard();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
