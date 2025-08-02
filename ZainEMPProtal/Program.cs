using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.IISIntegration; // ✅ Use IISDefaults here
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ZainEMPProtal.Data;
using ZainEMPProtal.Services;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Database Connections --------------------
builder.Services.AddScoped<IDbConnectionFactory>(provider =>
    new SqlConnectionFactory(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IZainFlowDbConnectionFactory>(provider =>
    new ZainFlowDbConnectionFactory(builder.Configuration.GetConnectionString("ZainWFProd")));

// -------------------- Authentication (JWT only, Windows via IIS) --------------------
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// -------------------- Authorization Policies --------------------
builder.Services.AddAuthorization(options =>
{
    // ✅ Default: JWT only
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .Build();

    // ✅ Windows only (IIS)
    options.AddPolicy("WindowsOnly", policy =>
    {
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes(IISDefaults.AuthenticationScheme); // ✅ Use IIS scheme
    });

    // ✅ Either JWT or Windows
    options.AddPolicy("JwtOrWindows", policy =>
    {
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, IISDefaults.AuthenticationScheme);
    });
});

// -------------------- Business Services --------------------
builder.Services.AddHttpClient<IAuthService, AuthService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ISkillGapRepository, SkillGapRepository>();
builder.Services.AddScoped<ISkillGapService, SkillGapService>();

// -------------------- CORS --------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:5173",
                "https://internalservices.jo.zain.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // ✅ Important for Windows Auth
    });
});

// -------------------- Controllers + Swagger --------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityDefinition("Windows", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Windows Authentication (via IIS)",
        Name = "Windows",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "negotiate"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// -------------------- Middleware --------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.OAuthClientId("swagger");
    c.OAuthAppName("Zain EMP Portal API");
});

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
