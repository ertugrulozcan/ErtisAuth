using System.Text.Json.Serialization;
using Ertis.MongoDB.Queries;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Core.Models.Memberships;

public class MembershipDefaults
{
	#region Properties
	
	[JsonPropertyName("encodings")]
	public required DefaultEncodingInfo[] Encodings { get; set; }
	
	[JsonPropertyName("defaultEncoding")]
	public required string DefaultEncoding { get; set; }
	
	[JsonPropertyName("hashAlgorithms")]
	public required string[] HashAlgorithms { get; set; }
	
	[JsonPropertyName("defaultHashAlgorithm")]
	public required string DefaultHashAlgorithm { get; set; }
	
	[JsonPropertyName("dbLocales")]
	public required TextSearchLanguage[] DbLocales { get; set; }
	
	[JsonPropertyName("defaultDbLocale")]
	public required string DefaultDbLocale { get; set; }
	
	[JsonPropertyName("defaultExpiresIn")]
	public required int DefaultExpiresIn { get; set; }
	
	[JsonPropertyName("defaultRefreshTokenExpiresIn")]
	public required int DefaultRefreshTokenExpiresIn { get; set; }
	
	[JsonPropertyName("defaultResetPasswordTokenExpiresIn")]
	public required int DefaultResetPasswordTokenExpiresIn { get; set; }
	
	[JsonPropertyName("defaultScopedTokenExpiresIn")]
	public required int DefaultScopedTokenExpiresIn { get; set; }
	
	[JsonPropertyName("defaultOtpExpiresIn")]
	public required int DefaultOtpExpiresIn { get; set; }
	
	#endregion
}

public class DefaultEncodingInfo
{
	#region Properties
	
	[JsonPropertyName("name")]
	public required string Name { get; set; }
	
	[JsonPropertyName("displayName")]
	public required string DisplayName { get; set; }
	
	#endregion
}