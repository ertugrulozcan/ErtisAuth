using MongoDB.Bson.Serialization;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Dao.Serialization;

/// <summary>
/// BSON element names of the users' connected accounts (same as the JSON names).
/// </summary>
public static class ProviderAccountInfoClassMap
{
	#region Methods
	
	public static void Register()
	{
		if (BsonClassMap.IsClassMapRegistered(typeof(ProviderAccountInfo)))
		{
			return;
		}
		
		BsonClassMap.RegisterClassMap<ProviderAccountInfo>(classMap =>
		{
			classMap.AutoMap();
			classMap.MapMember(x => x.Provider).SetElementName("provider");
			classMap.MapMember(x => x.Slug).SetElementName("slug").SetIgnoreIfNull(true);
			classMap.MapMember(x => x.UserId).SetElementName("user_id");
			classMap.MapMember(x => x.Token).SetElementName("token");
		});
	}
	
	#endregion
}