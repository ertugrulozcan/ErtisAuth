using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models.Applications;

/// <summary>
/// Response model carrying the plain application secret. It is only returned once, when the secret is generated
/// (application creation or secret rotation); only its hash is stored.
/// Never pass this model to events or caches, the secret would leak with it.
/// </summary>
public class ApplicationWithSecret : Application
{
	#region Properties
	
	[JsonPropertyName("secret")]
	[BsonIgnore]
	public string Secret { get; }
	
	#endregion
	
	#region Constructors
	
	[SetsRequiredMembers]
	public ApplicationWithSecret(Application application, string secret)
	{
		this.Id = application.Id;
		this.MembershipId = application.MembershipId;
		this.Name = application.Name;
		this.Slug = application.Slug;
		this.Role = application.Role;
		this.Permissions = application.Permissions;
		this.Forbidden = application.Forbidden;
		this.Sys = application.Sys;
		this.Secret = secret;
	}
	
	#endregion
}
