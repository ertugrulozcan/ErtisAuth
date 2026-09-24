using ErtisAuth.Core.Models.Mailing;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace ErtisAuth.Dao.Serialization;

public static class MailProviderDiscriminatorConvention
{
	#region Methods
	
	public static void Register()
	{
		var convention = new ScalarDiscriminatorConvention("type");
		
		BsonSerializer.RegisterDiscriminatorConvention(typeof(IMailProvider), convention);
		BsonSerializer.RegisterDiscriminatorConvention(typeof(SmtpServerProvider), convention);
		BsonSerializer.RegisterDiscriminatorConvention(typeof(SendGridProvider), convention);
		BsonSerializer.RegisterDiscriminatorConvention(typeof(MailChimpProvider), convention);
		
		BsonClassMap.RegisterClassMap<SmtpServerProvider>(classMap =>
		{
			classMap.AutoMap();
			classMap.SetDiscriminator(nameof(MailProviderType.SmtpServer));
		});
		
		BsonClassMap.RegisterClassMap<SendGridProvider>(classMap =>
		{
			classMap.AutoMap();
			classMap.SetDiscriminator(nameof(MailProviderType.SendGrid));
		});
		
		BsonClassMap.RegisterClassMap<MailChimpProvider>(classMap =>
		{
			classMap.AutoMap();
			classMap.SetDiscriminator(nameof(MailProviderType.MailChimp));
		});
	}
    
    #endregion
}