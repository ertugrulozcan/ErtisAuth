using System.Text.Json.Serialization;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Integrations.OAuth.Core;

/// <summary>
/// A provider account connected to a user (users' connected_accounts). The BSON element names are mapped in
/// ErtisAuth.Dao (ProviderAccountInfoClassMap), as this project has no MongoDB dependency.
/// </summary>
public class ProviderAccountInfo
{
	#region Properties
	
	/// <summary>
	/// The provider type (Facebook, Google, Microsoft, Apple or AppleNative); the account is matched by type and user id.
	/// </summary>
	[JsonPropertyName("provider")]
	public string? Provider { get; set; }
	
	/// <summary>
	/// The slug of the provider the account signed in with (its token is revoked with that provider).
	/// </summary>
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonPropertyName("user_id")]
	public string? UserId { get; set; }
	
	[JsonPropertyName("token")]
	public string? Token { get; set; }
	
	#endregion
}