using Application;
using Base.Contracts.DataAccess;
using Base.Contracts.DTO;
using Base.Contracts.Message;
using Base.Keycloak.Authentication;
using Base.Keycloak.Events;
using Base.Message.RabbitMQ;
using Contracts.Application;
using Contracts.DataAccess;
using DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using DTO.DataAccess.DataAccess.Mapper;
using DTO.DataAccess.Web.DTO;
using DTO.DataAccess.Web.Mapper;
using Infrastructure.Messaging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System.Net;
using Web.Configuration;
using IPNetwork = System.Net.IPNetwork;

DotEnvConfiguration.LoadFromRepositoryRoot();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/invoice-service-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    var configuredAppPort = Environment.GetEnvironmentVariable("APP_PORT") ?? Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrWhiteSpace(configuredAppPort) &&
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://+:{configuredAppPort}");
    }

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, _, loggerConfiguration) =>
    {
        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/invoice-service-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
    });

    var appDbConnectionString = RequiredConfiguration.AppConnectionString();
    var keycloakOptions = RequiredConfiguration.KeycloakOptions();
    var rabbitMqOptions = RequiredConfiguration.RabbitMqOptions();
    var invoiceMessagingOptions = RequiredConfiguration.InvoiceMessagingOptions(keycloakOptions);
    var glitchtipOptions = RequiredConfiguration.GlitchtipOptions();

    if (!string.IsNullOrWhiteSpace(glitchtipOptions.Dsn))
    {
        builder.WebHost.UseSentry(options =>
        {
            options.Dsn = glitchtipOptions.Dsn;
            options.Release = glitchtipOptions.Release;
            options.Environment = builder.Environment.EnvironmentName;
            options.MinimumBreadcrumbLevel = LogLevel.Information;
            options.MinimumEventLevel = LogLevel.Error;
            options.TracesSampleRate = 0;
        });
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(appDbConnectionString));
    builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<AppDbContext>());
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddKeycloakOidc(keycloakOptions);
    builder.Services.AddAuthorization();

    builder.Services.AddSingleton(invoiceMessagingOptions);
    builder.Services.AddRabbitMqPublisher(rabbitMqOptions);
    builder.Services.AddSingleton<IBaseEventHandler<UserEventContent>, UserEventHandler>();
    builder.Services.AddRabbitMqConsumer<UserEventConsumer>();
    builder.Services.AddSingleton<IMonthlyInvoiceEmailPublisher, RabbitMqMonthlyInvoiceEmailPublisher>();

    builder.Services.AddScoped<IMapper<Address, AddressEntity>, AddressEntityMapper>();
    builder.Services.AddScoped<IMapper<AddressContact, AddressContactEntity>, AddressContactEntityMapper>();
    builder.Services.AddScoped<IMapper<AppUser, AppUserEntity>, AppUserEntityMapper>();
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
    builder.Services.AddScoped<IMonthlyStatementSenderPaymentDetailsProvider, AppUserMonthlyStatementSenderPaymentDetailsProvider>();
    builder.Services.AddScoped<IPdfInvoiceImportService, PdfInvoiceImportService>();
    builder.Services.AddScoped<IPdfInvoiceTextExtractor, PdfInvoiceTextExtractor>();
    builder.Services.AddScoped<IPdfInvoiceProviderDetector, PdfInvoiceProviderDetector>();
    builder.Services.AddScoped<IPdfInvoiceParser, TeliaPdfInvoiceParser>();
    builder.Services.AddScoped<IPdfInvoiceParser, EnefitPdfInvoiceParser>();

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

    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
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
}
catch (Exception exception)
{
    Log.Fatal(exception, "Invoice service terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
