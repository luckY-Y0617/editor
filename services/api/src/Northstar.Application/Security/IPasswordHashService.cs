using Northstar.Domain.Users;

namespace Northstar.Application.Security;

public interface IPasswordHashService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string passwordHash, string password);
}
