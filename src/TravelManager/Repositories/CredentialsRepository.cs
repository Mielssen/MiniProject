namespace TravelManager.Repositories
{
    using TravelManager.Models;
    using Microsoft.EntityFrameworkCore;
    public class CredentialsRepository
    {
        private readonly TravelmanagerContext _context;

        public CredentialsRepository(TravelmanagerContext context)
        {
            _context = context;
        }

        public void Register(Credential credential)
        {
            _context.Credentials.Add(credential);
            _context.SaveChanges();
        }
        public Credential? GetById(int credentialId)
        {
            return _context.Credentials
                .AsNoTracking()
                .FirstOrDefault(x => x.CredentialId == credentialId);
        }

        public Credential? GetByEmail(string email)
        {
            return _context.Credentials.AsNoTracking()
                .FirstOrDefault(x => x.Email == email);
        }

        public bool EmailExists(string email)
        {
            return _context.Credentials.Any(x => x.Email == email);
        }

        public void AddUserDetail(UserDetail userDetail)
        {
            _context.UserDetails.Add(userDetail);
            _context.SaveChanges();
        }
        public UserDetail? GetUserDetail(int credentialId)
        {
            return _context.UserDetails
                .FirstOrDefault(x => x.CredentialId == credentialId);
        }

        public void UpdateUserDetail(UserDetail detail)
        {
            _context.UserDetails.Update(detail);
            _context.SaveChanges();
        }
    }
}
