using Amazon.Runtime;
using Amazon.S3;
using Contracts.External;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddR2FileStorage(this IServiceCollection services, StorageOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
            R2FileStorage.CreateClientConfig(options)));
        services.AddSingleton<IFileStorage, R2FileStorage>();

        return services;
    }
}
