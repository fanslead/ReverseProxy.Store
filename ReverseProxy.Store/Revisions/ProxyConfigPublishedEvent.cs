using System.Text.Json.Serialization;

namespace Yarp.ReverseProxy.Store.Revisions;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ProxyConfigPublishedEvent(
    [property: JsonPropertyName("revisionId")] Guid RevisionId,
    [property: JsonPropertyName("revisionNumber")] long RevisionNumber,
    [property: JsonPropertyName("contentHash")] string ContentHash,
    [property: JsonPropertyName("publishedAt")] DateTimeOffset PublishedAt)
{
    public const string EventType = "proxy-config.published";

    public void Validate()
    {
        if (RevisionId == Guid.Empty)
        {
            throw new InvalidDataException("The published revision ID is empty.");
        }

        if (RevisionNumber < 0)
        {
            throw new InvalidDataException("The published revision number cannot be negative.");
        }

        if (ContentHash.Length != 64 || ContentHash.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidDataException("The published revision content hash is not a SHA-256 hexadecimal value.");
        }

        if (PublishedAt == default)
        {
            throw new InvalidDataException("The published revision timestamp is empty.");
        }
    }
}