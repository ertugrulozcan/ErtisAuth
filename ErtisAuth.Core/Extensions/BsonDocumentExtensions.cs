using System.Text.Json;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace ErtisAuth.Core.Extensions;

public static class BsonDocumentExtensions
{
	#region Methods
	
	public static dynamic? ToDynamicObject(this BsonDocument bsonDocument)
	{
		var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson };
		var json = bsonDocument.ToJson(jsonWriterSettings);
		json = ClearObjectIds(json);
		return JsonSerializer.Deserialize<dynamic>(json);
	}
	
	private static string ClearObjectIds(string json)
	{
		return new Regex(@"ObjectId\((.[a-f0-9]{24}.)\)", RegexOptions.Compiled).Replace(json, s => s.Groups[1].Value);
	}
	
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