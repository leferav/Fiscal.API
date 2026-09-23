using Fiscal.API.Data;
using Fiscal.API.Services;
using Fiscal.API.Services.NFCe;
using Fiscal.API.Services.NFe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Certificado PFX vindo de variável de ambiente (Koyeb)
var certificadoBase64 = Environment.GetEnvironmentVariable("CERT_PFX_BASE64");
if (!string.IsNullOrWhiteSpace(certificadoBase64))
{
    var certificadoBytes = Convert.FromBase64String(certificadoBase64);
    var caminhoCertificado = Path.Combine(Path.GetTempPath(), "certificado.pfx");

    File.WriteAllBytes(caminhoCertificado, certificadoBytes);
    builder.Configuration["Fiscal:Certificado"] = caminhoCertificado;
}

// Banco de dados
builder.Services.AddDbContext<FiscalDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("FiscalDb")
    ));

// Controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================================
// JWT / AUTENTICAÇÃO
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key não configurada."
    );

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Jwt:Issuer não configurado."
    );

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Jwt:Audience não configurado."
    );

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

// ============================================================
// SERVIÇOS DA APLICAÇÃO
// ============================================================

builder.Services.AddScoped<FiscalService>();
builder.Services.AddAuthorization();
builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<ZeusConfigurationFactory>();
builder.Services.AddScoped<NFeBuilder>();

builder.Services.AddScoped<EmitenteBuilder>();
builder.Services.AddScoped<DestinatarioBuilder>();
builder.Services.AddScoped<ProdutoBuilder>();

builder.Services.AddScoped<TotalBuilder>();
builder.Services.AddScoped<TransporteBuilder>();
builder.Services.AddScoped<PagamentoBuilder>();

builder.Services.AddScoped<NFeXmlService>();

// NFC-e
builder.Services.AddScoped<NFCeBuilder>();

// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEnd", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ============================================================
// PIPELINE
// ============================================================

app.UseCors("FrontEnd");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// JWT
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await UsuarioSeed.CriarAdminInicialAsync(
    app.Services,
    app.Configuration
);

app.Run();