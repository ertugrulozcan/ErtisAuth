namespace ErtisAuth.Abstractions.Services
{
	public interface IScopeOwnerAccessor
	{
		string GetRequestOwner();
	}
}