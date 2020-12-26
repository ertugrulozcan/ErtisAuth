namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IScopeOwnerAccessor
	{
		string GetRequestOwner();
	}
}