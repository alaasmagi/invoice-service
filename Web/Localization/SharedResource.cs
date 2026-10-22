namespace Web.Localization;

/// <summary>
/// Marker for the single shared string table. Keys are the English texts; translations live in
/// Resources/Localization/SharedResource.{culture}.resx. A missing translation falls back to the key.
/// </summary>
public sealed class SharedResource;
