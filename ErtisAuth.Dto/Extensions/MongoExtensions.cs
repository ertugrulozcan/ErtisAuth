using System.Text.RegularExpressions;
using MongoDB.Bson;

namespace ErtisAuth.Dto.Extensions;

public static partial class MongoExtensions
{
    #region Constants
    
    private static readonly Regex ObjectIdReplacerRegex = ObjectIdRegex();
    
    [GeneratedRegex(@"ObjectId\((.[a-f0-9]{24}.)\)", RegexOptions.Compiled)]
    private static partial Regex ObjectIdRegex();
    
    #endregion
    
    #region Methods
    
    public static dynamic? ToDynamicObject(this BsonDocument bsonDocument)
    {
        var json = ObjectIdReplacerRegex.Replace(bsonDocument.ToJson(), s => s.Groups[1].Value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
    }
    
    #endregion
}