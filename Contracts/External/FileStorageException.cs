namespace Contracts.External;

public class FileStorageException : Exception
{
    public FileStorageException(string operation, string key, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Operation = operation;
        Key = key;
    }

    public string Operation { get; }

    public string Key { get; }
}

public class FileStorageObjectNotFoundException : FileStorageException
{
    public FileStorageObjectNotFoundException(string operation, string key, Exception? innerException = null)
        : base(operation, key, $"No stored object exists for key '{key}'.", innerException)
    {
    }
}
