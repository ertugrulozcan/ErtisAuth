using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Ertis.Schema.Extensions;
using Ertis.Schema.Types;
using Ertis.Schema.Types.Primitives;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Infrastructure.Services;
using MongoDB.Bson;

namespace ErtisAuth.Infrastructure.Helpers;

/// <summary>
/// Computes the unique indexes of the users collection from the user types (isUnique fields).
/// <list type="bullet">
/// <item>The unique fields of the origin user type (email_address, username) are unique per membership among all users:
/// one index for all memberships, named ux_&lt;path&gt;.</item>
/// <item>Any other unique field is unique per membership among the users of the user type declaring it and its descendants:
/// one index per membership and declaring type, named ux_&lt;membershipId&gt;_&lt;hash&gt;_&lt;path&gt;. The hash stands for the
/// partial filter (the covered user types), so the index is replaced by another one when the covered user types change.</item>
/// </list>
/// Unique fields in array item schemas are not indexed (the schema validation rejects them anyway).
/// </summary>
public static partial class UniqueFieldIndexHelper
{
	#region Constants
	
	public const string IndexNamePrefix = "ux_";
	
	#endregion
	
	#region Fields
	
	private static string[]? globalPaths;
	
	#endregion
	
	#region Properties
	
	/// <summary>
	/// The unique fields of the origin user type, which all user types inherit.
	/// </summary>
	private static string[] GlobalPaths => globalPaths ??= GetUniquePaths(UserTypeService.OriginUserType).Select(x => x.Path).ToArray();
	
	#endregion
	
	#region Methods
	
	public static string GetMembershipIndexNamePrefix(string membershipId) => $"{IndexNamePrefix}{membershipId}_";
	
	public static bool IsGlobalPath(string path) => GlobalPaths.Contains(path);
	
	public static IReadOnlyList<UniqueFieldIndex> GetGlobalIndexes()
	{
		return GlobalPaths
			.Select(path => new UniqueFieldIndex($"{IndexNamePrefix}{path}", path, [], new BsonDocument(path, GetStringFilter())))
			.ToArray();
	}
	
	/// <summary>
	/// The indexes of the membership's own unique fields, from all user types of the membership.
	/// </summary>
	public static IReadOnlyList<UniqueFieldIndex> GetMembershipIndexes(string membershipId, IEnumerable<UserType> userTypes)
	{
		var genealogy = new UserTypeGenealogy(userTypes);
		
		// The declaring type of a unique field is the farthest ancestor which declares it unique too
		var declarations = new Dictionary<(string DeclaringType, string Path), IFieldInfo>();
		foreach (var userType in genealogy.UserTypes)
		{
			foreach (var (path, fieldInfo) in GetUniquePaths(userType).Where(x => !IsGlobalPath(x.Path)))
			{
				var declaringType = userType;
				var declaringFieldInfo = fieldInfo;
				foreach (var ancestor in genealogy.GetAncestors(userType))
				{
					var ancestorFieldInfo = GetUniquePaths(ancestor).FirstOrDefault(x => x.Path == path).FieldInfo;
					if (ancestorFieldInfo != null)
					{
						declaringType = ancestor;
						declaringFieldInfo = ancestorFieldInfo;
					}
				}
				
				declarations.TryAdd((declaringType.Slug, path), declaringFieldInfo);
			}
		}
		
		var indexes = new List<UniqueFieldIndex>();
		foreach (var ((declaringType, path), fieldInfo) in declarations)
		{
			var coveredUserTypes = genealogy.UserTypes
				.Where(x => x.Slug == declaringType || genealogy.GetAncestors(x).Any(y => y.Slug == declaringType))
				.Select(x => x.Slug)
				.Distinct()
				.Order(StringComparer.Ordinal)
				.ToArray();
			
			var partialFilterExpression = new BsonDocument
			{
				{ "membership_id", membershipId },
				{ "user_type", new BsonDocument("$in", new BsonArray(coveredUserTypes)) },
				{ path, GetTypeFilter(fieldInfo) }
			};
			
			var name = $"{GetMembershipIndexNamePrefix(membershipId)}{GetHash(partialFilterExpression)}_{path}";
			indexes.Add(new UniqueFieldIndex(name, path, coveredUserTypes, partialFilterExpression));
		}
		
		return indexes;
	}
	
	/// <summary>
	/// The field path of a unique index by its name, and the membership for the index of a membership's own unique field.
	/// </summary>
	public static bool TryParseIndexName(string? indexName, out string? membershipId, out string path)
	{
		membershipId = null;
		path = string.Empty;
		if (string.IsNullOrEmpty(indexName) || !indexName.StartsWith(IndexNamePrefix, StringComparison.Ordinal))
		{
			return false;
		}
		
		var globalPath = indexName[IndexNamePrefix.Length..];
		if (IsGlobalPath(globalPath))
		{
			path = globalPath;
			return true;
		}
		
		var match = MembershipIndexNameRegex().Match(indexName);
		if (match.Success)
		{
			membershipId = match.Groups["membership"].Value;
			path = match.Groups["path"].Value;
			return true;
		}
		
		return false;
	}
	
	private static IEnumerable<(string Path, IFieldInfo FieldInfo)> GetUniquePaths(UserType userType)
	{
		return userType.GetUniqueProperties()
			.Where(x => !x.IsAnArrayItem(out _))
			.Select(x => (x.GetSelfPath(userType), x));
	}
	
	/// <summary>
	/// Users without a value (missing, null or an empty string) are not covered, like in the uniqueness check of the user service.
	/// </summary>
	private static BsonDocument GetTypeFilter(IFieldInfo fieldInfo)
	{
		return fieldInfo switch
		{
			StringFieldInfo => GetStringFilter(),
			IntegerFieldInfo or FloatFieldInfo => new BsonDocument("$type", "number"),
			BooleanFieldInfo => new BsonDocument("$type", "bool"),
			_ => new BsonDocument("$type", new BsonArray { "string", "number", "bool" })
		};
	}
	
	private static BsonDocument GetStringFilter()
	{
		return new BsonDocument
		{
			{ "$type", "string" },
			{ "$gt", string.Empty }
		};
	}
	
	private static string GetHash(BsonDocument partialFilterExpression)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(partialFilterExpression.ToJson()));
		return Convert.ToHexStringLower(hash)[..8];
	}
	
	[GeneratedRegex("^ux_(?<membership>[^_]+)_(?<hash>[0-9a-f]{8})_(?<path>.+)$")]
	private static partial Regex MembershipIndexNameRegex();
	
	#endregion
	
	#region Nested Types
	
	/// <summary>
	/// The user types of a membership with their ancestors (base types referred by slug or name).
	/// The origin user type is not an ancestor here: its unique fields have their own indexes.
	/// </summary>
	private sealed class UserTypeGenealogy
	{
		private readonly Dictionary<string, UserType> _userTypesBySlug;
		
		private readonly Dictionary<string, UserType> _userTypesByName;
		
		public IReadOnlyCollection<UserType> UserTypes => this._userTypesBySlug.Values;
		
		public UserTypeGenealogy(IEnumerable<UserType> userTypes)
		{
			var list = userTypes.Where(x => x.Slug != UserType.ORIGIN_USER_TYPE_SLUG).ToArray();
			this._userTypesBySlug = list.GroupBy(x => x.Slug).ToDictionary(x => x.Key, x => x.Last());
			this._userTypesByName = list.Where(x => !string.IsNullOrEmpty(x.Name)).GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.Last());
		}
		
		/// <summary>
		/// From the parent to the farthest ancestor.
		/// </summary>
		public IEnumerable<UserType> GetAncestors(UserType userType)
		{
			var visited = new HashSet<string> { userType.Slug };
			var current = this.Find(userType.BaseUserType);
			while (current != null && visited.Add(current.Slug))
			{
				yield return current;
				current = this.Find(current.BaseUserType);
			}
		}
		
		private UserType? Find(string? slugOrName)
		{
			if (string.IsNullOrEmpty(slugOrName))
			{
				return null;
			}
			
			return this._userTypesBySlug.GetValueOrDefault(slugOrName) ?? this._userTypesByName.GetValueOrDefault(slugOrName);
		}
	}
	
	#endregion
}

/// <summary>
/// A unique index on { membership_id, path } of the users collection.
/// </summary>
/// <param name="UserTypes">The covered user types; empty when all users of the membership are covered</param>
public sealed record UniqueFieldIndex(string Name, string Path, string[] UserTypes, BsonDocument PartialFilterExpression);
