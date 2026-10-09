namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IRepositoryBase
{
	Task CreateIndexesAsync(CancellationToken cancellationToken = default);
}