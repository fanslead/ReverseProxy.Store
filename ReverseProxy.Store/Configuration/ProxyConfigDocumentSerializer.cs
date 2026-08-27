using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Yarp.ReverseProxy.Store.Configuration;

public static class ProxyConfigDocumentSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static string SerializeCanonical(ProxyConfigDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var normalizedDocument = new ProxyConfigDocument
        {
            SchemaVersion = document.SchemaVersion,
            Routes = (document.Routes ?? []).OrderBy(route => route.RouteId, StringComparer.Ordinal).ToArray(),
            Clusters = (document.Clusters ?? []).OrderBy(cluster => cluster.ClusterId, StringComparer.Ordinal).ToArray()
        };

        var documentNode = JsonSerializer.SerializeToNode(normalizedDocument, SerializerOptions)
            ?? throw new JsonException("The proxy configuration document could not be serialized.");

        return SortObjectProperties(documentNode).ToJsonString(SerializerOptions);
    }

    public static ProxyConfigDocument Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<ProxyConfigDocument>(json, SerializerOptions)
            ?? throw new JsonException("The proxy configuration document is empty.");
    }

    public static string ComputeHash(ProxyConfigDocument document)
    {
        var canonicalJson = SerializeCanonical(document);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
        return Convert.ToHexStringLower(hash);
    }

    private static JsonNode SortObjectProperties(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            var sortedObject = new JsonObject();
            foreach (var property in jsonObject.OrderBy(property => property.Key, StringComparer.Ordinal))
            {
                sortedObject[property.Key] = property.Value is null
                    ? null
                    : SortObjectProperties(property.Value);
            }

            return sortedObject;
        }

        if (node is JsonArray jsonArray)
        {
            var sortedArray = new JsonArray();
            foreach (var item in jsonArray)
            {
                sortedArray.Add(item is null ? null : SortObjectProperties(item));
            }

            return sortedArray;
        }

        return node.DeepClone();
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = false
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}