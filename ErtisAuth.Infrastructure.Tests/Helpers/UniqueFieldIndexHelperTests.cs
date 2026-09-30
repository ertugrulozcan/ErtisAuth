using Ertis.Schema.Types;
using Ertis.Schema.Types.CustomTypes;
using Ertis.Schema.Types.Primitives;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Infrastructure.Helpers;
using MongoDB.Bson;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

/// <summary>
/// The unique indexes computed from the user types: which users an index covers, how the value filter depends on the field type,
/// and how an index name is read back.
/// </summary>
public class UniqueFieldIndexHelperTests
{
	#region Constants
	
	private const string MembershipId = "6abc0b364c011b01bdf444ba";
	
	#endregion
	
	#region Helpers
	
	private static UserType NewUserType(string name, string? baseType = null, params IFieldInfo[] properties)
	{
		return new UserType
		{
			Name = name,
			BaseUserType = baseType ?? UserType.ORIGIN_USER_TYPE_SLUG,
			Properties = properties,
			MembershipId = MembershipId
		};
	}
	
	private static StringFieldInfo UniqueString(string name) => new() { Name = name, IsUnique = true };
	
	#endregion
	
	#region Global Indexes
	
	[Fact]
	public void GlobalIndexes_AreTheUniqueFieldsOfTheOriginUserType()
	{
		var indexes = UniqueFieldIndexHelper.GetGlobalIndexes();
		
		Assert.Equal(["ux_email_address", "ux_username"], indexes.Select(x => x.Name).Order());
		Assert.All(indexes, x => Assert.Empty(x.UserTypes));
	}
	
	[Fact]
	public void UniqueFieldsOfTheOriginUserType_HaveNoMembershipIndex()
	{
		var indexes = UniqueFieldIndexHelper.GetMembershipIndexes([NewUserType("Customer", null, UniqueString("email_address"), UniqueString("username"))], MembershipId);
		
		Assert.Empty(indexes);
	}
	
	#endregion
	
	#region Covered User Types
	
	[Fact]
	public void Index_CoversTheDeclaringTypeAndItsDescendants()
	{
		var userTypes = new[]
		{
			NewUserType("Customer", null, UniqueString("code")),
			NewUserType("Premium", "customer", UniqueString("code")),
			NewUserType("Gold", "premium"),
			NewUserType("Employee", null, UniqueString("code"))
		};
		
		var indexes = UniqueFieldIndexHelper.GetMembershipIndexes(userTypes, MembershipId);
		
		Assert.Equal(2, indexes.Count);
		Assert.Contains(indexes, x => x.UserTypes.SequenceEqual(["customer", "gold", "premium"]));
		Assert.Contains(indexes, x => x.UserTypes.SequenceEqual(["employee"]));
		Assert.All(indexes, x => Assert.Equal("code", x.Path));
	}
	
	/// <summary>
	/// Base types may be referred by name as well as by slug.
	/// </summary>
	[Fact]
	public void BaseTypeReferredByName_IsResolved()
	{
		var userTypes = new[]
		{
			NewUserType("Customer Account", null, UniqueString("code")),
			NewUserType("Premium", "Customer Account")
		};
		
		var index = Assert.Single(UniqueFieldIndexHelper.GetMembershipIndexes(userTypes, MembershipId));
		
		Assert.Equal(["customer-account", "premium"], index.UserTypes);
	}
	
	[Fact]
	public void CircularInheritance_DoesNotLoop()
	{
		var userTypes = new[]
		{
			NewUserType("First", "second", UniqueString("code")),
			NewUserType("Second", "first", UniqueString("code"))
		};
		
		var indexes = UniqueFieldIndexHelper.GetMembershipIndexes(userTypes, MembershipId);
		
		Assert.NotEmpty(indexes);
	}
	
	[Fact]
	public void UniqueFieldInAnArrayItemSchema_HasNoIndex()
	{
		var array = new ArrayFieldInfo { Name = "codes", ItemSchema = UniqueString("code") };
		
		var indexes = UniqueFieldIndexHelper.GetMembershipIndexes([NewUserType("Customer", null, array)], MembershipId);
		
		Assert.Empty(indexes);
	}
	
	#endregion
	
	#region Partial Filter
	
	[Fact]
	public void PartialFilter_CoversTheMembershipAndTheUserTypes()
	{
		var index = Assert.Single(UniqueFieldIndexHelper.GetMembershipIndexes([NewUserType("Customer", null, UniqueString("code"))], MembershipId));
		
		var expected = new BsonDocument
		{
			{ "membership_id", MembershipId },
			{ "user_type", new BsonDocument("$in", new BsonArray { "customer" }) },
			{ "code", new BsonDocument { { "$type", "string" }, { "$gt", "" } } }
		};
		Assert.Equal(expected, index.PartialFilterExpression);
	}
	
	[Fact]
	public void PartialFilter_DependsOnTheFieldType()
	{
		var userType = NewUserType("Customer", null,
			new EmailAddressFieldInfo { Name = "backup_email", IsUnique = true },
			new IntegerFieldInfo { Name = "number", IsUnique = true },
			new BooleanFieldInfo { Name = "flag", IsUnique = true });
		
		var indexes = UniqueFieldIndexHelper.GetMembershipIndexes([userType], MembershipId).ToDictionary(x => x.Path);
		
		Assert.Equal(new BsonDocument { { "$type", "string" }, { "$gt", "" } }, indexes["backup_email"].PartialFilterExpression["backup_email"]);
		Assert.Equal(new BsonDocument("$type", "number"), indexes["number"].PartialFilterExpression["number"]);
		Assert.Equal(new BsonDocument("$type", "bool"), indexes["flag"].PartialFilterExpression["flag"]);
	}
	
	#endregion
	
	#region Names
	
	/// <summary>
	/// The name stands for the partial filter: the same user types give the same name, another descendant another name.
	/// </summary>
	[Fact]
	public void Name_ChangesWithTheCoveredUserTypes()
	{
		var customer = NewUserType("Customer", null, UniqueString("code"));
		
		var first = Assert.Single(UniqueFieldIndexHelper.GetMembershipIndexes([customer], MembershipId));
		var again = Assert.Single(UniqueFieldIndexHelper.GetMembershipIndexes([customer], MembershipId));
		var withChild = Assert.Single(UniqueFieldIndexHelper.GetMembershipIndexes([customer, NewUserType("Premium", "customer")], MembershipId));
		
		Assert.Equal(first.Name, again.Name);
		Assert.NotEqual(first.Name, withChild.Name);
		Assert.StartsWith(UniqueFieldIndexHelper.GetMembershipIndexNamePrefix(MembershipId), first.Name);
	}
	
	[Theory]
	[InlineData("ux_email_address", null, "email_address")]
	[InlineData("ux_username", null, "username")]
	[InlineData("ux_6abc0b364c011b01bdf444ba_422a3628_code", MembershipId, "code")]
	[InlineData("ux_6abc0b364c011b01bdf444ba_422a3628_address.zip_code", MembershipId, "address.zip_code")]
	public void IndexName_IsParsed(string name, string? membershipId, string path)
	{
		Assert.True(UniqueFieldIndexHelper.TryParseIndexName(name, out var parsedMembershipId, out var parsedPath));
		Assert.Equal(membershipId, parsedMembershipId);
		Assert.Equal(path, parsedPath);
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData("email_address_1")]
	[InlineData("ux_firstname")]
	[InlineData("ux_6abc0b364c011b01bdf444ba_code")]
	public void OtherIndexName_IsNotParsed(string? name)
	{
		Assert.False(UniqueFieldIndexHelper.TryParseIndexName(name, out _, out _));
	}
	
	[Fact]
	public void ComputedName_IsParsedBack()
	{
		var field = new ObjectFieldInfo([UniqueString("zip_code")]) { Name = "address" };
		var index = Assert.Single(UniqueFieldIndexHelper.GetMembershipIndexes([NewUserType("Customer", null, field)], MembershipId));
		
		Assert.True(UniqueFieldIndexHelper.TryParseIndexName(index.Name, out var membershipId, out var path));
		Assert.Equal(MembershipId, membershipId);
		Assert.Equal("address.zip_code", path);
		Assert.Equal(index.Path, path);
	}
	
	#endregion
}
