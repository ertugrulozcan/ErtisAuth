using System;
using System.Collections.Generic;
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
    public class UserType : MembershipBoundedResource, IHasSysInfo, ISchema
    {
        #region Constants

        public const string ORIGIN_USER_TYPE_NAME = "base_user";

        #endregion
        
        #region Fields

        private string name;
        private string slug;
        private IReadOnlyCollection<IFieldInfo> properties;
        private bool isAbstract;
        private bool isSealed;

        #endregion
        
        #region Properties

        [JsonProperty("name")]
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
        }
        
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("properties")]
        [JsonConverter(typeof(FieldInfoCollectionJsonConverter))]
        public IReadOnlyCollection<IFieldInfo> Properties
        {
            get => this.properties;
            set => this.properties = value ?? throw ErtisAuthException.UserTypePropertiesRequired();
        }

        [JsonProperty("allowAdditionalProperties")]
        public bool AllowAdditionalProperties { get; init; }
        
        [JsonProperty("isAbstract")]
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
        public string BaseUserType { get; set; }
		
        [JsonProperty("sys")]
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
    }
}