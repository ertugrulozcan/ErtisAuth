using Ertis.Schema.Dynamics;
using MongoDB.Bson;

namespace ErtisAuth.Core.Extensions;

public static class BsonDocumentExtensions
{
	#region Methods
	
	/// <summary>
	/// Maps the document to .NET values instead of a json round trip: no extended json wrappers ({ "$oid": .. }, { "$date": .. })
	/// in the output, the ObjectIds become strings (DynamicObject writes them as strings) and the dates stay dates
	/// </summary>
	public static DynamicObject ToDynamicObject(this BsonDocument bsonDocument)
	{
		return DynamicObject.Create((IDictionary<string, object?>) BsonTypeMapper.MapToDotNetValue(bsonDocument));
	}
	
	// ReSharper disable once UnusedMember.Global
	public static bool IsObjectId(this string id)
	{
		return id != "undefined" && ObjectId.TryParse(id, out _);
	}
	
	public static BsonDocument FixRegexOperators(this BsonDocument query)
	{
		var nodes = new List<BsonElement>();
		foreach (var node in query)
		{
			if (node.Value is BsonRegularExpression regexNode)
			{
				var regex = regexNode.Pattern;
				nodes.Add(new BsonElement(node.Name, new BsonDocument("$regex", BsonValue.Create(regex))));
			}
			else
			{
				nodes.Add(node);
			}
		}
		
		return new BsonDocument(nodes);
	}
	
	#endregion
}