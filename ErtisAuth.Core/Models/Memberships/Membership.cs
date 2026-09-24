using System.Text;
using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Cryptography;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Mailing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.Core.Models.Memberships;

public class Membership : ResourceBase, IHasSysInfo
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	[BsonElement("name")]
	public required string Name { get; set; }
	
	[JsonProperty("slug")]
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
	
	[JsonProperty("expires_in")]
	[JsonPropertyName("expires_in")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[BsonElement("expires_in")]
	[BsonIgnoreIfDefault]
	public int ExpiresIn { get; set; }
	
	[JsonProperty("scoped_token_expires_in")]
	[JsonPropertyName("scoped_token_expires_in")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[BsonElement("scoped_token_expires_in")]
	[BsonIgnoreIfDefault]
	public int ScopedTokenExpiresIn { get; set; }
	
	[JsonProperty("refresh_token_expires_in")]
	[JsonPropertyName("refresh_token_expires_in")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[BsonElement("refresh_token_expires_in")]
	[BsonIgnoreIfDefault]
	public int RefreshTokenExpiresIn { get; set; }
	
	[JsonProperty("reset_password_token_expires_in")]
	[JsonPropertyName("reset_password_token_expires_in")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("reset_password_token_expires_in")]
	[BsonIgnoreIfNull]
	public int? ResetPasswordTokenExpiresIn { get; set; }
	
	[JsonProperty("secret_key")]
	[JsonPropertyName("secret_key")]
	[BsonElement("secret_key")]
	public required string SecretKey { get; set; }
	
	[JsonProperty("hash_algorithm")]
	[JsonPropertyName("hash_algorithm")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("hash_algorithm")]
	[BsonIgnoreIfNull]
	public string? HashAlgorithm { get; set; }
	
	[JsonProperty("encoding")]
	[JsonPropertyName("encoding")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("encoding")]
	[BsonIgnoreIfNull]
	public string? DefaultEncoding { get; set; }
	
	[JsonProperty("default_language")]
	[JsonPropertyName("default_language")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("default_language")]
	[BsonIgnoreIfNull]
	public string? DefaultLanguage { get; set; }
	
	[JsonProperty("mail_providers")]
	[JsonPropertyName("mail_providers")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("mail_providers")]
	[BsonIgnoreIfNull]
	public IMailProvider[]? MailProviders { get; set; }
	
	[JsonProperty("user_activation")]
	[JsonPropertyName("user_activation")]
	[BsonElement("user_activation")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonRepresentation(BsonType.String)]
	public Status UserActivation { get; set; }
	
	[JsonProperty("code_policy")]
	[JsonPropertyName("code_policy")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("code_policy")]
	[BsonIgnoreIfNull]
	public string? CodePolicy { get; set; }
	
	[JsonProperty("otp_settings")]
	[JsonPropertyName("otp_settings")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("otp_settings")]
	[BsonIgnoreIfNull]
	public OtpSettings? OtpSettings { get; set; }
	
	[JsonProperty("sys")]
	[JsonPropertyName("sys")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("sys")]
	public SysModel? Sys { get; set; }
	
	#endregion
	
	#region Method
	
	public HashAlgorithms GetHashAlgorithm()
	{
		if (string.IsNullOrEmpty(this.HashAlgorithm))
		{
			return Constants.Defaults.DEFAULT_HASH_ALGORITHM;
		}
		
		if (!HashParser.TryParseHashAlgorithm(this.HashAlgorithm, out var algorithm, out _, out _))
		{
			algorithm = Constants.Defaults.DEFAULT_HASH_ALGORITHM;
		}
		
		return algorithm;
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