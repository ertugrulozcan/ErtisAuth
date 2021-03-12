using ErtisAuth.Abstractions.Services.Interfaces;

namespace ErtisAuth.Tests.MockServices
{
	public class MockScopeOwnerAccessor : IScopeOwnerAccessor
	{
		#region Methods

		public string GetRequestOwner()
		{
			return "nunit";
		}

		#endregion
	}
}