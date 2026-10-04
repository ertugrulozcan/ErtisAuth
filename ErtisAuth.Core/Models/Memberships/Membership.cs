using System.Text;
using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Cryptography;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Memberships;

public class Membership : ResourceBase, IHasSlug, IHasSysInfo
{
	#region Properties
	
	[JsonPropertyName("name")]
	[BsonElement("name")]
	public required string Name { get; set; }
	
	[JsonPropertyName("slug")]
	[BsonElement("slug")]
	public string Slug
	{
		get
		{
			if (string.IsNullOrEmpty(field))
			{
				field = Slugifier.Slugify(this.Name, Slugifier.Options.Ignore('_'));
			}
			
			return field;
		}
		set => field = Slugifier.Slugify(value, Slugifier.Options.Ignore('_'));
	}
	
	[JsonPropertyName("expires_in")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[BsonElement("expires_in")]
	[BsonIgnoreIfDefault]
	public int ExpiresIn { get; set; }
	
	[JsonPropertyName("scoped_token_expires_in")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[BsonElement("scoped_token_expires_in")]
	[BsonIgnoreIfDefault]
	public int ScopedTokenExpiresIn { get; set; }
	
	[JsonPropertyName("refresh_token_expires_in")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[BsonElement("refresh_token_expires_in")]
	[BsonIgnoreIfDefault]
	public int RefreshTokenExpiresIn { get; set; }
	
	[JsonPropertyName("reset_password_token_expires_in")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("reset_password_token_expires_in")]
	[BsonIgnoreIfNull]
	public int? ResetPasswordTokenExpiresIn { get; set; }
	
	[JsonPropertyName("secret_key")]
	[BsonElement("secret_key")]
	public required string SecretKey { get; set; }
	
	[JsonPropertyName("hash_algorithm")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("hash_algorithm")]
	[BsonIgnoreIfNull]
	public string? HashAlgorithm { get; set; }
	
	[JsonPropertyName("encoding")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("encoding")]
	[BsonIgnoreIfNull]
	public string? DefaultEncoding { get; set; }
	
	[JsonPropertyName("default_language")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("default_language")]
	[BsonIgnoreIfNull]
	public string? DefaultLanguage { get; set; }
	
	[JsonPropertyName("mail_providers")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("mail_providers")]
	[BsonIgnoreIfNull]
	public IMailProvider[]? MailProviders { get; set; }
	
	[JsonPropertyName("user_activation")]
	[BsonElement("user_activation")]
	[JsonConverter(typeof(EnumMemberJsonConverter<Status>))]
	[BsonSerializer(typeof(EnumMemberBsonSerializer<Status>))]
	public Status UserActivation { get; set; }
	
	[JsonPropertyName("code_policy")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("code_policy")]
	[BsonIgnoreIfNull]
	public string? CodePolicy { get; set; }
	
	[JsonPropertyName("otp_settings")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("otp_settings")]
	[BsonIgnoreIfNull]
	public OtpSettings? OtpSettings { get; set; }
	
	// LEGACY-APP-SECRET: temporary switch, remove after all applications are migrated to their own secrets
	/// <summary>
	/// Allows applications without their own secret to authenticate with the membership secret key.
	/// Missing (null) on memberships created before application secrets existed, which is treated as allowed.
	/// </summary>
	[JsonPropertyName("allow_membership_secret_for_applications")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("allow_membership_secret_for_applications")]
	[BsonIgnoreIfNull]
	public bool? AllowMembershipSecretForApplications { get; set; }
	
	[JsonPropertyName("sys")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("sys")]
	public SysModel? Sys { get; set; }
	
	#endregion
	
	#region Method
	
	public HashAlgorithms GetHashAlgorithm()
	{
		if (!this.TryGetHashAlgorithm(out var algorithm))
		{
			throw ErtisAuthException.MembershipHashAlgorithmInvalid(this.Id, this.HashAlgorithm);
		}

		return algorithm;
	}

	public bool TryGetHashAlgorithm(out HashAlgorithms algorithm)
	{
		if (string.IsNullOrEmpty(this.HashAlgorithm))
		{
			algorithm = default;
			return false;
		}

		return HashParser.TryParseHashAlgorithm(this.HashAlgorithm, out algorithm, out _, out _);
	}

	public bool IsEncodingValid()
	{
		return
			string.IsNullOrEmpty(this.DefaultEncoding) ||
			Encoding.GetEncodings().Any(x => x.Name.Equals(this.DefaultEncoding, StringComparison.InvariantCultureIgnoreCase));
	}
	
	public Encoding GetEncoding()
	{
		if (string.IsNullOrEmpty(this.DefaultEncoding))
		{
			return Constants.Defaults.DEFAULT_ENCODING;
		}
		
		var encoding = Constants.Defaults.DEFAULT_ENCODING;
		var encodings = Encoding.GetEncodings();
		var encodingInfo = encodings.FirstOrDefault(x => x.Name.Equals(this.DefaultEncoding, StringComparison.InvariantCultureIgnoreCase));
		if (encodingInfo != null)
		{
			encoding = encodingInfo.GetEncoding();
		}
		
		return encoding;
	}
	
	#endregion
}