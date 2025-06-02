using Agazaty.Data.Base;
using Agazaty.Data.Services.Implementation;
using Agazaty.Data.Services.Interfaces;
using Agazaty.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Agazaty.Data.Email;
using Agazaty.Data.Services.AutomaticInitializationService;
using Agazaty.Data.Services;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Agazaty
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<AppDbContext>(op => op.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(
                options =>
                {
                    options.Password.RequireDigit = false;       // No numbers required
                    options.Password.RequireLowercase = false;   // No lowercase required
                    options.Password.RequireUppercase = false;   // No uppercase required
                    options.Password.RequireNonAlphanumeric = false; // No special characters required
                    options.Password.RequiredLength = 1;         // Min length = 1 (can be any value)
                    options.Password.RequiredUniqueChars = 0;    // No unique characters required
                }).AddEntityFrameworkStores<AppDbContext>();/*.AddDefaultTokenProviders();*/

            builder.Services.AddScoped<IEntityBaseRepository<SickLeave>, EntityBaseRepository<SickLeave>>();
            builder.Services.AddScoped<IEntityBaseRepository<Department>, EntityBaseRepository<Department>>();
            builder.Services.AddScoped<IEntityBaseRepository<CasualLeave>, EntityBaseRepository<CasualLeave>>();
            builder.Services.AddScoped<IEntityBaseRepository<PermitLeave>, EntityBaseRepository<PermitLeave>>();
            builder.Services.AddScoped<IEntityBaseRepository<PermitLeaveImage>, EntityBaseRepository<PermitLeaveImage>>();
            builder.Services.AddScoped<IEntityBaseRepository<NormalLeave>, EntityBaseRepository<NormalLeave>>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<ILeaveValidationService, LeaveValidationService>();
            builder.Services.AddScoped<IEntityBaseRepository<Holiday>, EntityBaseRepository<Holiday>>();
            builder.Services.AddTransient<IDbConnection>(sp =>
              new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

            builder.Services.Configure<JWT>(builder.Configuration.GetSection("JWT"));

            builder.Services.AddAutoMapper(typeof(Program));

            builder.Services.AddHostedService<JulyLeaveInitializationService>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(o => {
                    o.RequireHttpsMetadata = false;
                    o.SaveToken = true;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = builder.Configuration["JWT:Issuer"],
                        ValidAudience = builder.Configuration["JWT:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]))
                    };
                });
            /*
             Development (Local Testing)
                options.RequireHttpsMetadata = false; // Allow HTTP for local development
                options.SaveToken = true;             // Store token if needed for later access

             Production (Live Environment)
                options.RequireHttpsMetadata = true;  // Enforce HTTPS for security
                options.SaveToken = false;            // No need to store token if only validating it
             */
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                       builder => builder
                               .AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader());
            });

            builder.Services.AddAuthorization();

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            // Serve static files if your API needs to serve any (e.g., documentation files)
            app.UseStaticFiles();

            // Enables routing in the app
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}