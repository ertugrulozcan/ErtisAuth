using System.Threading.Tasks;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IRepositoryBase
{
	Task CreateIndexesAsync();
}