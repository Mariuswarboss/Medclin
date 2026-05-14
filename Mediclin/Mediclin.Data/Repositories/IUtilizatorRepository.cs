using Mediclin.Data.Models;

namespace Mediclin.Data.Repositories;

public interface IUtilizatorRepository : IRepository<Utilizator>
{
    Task<Utilizator?> GetByEmailAsync(string email);
    Task UpdateUltimLoginAsync(int id);
    Task<bool> ExistsEmailAsync(string email);
    Task SetActivAsync(int id, bool activ);
    Task UpdateParolaHashAsync(int id, string parolaHash);
}
