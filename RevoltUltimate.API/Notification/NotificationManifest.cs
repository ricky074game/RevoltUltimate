namespace RevoltUltimate.API.Notification
{
    public record NotificationAuthor(string Name, string? Url);

    public class NotificationManifest
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string Version { get; init; } = "1.0.0";
        public NotificationAuthor? Author { get; init; }
        public string? Description { get; init; }
        public string? PreviewImage { get; init; }
        public string Entry { get; init; } = string.Empty;
        public List<string>? Links { get; init; }
        public List<string>? Tags { get; init; }
    }
}