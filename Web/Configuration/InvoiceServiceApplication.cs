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
using Web.Filters;
using IPNetwork = System.Net.IPNetwork;

namespace Web.Configuration;

public static class InvoiceServiceApplication
{
    public static void Run(string[] args, Action<WebApplication> configurePipeline)
    {
        DotEnvConfiguration.LoadFromRepositoryRoot();
        ConfigureBootstrapLogger();

        try
        {
            ConfigureAspNetCoreUrls();

            var builder = WebApplication.CreateBuilder(args);
            builder.ConfigureHost();
            builder.ConfigureServices();

            var app = builder.Build();
            configurePipeline(app);
            app.Run();
        }
        catch (HostAbortedException)
        {
            // EF Core design-time tooling aborts host construction after service discovery.
            throw;
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
    }

    private static void ConfigureBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/invoice-service-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();
    }

    private static void ConfigureAspNetCoreUrls()
    {
        var configuredAppPort = Environment.GetEnvironmentVariable("APP_PORT") ?? Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(configuredAppPort) &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://+:{configuredAppPort}");
        }
    }

    private static void ConfigureHost(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((_, _, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/invoice-service-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
        });

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
    }

    private static void ConfigureServices(this WebApplicationBuilder builder)
    {
        var appDbConnectionString = RequiredConfiguration.AppConnectionString();
        var keycloakOptions = RequiredConfiguration.KeycloakOptions();
        var rabbitMqOptions = RequiredConfiguration.RabbitMqOptions();
        var invoiceMessagingOptions = RequiredConfiguration.InvoiceMessagingOptions(keycloakOptions);

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(appDbConnectionString));
        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<AppDbContext>());
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        builder.Services.AddHealthChecks();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddKeycloakOidc(keycloakOptions);
        builder.Services.AddAuthorization();

        builder.Services.AddSingleton(invoiceMessagingOptions);
        builder.Services.AddRabbitMqPublisher(rabbitMqOptions);
        builder.Services.AddSingleton<IBaseEventHandler<UserEventContent>, UserEventHandler>();
        builder.Services.AddRabbitMqConsumer<UserEventConsumer>();
        builder.Services.AddSingleton<IMonthlyInvoiceEmailPublisher, RabbitMqMonthlyInvoiceEmailPublisher>();

        builder.Services.AddDataMappers();
        builder.Services.AddWebMappers();
        builder.Services.AddRepositories();
        builder.Services.AddApplicationServices();

        builder.Services.AddScoped<CurrentAppUserViewDataFilter>();
        builder.Services.AddControllersWithViews(options =>
            options.Filters.Add<CurrentAppUserViewDataFilter>());
        builder.Services.ConfigureForwardedHeaders();
    }

    private static void AddDataMappers(this IServiceCollection services)
    {
        services.AddScoped<IMapper<Address, AddressEntity>, AddressEntityMapper>();
        services.AddScoped<IMapper<AddressContact, AddressContactEntity>, AddressContactEntityMapper>();
        services.AddScoped<IMapper<AppUser, AppUserEntity>, AppUserEntityMapper>();
        services.AddScoped<IMapper<Contact, ContactEntity>, ContactEntityMapper>();
        services.AddScoped<IMapper<Invoice, InvoiceEntity>, InvoiceEntityMapper>();
        services.AddScoped<IMapper<InvoiceAllocation, InvoiceAllocationEntity>, InvoiceAllocationEntityMapper>();
        services.AddScoped<IMapper<MonthlyStatement, MonthlyStatementEntity>, MonthlyStatementEntityMapper>();
        services.AddScoped<IMapper<MonthlyStatementLine, MonthlyStatementLineEntity>, MonthlyStatementLineEntityMapper>();
        services.AddScoped<IMapper<Service, ServiceEntity>, ServiceEntityMapper>();
    }

    private static void AddWebMappers(this IServiceCollection services)
    {
        services.AddScoped<IMapper<AddressDto, Address>, AddressDtoMapper>();
        services.AddScoped<IMapper<AddressContactDto, AddressContact>, AddressContactDtoMapper>();
        services.AddScoped<IMapper<ContactDto, Contact>, ContactDtoMapper>();
        services.AddScoped<IMapper<InvoiceDto, Invoice>, InvoiceDtoMapper>();
        services.AddScoped<IMapper<InvoiceAllocationDto, InvoiceAllocation>, InvoiceAllocationDtoMapper>();
        services.AddScoped<IMapper<MonthlyStatementDto, MonthlyStatement>, MonthlyStatementDtoMapper>();
        services.AddScoped<IMapper<MonthlyStatementLineDto, MonthlyStatementLine>, MonthlyStatementLineDtoMapper>();
        services.AddScoped<IMapper<ServiceDto, Service>, ServiceDtoMapper>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IAddressContactRepository, AddressContactRepository>();
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceAllocationRepository, InvoiceAllocationRepository>();
        services.AddScoped<IMonthlyStatementRepository, MonthlyStatementRepository>();
        services.AddScoped<IMonthlyStatementLineRepository, MonthlyStatementLineRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IBaseUow, DataAccessUow>();
    }

    private static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICreateAddressService, CreateAddressService>();
        services.AddScoped<ICreateContactService, CreateContactService>();
        services.AddScoped<ICreateInvoiceService, CreateInvoiceService>();
        services.AddScoped<IGenerateMonthlyStatementService, GenerateMonthlyStatementService>();
        services.AddScoped<ISendMonthlyStatementService, SendMonthlyStatementService>();
        services.AddScoped<IMonthlyStatementSenderPaymentDetailsProvider, AppUserMonthlyStatementSenderPaymentDetailsProvider>();
        services.AddScoped<IPdfInvoiceImportService, PdfInvoiceImportService>();
        services.AddScoped<IPdfInvoiceTextExtractor, PdfInvoiceTextExtractor>();
        services.AddScoped<IPdfInvoiceProviderDetector, PdfInvoiceProviderDetector>();
        services.AddScoped<IPdfInvoiceParser, TeliaPdfInvoiceParser>();
        services.AddScoped<IPdfInvoiceParser, EnefitPdfInvoiceParser>();
    }

    private static void ConfigureForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            options.KnownIPNetworks.Clear();
            options.KnownIPNetworks.Add(
                new IPNetwork(IPAddress.Parse("172.18.0.0"), 16));
        });
    }
}
