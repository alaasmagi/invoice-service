using Application;
using Application.Allocation;
using Base.Contracts.DataAccess;
using Base.Contracts.DTO;
using Base.Contracts.Message;
using Base.Keycloak.Authentication;
using Base.Keycloak.Events;
using Base.Message.RabbitMQ;
using Contracts.Application;
using Contracts.DataAccess;
using Contracts.External;
using DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using DTO.DataAccess.DataAccess.Mapper;
using DTO.DataAccess.Web.DTO;
using DTO.DataAccess.Web.Mapper;
using Infrastructure.Messaging;
using Infrastructure.Storage;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Web.Authentication;
using Web.Filters;
using Web.Helpers;
using Web.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using Web.Mapping;
using Web.Services;
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
        var invoiceMessagingOptions = RequiredConfiguration.InvoiceMessagingOptions();
        var storageOptions = RequiredConfiguration.StorageOptions();

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(appDbConnectionString));
        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<AppDbContext>());
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        builder.Services.AddHealthChecks();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddKeycloakOidc(keycloakOptions)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);
        builder.Services.AddApiRateLimiting();
        builder.Services.AddAuthorization();

        builder.Services.AddSingleton(invoiceMessagingOptions);
        builder.Services.AddRabbitMq(rabbitMqOptions);
        builder.Services.AddRabbitMqExchangePublisher(
            RabbitMqPublisherKeys.NotificationHub, rabbitMqOptions.ForExchange(invoiceMessagingOptions.NotificationHubExchange));
        builder.Services.AddRabbitMqExchangePublisher(
            RabbitMqPublisherKeys.AiHub, rabbitMqOptions.ForExchange(invoiceMessagingOptions.AiHubExchange));
        builder.Services.AddSingleton<IBaseEventHandler<UserEventContent>, UserEventHandler>();
        // One queue per service: the router dispatches each message by its routing-key source.
        builder.Services.AddRabbitMqRoutingConsumer(invoiceMessagingOptions.Queue, routes => routes
            .Route<UserEventContent>(MessagingContract.IdentitySource)
            .Route<InvoiceParsedContent>(MessagingContract.AiHubSource, MessagingContract.InvoiceParsedAction)
            .Route<InvoiceParseFailedContent>(MessagingContract.AiHubSource, MessagingContract.InvoiceParseFailedAction));
        builder.Services.AddScoped<IBaseEventHandler<InvoiceParsedContent>, InvoiceParsedEventHandler>();
        builder.Services.AddScoped<IBaseEventHandler<InvoiceParseFailedContent>, InvoiceParseFailedEventHandler>();
        builder.Services.AddSingleton<IMonthlyInvoiceEmailPublisher, RabbitMqMonthlyInvoiceEmailPublisher>();
        builder.Services.AddSingleton<IInvoiceParseRequestPublisher, RabbitMqInvoiceParseRequestPublisher>();
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddR2FileStorage(storageOptions);

        builder.Services.AddDataMappers();
        builder.Services.AddWebMappers();
        builder.Services.AddRepositories();
        builder.Services.AddApplicationServices();

        builder.Services.AddScoped<WebDtoMapper>();
        builder.Services.AddScoped<PeriodOverviewService>();
        builder.Services.AddScoped<StatisticsService>();

        builder.Services.AddLocalizationSupport();

        builder.Services.AddScoped<CurrentAppUserViewDataFilter>();
        builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<CurrentAppUserViewDataFilter>();
                options.ModelMetadataDetailsProviders.Add(new RequiredMessageMetadataProvider());
                var formFactory = options.ValueProviderFactories.OfType<FormValueProviderFactory>().FirstOrDefault();
                var formIndex = formFactory == null ? 0 : options.ValueProviderFactories.IndexOf(formFactory);
                if (formFactory != null) options.ValueProviderFactories.RemoveAt(formIndex);
                options.ValueProviderFactories.Insert(formIndex, new InvariantFormValueProviderFactory());
            })
            .AddViewLocalization()
            .AddDataAnnotationsLocalization(options =>
                options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));
        builder.Services.ConfigureForwardedHeaders();
    }

    private static void AddRabbitMqExchangePublisher(this IServiceCollection services, string key, RabbitMqOptions options)
    {
        services.AddKeyedSingleton<IBaseEventPublisher>(key, (provider, _) => new RabbitMqEventPublisher(
            provider.GetRequiredService<RabbitMqConnectionManager>(),
            options,
            provider.GetRequiredService<ILogger<RabbitMqEventPublisher>>()));
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
        services.AddScoped<IMapper<AllocationRule, AllocationRuleEntity>, AllocationRuleEntityMapper>();
        services.AddScoped<IMapper<AllocationRuleShare, AllocationRuleShareEntity>, AllocationRuleShareEntityMapper>();
        services.AddScoped<IMapper<InvoiceDocument, InvoiceDocumentEntity>, InvoiceDocumentEntityMapper>();
        services.AddScoped<IMapper<InvoiceDocumentRow, InvoiceDocumentRowEntity>, InvoiceDocumentRowEntityMapper>();
        services.AddScoped<IMapper<ApiKey, ApiKeyEntity>, ApiKeyEntityMapper>();
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
        services.AddScoped<IMapper<AllocationRuleDto, AllocationRule>, AllocationRuleDtoMapper>();
        services.AddScoped<IMapper<AllocationRuleShareDto, AllocationRuleShare>, AllocationRuleShareDtoMapper>();
        services.AddScoped<IMapper<InvoiceDocumentDto, InvoiceDocument>, InvoiceDocumentDtoMapper>();
        services.AddScoped<IMapper<InvoiceDocumentDetailsDto, InvoiceDocument>, InvoiceDocumentDetailsDtoMapper>();
        services.AddScoped<IMapper<ApiKeyDto, ApiKey>, ApiKeyDtoMapper>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IAddressContactRepository, AddressContactRepository>();
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IPublishedInvoiceEmailRepository, PublishedInvoiceEmailRepository>();
        services.AddScoped<IProcessedInboxMessageRepository, ProcessedInboxMessageRepository>();
        services.AddScoped<IIdentityUserProjection, IdentityUserProjection>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceAllocationRepository, InvoiceAllocationRepository>();
        services.AddScoped<IMonthlyStatementRepository, MonthlyStatementRepository>();
        services.AddScoped<IMonthlyStatementLineRepository, MonthlyStatementLineRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IAllocationRuleRepository, AllocationRuleRepository>();
        services.AddScoped<IInvoiceDocumentRepository, InvoiceDocumentRepository>();
        services.AddScoped<IInvoiceDocumentRowRepository, InvoiceDocumentRowRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IBaseUow, DataAccessUow>();
    }

    private static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICreateAddressService, CreateAddressService>();
        services.AddScoped<ICreateContactService, CreateContactService>();
        services.AddScoped<ICreateInvoiceService, CreateInvoiceService>();
        services.AddScoped<IGenerateMonthlyStatementService, GenerateMonthlyStatementService>();
        services.AddScoped<EqualAllocationStrategy>();
        services.AddScoped<IAllocationStrategy>(provider => provider.GetRequiredService<EqualAllocationStrategy>());
        services.AddScoped<IAllocationStrategy, PercentageAllocationStrategy>();
        services.AddScoped<IAllocationStrategy, FixedAllocationStrategy>();
        services.AddScoped<IAllocationStrategy, ManualAllocationStrategy>();
        services.AddScoped<IInvoiceSplitCalculator, InvoiceSplitCalculator>();
        services.AddScoped<IAllocationRuleValidator, AllocationRuleValidator>();
        services.AddScoped<ISendMonthlyStatementService, SendMonthlyStatementService>();
        services.AddScoped<ISendPeriodInvoiceEmailsService, SendPeriodInvoiceEmailsService>();
        services.AddScoped<IMonthlyStatementSenderPaymentDetailsProvider, AppUserMonthlyStatementSenderPaymentDetailsProvider>();
        services.AddScoped<IParsedInvoiceImporter, ParsedInvoiceImporter>();
        services.AddScoped<IInvoiceDocumentService, InvoiceDocumentService>();
        services.AddScoped<IInvoiceParseResultService, InvoiceParseResultService>();
        services.AddScoped<IInvoiceDocumentReviewService, InvoiceDocumentReviewService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
    }

    private static void AddLocalizationSupport(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddMemoryCache();
        services.AddSingleton<UserPreferencesProvider>();
        services.AddScoped<Formatter>();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var formatCultures = LocalizationSettings.FormatCultures
                .Concat(LocalizationSettings.Languages)
                .Select(name => new CultureInfo(name))
                .ToList();
            options.DefaultRequestCulture = new RequestCulture(
                LocalizationSettings.FormatCultureFor(null, LocalizationSettings.DefaultLanguage),
                LocalizationSettings.DefaultLanguage);
            options.SupportedCultures = formatCultures;
            options.SupportedUICultures = LocalizationSettings.Languages.Select(name => new CultureInfo(name)).ToList();
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;
            // Signed-in users get their own settings; visitors get their browser language.
            options.RequestCultureProviders =
            [
                new UserPreferencesCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        // Model binding messages are produced at request time, so they translate with the request's UI culture.
        services.AddOptions<MvcOptions>().Configure<IStringLocalizerFactory>((options, factory) =>
        {
            var t = factory.Create(typeof(SharedResource));
            var messages = options.ModelBindingMessageProvider;
            // Empty "Select …" placeholder options post an empty value for non-nullable ids and amounts.
            messages.SetValueMustNotBeNullAccessor(_ => t["This field is required."]);
            messages.SetMissingBindRequiredValueAccessor(_ => t["This field is required."]);
            messages.SetAttemptedValueIsInvalidAccessor((value, field) => t["The value '{0}' is not valid for {1}.", value, field]);
            messages.SetValueIsInvalidAccessor(value => t["The value '{0}' is invalid.", value]);
            messages.SetValueMustBeANumberAccessor(field => t["The field {0} must be a number.", field]);
            messages.SetNonPropertyValueMustBeANumberAccessor(() => t["The field must be a number."]);
        });
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
