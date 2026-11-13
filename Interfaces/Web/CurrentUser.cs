using System.Security.Claims;
using Qwek.Applications.Ports;

namespace Qwek.Interfaces.Web;

public class CurrentUser : ICurrentUser
{

    private readonly IHttpContextAccessor _httpContextAccessor; 

    public CurrentUser(IHttpContextAccessor httpContextAccessor) => 
        _httpContextAccessor = httpContextAccessor;

    public string? UserId => 
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier); 
    
    public bool IsAuthenticated => 
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

}
