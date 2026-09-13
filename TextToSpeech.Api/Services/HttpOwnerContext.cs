using System.Security.Claims;
using TextToSpeech.Core.Interfaces;

namespace TextToSpeech.Api.Services;

public sealed class HttpOwnerContext(IHttpContextAccessor httpContextAccessor) : IOwnerContext
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string GetOwnerId()
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HttpContext.");

        var ownerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new UnauthorizedAccessException("Missing owner id claim.");
        }

        return ownerId;
    }
}
