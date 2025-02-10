using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Users;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Core.Models.Identity
{
	public struct Utilizer
	{
		#region Constants

		public const string UtilizerIdClaimName = "utilizer_id";
		public const string UtilizerTypeClaimName = "utilizer_type";
		public const string UtilizerUsernameClaimName = "utilizer_username";
		public const string UtilizerRoleClaimName = "role";
		public const string MembershipIdClaimName = "membership_id";
		public const string UtilizerTokenClaimName = "access_token";
		public const string UtilizerTokenTypeClaimName = "token_type";
		public const string ScopeClaimName = "scope";
		
		#endregion
		
		#region Properties

		[JsonProperty("id")]
		[JsonPropertyName("id")]
		public string Id { get; set; }
		
		[JsonProperty("type")]
		[JsonPropertyName("type")]
		[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
		[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
		public UtilizerType Type { get; set; }
		
		[JsonProperty("username")]
		[JsonPropertyName("username")]
		public string Username { get; set; }
		
		[JsonProperty("membership_id")]
		[JsonPropertyName("membership_id")]
		public string MembershipId { get; set; }
		
		[JsonProperty("role")]
		[JsonPropertyName("role")]
		public string Role { get; set; }
		
		[JsonProperty("permissions")]
		[JsonPropertyName("permissions")]
		public IEnumerable<string> Permissions { get; set; }
		
		[JsonProperty("forbidden")]
		[JsonPropertyName("forbidden")]
		public IEnumerable<string> Forbidden { get; set; }
		
		[JsonProperty("token")]
		[JsonPropertyName("token")]
		public string Token { get; set; }
		
		[JsonProperty("scopes", NullValueHandling = NullValueHandling.Ignore)]
		[JsonPropertyName("scopes")]
		[System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string[] Scopes { get; set; }
		
		[JsonProperty("tokenType")]
		[JsonPropertyName("tokenType")]
		[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
		[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
		public SupportedTokenTypes TokenType { get; set; }

		#endregion

		#region Implicit Operators

		public static implicit operator Utilizer(User user) => new()
		{
			Id = user.Id,
			Type = UtilizerType.User,
			Username = user.Username,
			MembershipId = user.MembershipId,
			Role = user.Role,
			Permissions = user.Permissions,
			Forbidden = user.Forbidden
		};
		
		public static implicit operator Utilizer(Application application) => new()
		{
			Id = application.Id,
			Type = UtilizerType.Application,
			Username = application.Slug,
			MembershipId = application.MembershipId,
			Role = application.Role,
			Permissions = application.Permissions,
			Forbidden = application.Forbidden
		};

		#endregion

		#region Methods

		public static UtilizerType ParseType(string type)
		{
			if (string.IsNullOrEmpty(type) || string.IsNullOrWhiteSpace(type))
			{
				return UtilizerType.None;
			}
			
			type = type.ToLower();
			type = char.ToUpper(type[0]) + type.Substring(1);
			
			if (Enum.GetNames(typeof(UtilizerType)).Any(x => x == type))
			{
				return (UtilizerType) Enum.Parse(typeof(UtilizerType), type);
			}

			return UtilizerType.None;
		}

		#endregion

		#region Enums

		public enum UtilizerType
		{
			None,
			System,
			User,
			Application
		}

		#endregion
	}
}