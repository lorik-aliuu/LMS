     using AutoMapper;
     using FluentValidation;
using FluentValidation.AspNetCore;
     using LMS.API.Middleware;
     using LMS.Application.AutoMapper;
     using LMS.Application.RepositoryInterfaces;
     using LMS.Application.ServiceInterfaces;
     using LMS.Application.Services;
     using LMS.Application.Validators;
     using LMS.Infrastructure;
     using LMS.Infrastructure.Hubs;
     using LMS.Infrastructure.Identity;
     using LMS.Infrastructure.Interfaces;
     using LMS.Infrastructure.Repositories;
     using LMS.Infrastructure.Services;
     using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.IdentityModel.Tokens;
        using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Diagnostics;
        using System.Text;


        var builder = WebApplication.CreateBuilder(args);




    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;

            options.User.RequireUniqueEmail = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();


        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false; 
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost",          
                "http://localhost:80",        
                "http://localhost:3000",
                "http://localhost:8080"

            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true); 
    });
});





builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString,
                b => b.MigrationsAssembly("LMS.Infrastructure"))
        );


        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
       


                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/user-event"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });


        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
        });


        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Library Management System API",
                Version = "v1",
                Description = "API for managing personal library"
            });

   
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });




        builder.Services.AddSignalR();
        builder.Services.AddControllers();
        builder.Services.AddFluentValidationAutoValidation();



        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        builder.Services.AddScoped<IBookRepository, BookRepository>();

    builder.Services.AddScoped<IOpenAiService, OpenAiService>();
    builder.Services.AddScoped<IAiQueryService, AiQueryService>();

    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICacheService, RedisService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

builder.Services.AddScoped<ILibraryInsightsService, LibraryInsightsService>();

builder.Services.AddScoped<IUserNotifierService, UserNotifierService>();



    builder.Services.AddScoped<IBookService, BookService>();

        builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

        builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();







        var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));



app.UseMiddleware<ExceptionHandlingMiddleware>();


        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "LMS API ");
            });
        }



           // app.UseHttpsRedirection();


app.UseRouting();

            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<UserEventHub>("/hubs/user-event");
}); 



using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var configuration = services.GetRequiredService<IConfiguration>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                var adminEmail = configuration["AdminUser:Email"];
                var adminPassword = configuration["AdminUser:Password"];
                var adminUserName = configuration["AdminUser:UserName"];
                var adminFirstName = configuration["AdminUser:FirstName"];
                var adminLastName = configuration["AdminUser:LastName"];

                await SeedDefaultRolesAndAdmin(
                    userManager,
                    roleManager,
                    adminEmail,
                    adminPassword,
                    adminUserName,
                    adminFirstName,
                    adminLastName
                );
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
            }

    
        }


        static async Task SeedDefaultRolesAndAdmin(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            string adminEmail,
            string adminPassword,
            string adminUserName,
            string adminFirstName,
            string adminLastName)
        {
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    FirstName = adminFirstName,
                    LastName = adminLastName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    app.Run();


public partial class Program { }

