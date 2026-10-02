using System.Text.Json.Serialization;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Memberships;

/// <summary>
/// The create request of a membership; the id is generated.
/// </summary>
public class CreateMembershipFormModel
{
	#region Properties
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }
	
	[JsonPropertyName("scoped_token_expires_in")]
	public int ScopedTokenExpiresIn { get; set; }
	
	[JsonPropertyName("refresh_token_expires_in")]
	public int RefreshTokenExpiresIn { get; set; }
	
	[JsonPropertyName("reset_password_token_expires_in")]
	public int? ResetPasswordTokenExpiresIn { get; set; }
	
	[JsonPropertyName("secret_key")]
	public string? SecretKey { get; set; }
	
	[JsonPropertyName("hash_algorithm")]
	public string? HashAlgorithm { get; set; }
	
	[JsonPropertyName("encoding")]
	public string? DefaultEncoding { get; set; }
	
	[JsonPropertyName("default_language")]
	public string? DefaultLanguage { get; set; }
	
	[JsonPropertyName("mail_providers")]
	public IMailProvider[]? MailProviders { get; set; }
	
	[JsonPropertyName("user_activation")]
	[JsonConverter(typeof(EnumMemberJsonConverter<Status>))]
	public Status UserActivation { get; set; }
	
	[JsonPropertyName("code_policy")]
	public string? CodePolicy { get; set; }
	
	[JsonPropertyName("otp_settings")]
	public OtpSettings? OtpSettings { get; set; }
	
	#endregion
}