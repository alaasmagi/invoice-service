namespace Infrastructure.Storage;

public class StorageOptions
{
    public string Endpoint { get; init; } = default!;

    public string AccessKeyId { get; init; } = default!;

    public string SecretAccessKey { get; init; } = default!;

    public string BucketName { get; init; } = default!;
}
