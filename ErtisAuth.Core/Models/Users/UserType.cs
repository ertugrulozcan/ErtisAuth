using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using Ertis.Schema.Dynamics;
using Ertis.Schema.Extensions;
using Ertis.Schema.Serialization;
using Ertis.Schema.Types;
using Ertis.Schema.Validation;
using ErtisAuth.Core.Exceptions;

using NewtonsoftJsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftFieldInfoCollectionJsonConverter = Ertis.Schema.Serialization.Legacy.FieldInfoCollectionJsonConverter;

namespace ErtisAuth.Core.Models.Users;

public class UserType : MembershipBoundedResource, IHasSysInfo, ISchema, ICloneable
{
    #region Constants
    
    public const string ORIGIN_USER_TYPE_NAME = "Base User";
    public const string ORIGIN_USER_TYPE_SLUG = "base-user";
    
    private static readonly JsonSerializerOptions PropertiesJsonSerializerOptions = new()
    {
        Converters =
        {
            new FieldInfoJsonConverter(),
            new FieldInfoCollectionJsonConverterFactory()
        }
    };
    
    #endregion
    
    #region Fields
    
    private bool isSealed;
    
    #endregion
    
    #region Properties
    
    [JsonPropertyName("name")]
    [NewtonsoftJsonProperty("name")]
    [BsonElement("name")]
    public required string Name
    {
        get;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw ErtisAuthException.UserTypeNameRequired();
            }
            
            field = value;
        }
    }
    
    [JsonPropertyName("slug")]
    [NewtonsoftJsonProperty("slug")]
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
    
    [JsonPropertyName("description")]
    [NewtonsoftJsonProperty("description")]
    [BsonElement("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("properties")]
    [NewtonsoftJsonProperty("properties")]
    [JsonConverter(typeof(FieldInfoCollectionJsonConverterFactory))]
    [NewtonsoftJsonConverter(typeof(NewtonsoftFieldInfoCollectionJsonConverter))]
    [BsonIgnore]
    public IReadOnlyCollection<IFieldInfo> Properties { get => field ?? new List<IFieldInfo>(); set; }
    
    [JsonIgnore]
    [NewtonsoftJsonIgnore]
    [BsonElement("properties")]
    // ReSharper disable once UnusedMember.Local
    private BsonDocument? PropertiesDocument
    {
        get
        {
            var json = JsonSerializer.Serialize(this.Properties, PropertiesJsonSerializerOptions);
            return MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
        }
        set
        {
            var json = value.ToJson();
            var properties = JsonSerializer.Deserialize<IReadOnlyCollection<IFieldInfo>>(json, PropertiesJsonSerializerOptions);
            if (properties != null)
            {
                this.Properties = properties;
            }
        }
    }
    
    [JsonPropertyName("allowAdditionalProperties")]
    [NewtonsoftJsonProperty("allowAdditionalProperties")]
    [BsonElement("allowAdditionalProperties")]
    public bool AllowAdditionalProperties { get; init; }
    
    [JsonPropertyName("isAbstract")]
    [NewtonsoftJsonProperty("isAbstract")]
    [BsonElement("isAbstract")]
    public bool IsAbstract
    {
        get;
        set
        {
            if (this.isSealed && value)
            {
                throw ErtisAuthException.UserTypeCannotBeBothAbstractAndSealed();
            }
            
            field = value;
        }
    }
    
    [JsonPropertyName("isSealed")]
    [NewtonsoftJsonProperty("isSealed")]
    [BsonElement("isSealed")]
    public bool IsSealed
    {
        get => this.isSealed;
        set
        {
            if (this.IsAbstract && value)
            {
                throw ErtisAuthException.UserTypeCannotBeBothAbstractAndSealed();
            }
            
            this.isSealed = value;
        }
    }
    
    [JsonPropertyName("baseType")]
    [NewtonsoftJsonProperty("baseType")]
    [BsonElement("baseType")]
    public string? BaseUserType { get; set; }
    
    [JsonPropertyName("sys")]
    [NewtonsoftJsonProperty("sys")]
    [BsonElement("sys")]
    public SysModel? Sys { get; set; }
    
    #endregion
    
    #region Methods
    
    public bool ValidateSchema(out Exception? exception)
    {
        this.Validate(out exception);
        return exception == null;
    }
    
    public bool ValidateContent(DynamicObject obj, IValidationContext validationContext)
    {
        return this.ValidateData(obj, validationContext);
    }
    
    #endregion
    
    #region Clone
    
    public object Clone()
    {
        return new UserType
        {
            Id = this.Id,
            Name = this.Name,
            Description = this.Description,
            Properties = this.Properties,
            IsAbstract = this.IsAbstract,
            IsSealed = this.IsSealed,
            AllowAdditionalProperties = this.AllowAdditionalProperties,
            BaseUserType = this.BaseUserType,
            MembershipId = this.MembershipId,
            Sys = this.Sys
        };
    }
    
    #endregion
}