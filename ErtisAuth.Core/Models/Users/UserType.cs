using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using Ertis.Schema.Dynamics;
using Ertis.Schema.Extensions;
using Ertis.Schema.Serialization;
using Ertis.Schema.Types;
using Ertis.Schema.Validation;
using ErtisAuth.Core.Exceptions;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Users
{
    public class UserType : MembershipBoundedResource, IHasSysInfo, ISchema, ICloneable
    {
        #region Constants

        public const string ORIGIN_USER_TYPE_NAME = "Base User";
        public const string ORIGIN_USER_TYPE_SLUG = "base-user";

        #endregion
        
        #region Fields

        private string name;
        private string slug;
        private bool isAbstract;
        private bool isSealed;
        private IReadOnlyCollection<IFieldInfo> properties;

        #endregion
        
        #region Properties

        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string Name
        {
            get => this.name;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw ErtisAuthException.UserTypeNameRequired();
                }

                this.name = value;
            }
        }

        [JsonProperty("slug")]
        [JsonPropertyName("slug")]
        public string Slug
        {
            get
            {
                if (string.IsNullOrEmpty(this.slug))
                {
                    this.slug = Slugifier.Slugify(this.Name, Slugifier.Options.Ignore('_'));
                }

                return this.slug;
            }
            set => this.slug = Slugifier.Slugify(value, Slugifier.Options.Ignore('_'));
        }
        
        [JsonProperty("description")]
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonProperty("properties")]
        [JsonPropertyName("properties")]
        [Newtonsoft.Json.JsonConverter(typeof(FieldInfoCollectionJsonConverter))]
        public IReadOnlyCollection<IFieldInfo> Properties
        {
            get => this.properties;
            set => this.properties = value ?? throw ErtisAuthException.UserTypePropertiesRequired();
        }

        [JsonProperty("allowAdditionalProperties")]
        [JsonPropertyName("allowAdditionalProperties")]
        public bool AllowAdditionalProperties { get; init; }
        
        [JsonProperty("isAbstract")]
        [JsonPropertyName("isAbstract")]
        public bool IsAbstract
        {
            get => this.isAbstract;
            set
            {
                if (this.isSealed && value)
                {
                    throw ErtisAuthException.UserTypeCannotBeBothAbstractAndSealed();
                }

                this.isAbstract = value;
            }
        }

        [JsonProperty("isSealed")]
        [JsonPropertyName("isSealed")]
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
		
        [JsonProperty("baseType")]
        [JsonPropertyName("baseType")]
        public string BaseUserType { get; set; }
		
        [JsonProperty("sys")]
        [JsonPropertyName("sys")]
        public SysModel Sys { get; set; }

        #endregion

        #region Methods

        public bool ValidateSchema(out Exception exception)
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
}