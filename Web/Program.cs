using Application;
using Application.RoleManagement;
using Application.Roles;
using Base.Contracts.DataAccess;
using Base.Contracts.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using DTO.DataAccess.DataAccess.Mapper;
using DTO.DataAccess.Web.DTO;
using DTO.DataAccess.Web.Mapper;
using Infrastructure.AuthService;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Web;
using Web.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using Web.IdentityHub;
using IPNetwork = System.Net.IPNetwork;

DotEnvConfiguration.LoadFromRepositoryRoot();

var configuredAppPort = Environment.GetEnvironmentVariable("APP_PORT") ?? Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(configuredAppPort) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://+:{configuredAppPort}");
}

var builder = WebApplication.CreateBuilder(args);

var appDbConnectionString = RequiredConfiguration.AppConnectionString(builder.Configuration);
var identityHubOptions = RequiredConfiguration.IdentityHubOptions(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(appDbConnectionString));
builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<IdentityHubOptions>(options =>
{
    options.BaseUrl = identityHubOptions.BaseUrl;
    options.ClientId = identityHubOptions.ClientId;
    options.ClientSecret = identityHubOptions.ClientSecret;
    options.CallbackUrl = identityHubOptions.CallbackUrl;
});

builder.Services.Configure<AuthServiceOptions>(options =>
{
    options.BaseUrl = identityHubOptions.BaseUrl;
    options.ClientId = Guid.Parse(identityHubOptions.ClientId);
    options.ClientSecret = identityHubOptions.ClientSecret;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
    })
    .AddJwtBearer(options =>
    {
        options.Authority = identityHubOptions.BaseUrl.TrimEnd('/');
        options.Audience = identityHubOptions.ClientId;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            RoleClaimType = "roles"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpClient<IIdentityHubClient, IdentityHubClient>(client =>
    {
        client.BaseAddress = new Uri($"{identityHubOptions.BaseUrl.TrimEnd('/')}/");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false
    });
builder.Services.AddScoped<IdentityHubAuthenticationService>();

builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri($"{identityHubOptions.BaseUrl.TrimEnd('/')}/");
});

builder.Services.AddScoped<IMapper<Address, AddressEntity>, AddressEntityMapper>();
builder.Services.AddScoped<IMapper<AddressContact, AddressContactEntity>, AddressContactEntityMapper>();
builder.Services.AddScoped<IMapper<AppUser, AppUserEntity>, AppUserEntityMapper>();
builder.Services.AddScoped<IMapper<AppRole, AppRoleEntity>, AppRoleEntityMapper>();
builder.Services.AddScoped<IMapper<Contact, ContactEntity>, ContactEntityMapper>();
builder.Services.AddScoped<IMapper<Invoice, InvoiceEntity>, InvoiceEntityMapper>();
builder.Services.AddScoped<IMapper<InvoiceAllocation, InvoiceAllocationEntity>, InvoiceAllocationEntityMapper>();
builder.Services.AddScoped<IMapper<MonthlyStatement, MonthlyStatementEntity>, MonthlyStatementEntityMapper>();
builder.Services.AddScoped<IMapper<MonthlyStatementLine, MonthlyStatementLineEntity>, MonthlyStatementLineEntityMapper>();
builder.Services.AddScoped<IMapper<Service, ServiceEntity>, ServiceEntityMapper>();

builder.Services.AddScoped<IMapper<AddressDto, Address>, AddressDtoMapper>();
builder.Services.AddScoped<IMapper<AddressContactDto, AddressContact>, AddressContactDtoMapper>();
builder.Services.AddScoped<IMapper<ContactDto, Contact>, ContactDtoMapper>();
builder.Services.AddScoped<IMapper<InvoiceDto, Invoice>, InvoiceDtoMapper>();
builder.Services.AddScoped<IMapper<InvoiceAllocationDto, InvoiceAllocation>, InvoiceAllocationDtoMapper>();
builder.Services.AddScoped<IMapper<MonthlyStatementDto, MonthlyStatement>, MonthlyStatementDtoMapper>();
builder.Services.AddScoped<IMapper<MonthlyStatementLineDto, MonthlyStatementLine>, MonthlyStatementLineDtoMapper>();
builder.Services.AddScoped<IMapper<ServiceDto, Service>, ServiceDtoMapper>();

builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IAddressContactRepository, AddressContactRepository>();
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddScoped<IAppRoleRepository, AppRoleRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceAllocationRepository, InvoiceAllocationRepository>();
builder.Services.AddScoped<IMonthlyStatementRepository, MonthlyStatementRepository>();
builder.Services.AddScoped<IMonthlyStatementLineRepository, MonthlyStatementLineRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IBaseUow, DataAccessUow>();

builder.Services.AddScoped<ICreateAddressService, CreateAddressService>();
builder.Services.AddScoped<ICreateContactService, CreateContactService>();
builder.Services.AddScoped<ICreateInvoiceService, CreateInvoiceService>();
builder.Services.AddScoped<IGenerateMonthlyStatementService, GenerateMonthlyStatementService>();
builder.Services.AddScoped<ISendMonthlyStatementService, SendMonthlyStatementService>();
builder.Services.AddScoped<IMonthlyStatementSenderPaymentDetailsProvider, IdentityMonthlyStatementSenderPaymentDetailsProvider>();
builder.Services.AddScoped<IPdfInvoiceImportService, PdfInvoiceImportService>();
builder.Services.AddScoped<IPdfInvoiceTextExtractor, PdfInvoiceTextExtractor>();
builder.Services.AddScoped<IPdfInvoiceProviderDetector, PdfInvoiceProviderDetector>();
builder.Services.AddScoped<IPdfInvoiceParser, TeliaPdfInvoiceParser>();
builder.Services.AddScoped<IPdfInvoiceParser, EnefitPdfInvoiceParser>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserRoleManagementService, UserRoleManagementService>();

var emailProvider = RequiredConfiguration.EmailProvider(builder.Configuration);
if (emailProvider.Equals("Console", StringComparison.OrdinalIgnoreCase))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("EMAIL_PROVIDER=Console is only allowed in Development.");
    }

    builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
}
else if (emailProvider.Equals("Brevo", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton(RequiredConfiguration.BrevoEmailOptions(builder.Configuration));
    builder.Services.AddHttpClient<IEmailSender, BrevoEmailSender>(client =>
    {
        client.BaseAddress = new Uri("https://api.brevo.com/v3/");
    });
}
else
{
    throw new InvalidOperationException($"Unsupported email provider '{emailProvider}'. Use 'Brevo' or 'Console'.");
}

builder.Services.AddControllersWithViews();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    options.KnownIPNetworks.Clear();

    options.KnownIPNetworks.Add(
        new IPNetwork(IPAddress.Parse("172.18.0.0"), 16)
    );
});

var app = builder.Build();
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
