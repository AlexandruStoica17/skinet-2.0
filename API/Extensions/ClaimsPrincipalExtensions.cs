using System.Security.Claims;

namespace API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string RetrieveEmailFromPrincipal(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Email);
        }

        // --- ADAUGĂ ACEASTĂ METODĂ PENTRU SIGNALR ---
        public static string GetUsername(this ClaimsPrincipal user)
        {
            // Identity folosește de obicei ClaimTypes.Name pentru username (UniqueName în JWT)
            return user.FindFirst(ClaimTypes.Name)?.Value 
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}