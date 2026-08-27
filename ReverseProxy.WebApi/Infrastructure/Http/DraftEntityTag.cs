using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using ReverseProxy.WebApi.Application.Common;

namespace ReverseProxy.WebApi.Infrastructure.Http;

public static class DraftEntityTag
{
    public static string Create(long rowVersion) => $"\"{rowVersion}\"";

    public static long ParseRequired(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(HeaderNames.IfMatch, out StringValues values)
            || values.Count != 1)
        {
            throw new ApiProblemException(
                StatusCodes.Status428PreconditionRequired,
                "if_match_required",
                "A current draft version is required",
                "Send the ETag returned by the latest draft response in the If-Match header.");
        }

        var value = values[0];
        if (value is null
            || !EntityTagHeaderValue.TryParse(value, out var entityTag)
            || entityTag.IsWeak
            || entityTag.Tag.Length < 3
            || !long.TryParse(entityTag.Tag.AsSpan(1, entityTag.Tag.Length - 2), out var rowVersion)
            || rowVersion < 0)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                "invalid_if_match",
                "The If-Match header is invalid",
                "Use the quoted ETag value returned by the latest draft response.");
        }

        return rowVersion;
    }
}