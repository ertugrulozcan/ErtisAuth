using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Dao.Serialization;

public static class ProviderDiscriminatorConvention
{
	#region Methods
	
	public static void Register()
	{
		var convention = new ScalarDiscriminatorConvention("type");
		
		BsonSerializer.RegisterDiscriminatorConvention(typeof(Provider), convention);
		BsonSerializer.RegisterDiscriminatorConvention(typeof(BaseAppleProvider), convention);
		BsonSerializer.RegisterDiscriminatorConvention(typeof(AppleProvider), convention);
		BsonSerializer.RegisterDiscriminatorConvention(typeof(AppleNativeProvider), convention);
		BsonSerializer.RegisterDiscriminatorConvention(typeof(FacebookProvider), convention);
		BsonSerializer.RegisterDiscriminatorConvention(typeof(GoogleProvider), convention);
		BsonSerializer.RegisterDiscriminatorConvention(typeof(MicrosoftProvider), convention);
		
		BsonClassMap.RegisterClassMap<AppleProvider>(classMap =>
		{
			classMap.AutoMap();
			classMap.SetDiscriminator(nameof(ProviderType.Apple));
		});
		
		BsonClassMap.RegisterClassMap<AppleNativeProvider>(classMap =>
		{
			classMap.AutoMap();
			classMap.SetDiscriminator(nameof(ProviderType.AppleNative));
		});
		
		BsonClassMap.RegisterClassMap<FacebookProvider>(classMap =>
		{
			classMap.AutoMap();
			classMap.SetDiscriminator(nameof(ProviderType.Facebook));
		});
		
		BsonClassMap.RegisterClassMap<GoogleProvider>(classMap =>
		{
			classMap.AutoMap();
			classMap.SetDiscriminator(nameof(ProviderType.Google));
		});
		
		BsonClassMap.RegisterClassMap<MicrosoftProvider>(classMap =>
		{
			classMap.AutoMap();
			classMap.SetDiscriminator(nameof(ProviderType.Microsoft));
		});
	}
	
	#endregion
}