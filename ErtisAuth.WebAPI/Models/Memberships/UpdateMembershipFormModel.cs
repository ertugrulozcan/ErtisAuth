using System.Text.Json.Serialization;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Serialization;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.WebAPI.Models.Memberships;

/// <summary>
/// The update request of a membership; the id comes from the route (an id in the body is ignored).
/// Empty fields keep their current values where the membership service does so (e.g. secret_key, hash_algorithm).
/// </summary>
public class UpdateMembershipFormModel
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
	
	[JsonProperty("scoped_token_expires_in")]
	[JsonPropertyName("scoped_token_expires_in")]
	public int ScopedTokenExpiresIn { get; set; }
	
	[JsonProperty("refresh_token_expires_in")]
	[JsonPropertyName("refresh_token_expires_in")]
	public int RefreshTokenExpiresIn { get; set; }
	
	[JsonProperty("reset_password_token_expires_in")]
	[JsonPropertyName("reset_password_token_expires_in")]
	public int? ResetPasswordTokenExpiresIn { get; set; }
	
	[JsonProperty("secret_key")]
	[JsonPropertyName("secret_key")]
	public string? SecretKey { get; set; }
	
	[JsonProperty("hash_algorithm")]
	[JsonPropertyName("hash_algorithm")]
	public string? HashAlgorithm { get; set; }
	
	[JsonProperty("encoding")]
	[JsonPropertyName("encoding")]
	public string? DefaultEncoding { get; set; }
	
	[JsonProperty("default_language")]
	[JsonPropertyName("default_language")]
	public string? DefaultLanguage { get; set; }
	
	[JsonProperty("mail_providers")]
	[JsonPropertyName("mail_providers")]
	public IMailProvider[]? MailProviders { get; set; }
	
	[JsonProperty("user_activation")]
	[JsonPropertyName("user_activation")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[JsonConverter(typeof(EnumMemberJsonConverter<Status>))]
	public Status UserActivation { get; set; }
	
	[JsonProperty("code_policy")]
	[JsonPropertyName("code_policy")]
	public string? CodePolicy { get; set; }
	
	[JsonProperty("otp_settings")]
	[JsonPropertyName("otp_settings")]
	public OtpSettings? OtpSettings { get; set; }
	
	// LEGACY-APP-SECRET: temporary switch, remove after all applications are migrated to their own secrets
	[JsonProperty("allow_membership_secret_for_applications")]
	[JsonPropertyName("allow_membership_secret_for_applications")]
	public bool? AllowMembershipSecretForApplications { get; set; }
	
	#endregion
}