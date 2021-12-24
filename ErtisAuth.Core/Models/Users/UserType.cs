using System.Collections.Generic;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace ErtisAuth.Core.Models.Users
{
    public class UserType
    {
        #region Constants

        public const string PROGENITOR_SCHEMA_NAME = "userbase";
        public const string JSON_SCHEMA_URL = "https://json-schema.org/draft/2020-12/schema";
        public const string JSON_SCHEMA_ID = "https://example.com/product.schema.json";
		
        #endregion

        #region Properties

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("name")] 
        public string Name => Slugifier.Slugify(this.Title);
		
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("type")]
        public string Type => "object";
		
        [JsonProperty("properties")]
        public dynamic Properties { get; set; }
		
        [JsonProperty("required")]
        public string[] RequiredFields { get; set; }

        #endregion

        #region Methods

        public void Validate()
        {
            if (string.IsNullOrEmpty(this.Title))
            {
                throw ErtisAuthException.UserTypeTitleRequired();
            }
            
            if (string.IsNullOrEmpty(this.Description))
            {
                this.Description = this.Title;
            }
            
            if (this.Properties == null)
            {
                throw ErtisAuthException.UserTypePropertiesRequired();
            }
			
            if (this.RequiredFields == null)
            {
                throw ErtisAuthException.UserTypeRequiredFieldsRequired();
            }
        }
        
        public JSchema ConvertToJSchema()
        {
            this.Validate();
			
            var jObject = JObject.FromObject(this);
			
            jObject.Add("$schema", JSON_SCHEMA_URL);
            jObject.Add("$id", JSON_SCHEMA_ID);
            jObject.Add("definitions", JObject.FromObject(new { sys = new SysModel() }));

            var customTypes = CustomTypes.GetList();
            jObject.Add("customTypes", JObject.FromObject(CustomTypes.GetReferences(customTypes)));
            InjectCustomTypeReferences(jObject, customTypes);
			
            var schema = JSchema.Parse(jObject.ToString());
            schema.AllowAdditionalProperties = false;
            schema.AllowAdditionalPropertiesSpecified = false;
			
            return schema;
        }
		
        private static void InjectCustomTypeReferences(JToken jToken, ICollection<dynamic> customTypeCollection)
        {
            switch (jToken)
            {
                case JObject jObject:
                {
                    if (jObject.ContainsKey("properties") && jObject["properties"] != null)
                    {
                        foreach (var propertiesNode in jObject["properties"])
                        {
                            if (propertiesNode is JProperty jProperty)
                            {
                                foreach (var childToken in jProperty)
                                {
                                    var typeNode = childToken["type"];
                                    if (typeNode != null)
                                    {
                                        foreach (var customType in customTypeCollection)
                                        {
                                            if (typeNode.Value<string>() == customType.name)
                                            {
                                                if (childToken is JObject parentOfTypeNode)
                                                {
                                                    parentOfTypeNode.Remove("type");
                                                    parentOfTypeNode.Add("$ref", $"#/customTypes/{customType.name}");	
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    break;
                }
                case JProperty jProperty:
                    InjectCustomTypeReferences(jProperty.Value, customTypeCollection);
                    break;
            }

            foreach (var jChild in jToken.Children())
            {
                switch (jChild)
                {
                    case JProperty childObject:
                        InjectCustomTypeReferences(childObject, customTypeCollection);
                        break;
                    case JArray jArray:
                    {
                        foreach (var arrayItem in jArray)
                        {
                            InjectCustomTypeReferences(arrayItem, customTypeCollection);	
                        }

                        break;
                    }
                }
            }
        }
        
        #endregion
    }
}