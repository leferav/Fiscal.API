using Fiscal.API.Data;
using Fiscal.API.Services;
using Fiscal.API.Services.NFCe;
using Fiscal.API.Services.NFe;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FiscalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FiscalDb")));

// Controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Serviços da aplicação
builder.Services.AddScoped<FiscalService>();

builder.Services.AddScoped<ZeusConfigurationFactory>();
builder.Services.AddScoped<NFeBuilder>();

builder.Services.AddScoped<EmitenteBuilder>();
builder.Services.AddScoped<DestinatarioBuilder>();
builder.Services.AddScoped<ProdutoBuilder>();

builder.Services.AddScoped<TotalBuilder>();
builder.Services.AddScoped<TransporteBuilder>();
builder.Services.AddScoped<PagamentoBuilder>();

builder.Services.AddScoped<NFeXmlService>();

//NFCe
builder.Services.AddScoped<NFCeBuilder>();

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


app.UseCors("FrontEnd");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();