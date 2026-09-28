using System.Text.Json.Serialization;
using ErtisAuth.WebAPI.Models.Applications;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Setup;

/// <summary>
/// The setup request. Not the domain models: the membership secret is generated when omitted, the user always gets the
/// administrator role and the new membership.
/// </summary>
public class SetupModel
{
	#region Properties
	
	[JsonProperty("membership")]
	[JsonPropertyName("membership")]
	public SetupMembershipModel? Membership { get; set; }
	
	[JsonProperty("user")]
	[JsonPropertyName("user")]
	public SetupUserModel? User { get; set; }
	
	[JsonProperty("application")]
	[JsonPropertyName("application")]
	public CreateApplicationFormModel? Application { get; set; }
	
	#endregion
}

public class SetupMembershipModel
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonProperty("expires_in")]
	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }
	
	[JsonProperty("refresh_token_expires_in")]
	[JsonPropertyName("refresh_token_expires_in")]
	public int RefreshTokenExpiresIn { get; set; }
	
	[JsonProperty("hash_algorithm")]
	[JsonPropertyName("hash_algorithm")]
	public string? HashAlgorithm { get; set; }
	
	[JsonProperty("encoding")]
	[JsonPropertyName("encoding")]
	public string? DefaultEncoding { get; set; }
	
	/// <summary>
	/// Generated when omitted.
	/// </summary>
	[JsonProperty("secret_key")]
	[JsonPropertyName("secret_key")]
	public string? SecretKey { get; set; }
	
	#endregion
}

public class SetupUserModel
{
	#region Properties
	
	[JsonProperty("username")]
	[JsonPropertyName("username")]
	public string? Username { get; set; }
	
	[JsonProperty("firstname")]
	[JsonPropertyName("firstname")]
	public string? FirstName { get; set; }
	
	[JsonProperty("lastname")]
	[JsonPropertyName("lastname")]
	public string? LastName { get; set; }
	
	[JsonProperty("email_address")]
	[JsonPropertyName("email_address")]
	public string? EmailAddress { get; set; }
	
	[JsonProperty("password")]
	[JsonPropertyName("password")]
	public string? Password { get; set; }
	
	[JsonProperty("user_type")]
	[JsonPropertyName("user_type")]
	public string? UserType { get; set; }
	
	#endregion
}
