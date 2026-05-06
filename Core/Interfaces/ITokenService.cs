using System.Threading.Tasks;
using Core.Entities.Identity;

namespace Core.Interfaces
{
    public interface ITokenService
    {
        // Am adăugat Task<> aici
        Task<string> CreateToken(AppUser user);
    }
}