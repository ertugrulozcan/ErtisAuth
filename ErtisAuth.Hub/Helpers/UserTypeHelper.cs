using System;
using System.Collections.Generic;
using System.Globalization;

namespace ErtisAuth.Hub.Helpers
{
    public static class UserTypeHelper
    {
        #region Methods

        public static string[] GetFieldTypeNames()
        {
            return new[]
            {
                "string",
                "number",
                "integer",
                "boolean",
                "array",
                "object",
                "longtext",
                "richtext",
                "date",
                "datetime",
                "email",
                "hostname",
                "uri",
                "color",
                "location",
                "json_object",
                "single_reference",
                "multiple_reference"
            };
        }
        
        public static string GetFieldTypeDisplayName(string propertyTypeName)
        {
            switch (propertyTypeName)
            {
                case "string":
                    return "String";
                case "number":
                    return "Number";
                case "integer":
                    return "Integer";
                case "boolean":
                    return "Boolean";
                case "array":
                    return "Array";
                case "object":
                    return "Object";
                case "longtext":
                    return "Long Text";
                case "richtext":
                    return "Rich Text";
                case "date":
                    return "Date";
                case "datetime":
                    return "Date & Time";
                case "email":
                    return "Email Address";
                case "hostname":
                    return "Hostname";
                case "uri":
                    return "Url";
                case "color":
                    return "Color";
                case "location":
                    return "Location";
                case "json_object":
                    return "Json Object";
                case "single_reference":
                    return "Single Reference";
                case "multiple_reference":
                    return "Multiple Reference";
                default:
                    return "Unknown";
            }
        }
        
        public static string GetFieldTypeDescription(string propertyTypeName)
        {
            switch (propertyTypeName)
            {
                case "string":
                    return "Plain Text";
                case "number":
                    return "Floating Number";
                case "integer":
                    return "Integer Number";
                case "boolean":
                    return "True/False, On/Off etc.";
                case "array":
                    return "Collection";
                case "object":
                    return "Composite Object";
                case "longtext":
                    return "Long Text";
                case "richtext":
                    return "Rich Text";
                case "date":
                    return "Date";
                case "datetime":
                    return "Date & Time";
                case "email":
                    return "Email Address";
                case "hostname":
                    return "Hostname";
                case "uri":
                    return "Url";
                case "color":
                    return "RGB Color Code";
                case "location":
                    return "Latitude & Longitude";
                case "json_object":
                    return "Raw Json Object";
                case "single_reference":
                    return "Single Reference";
                case "multiple_reference":
                    return "Multiple Reference";
                default:
                    return "Unknown";
            }
        }

        public static string GetFieldTypeBadgeColor(string propertyTypeName)
        {
            switch (propertyTypeName)
            {
                case "string":
                    return "primary";
                case "number":
                    return "success";
                case "integer":
                    return "light-success";
                case "boolean":
                    return "light-danger";
                case "array":
                    return "secondary";
                case "object":
                    return "dark";
                case "longtext":
                case "richtext":
                    return "primary";
                case "date":
                case "datetime":
                case "email":
                case "hostname":
                case "uri":
                case "color":
                case "location":
                    return "warning";
                case "json_object":
                    return "dark";
                case "single_reference":
                    return "light-info";
                case "multiple_reference":
                    return "info";
                default:
                    return "light-dark";
            }
        }
        
        public static string GetFieldTypeIcon(string propertyTypeName)
        {
            switch (propertyTypeName)
            {
                case "string":
                    return "sms";
                case "number":
                    return "superscript";
                case "integer":
                    return "superscript";
                case "boolean":
                    return "toggle-on";
                case "array":
                    return "list";
                case "object":
                    return "sitemap";
                case "longtext":
                case "richtext":
                    return "comment";
                case "date":
                    return "calendar";
                case "datetime":
                    return "clock";
                case "email":
                    return "at";
                case "hostname":
                    return "globe-africa";
                case "uri":
                    return "globe-americas";
                case "color":
                    return "palette";
                case "location":
                    return "map-marked";
                case "json_object":
                    return "code";
                case "single_reference":
                case "multiple_reference":
                    return "link";
                default:
                    return "light-dark";
            }
        }
        
        public static object EnsurePropertyValue(string propertyTypeName, object value)
        {
            if (value == null)
            {
                return GetPropertyDefaultValue(propertyTypeName);
            }
            
            switch (propertyTypeName)
            {
                case "string":
                case "longtext":
                case "richtext":
                case "email":
                case "hostname":
                case "uri":
                case "json_object":
                    return value.ToString();
                case "number":
                    return (double)value;
                case "integer":
                    return (int)value;
                case "boolean":
                    return (bool)value;
                case "array":
                    return value as IEnumerable<object>;
                case "object":
                    return value;
                case "date":
                case "datetime":
                    return DateTime.Parse(value.ToString() ?? DateTime.Now.ToString(CultureInfo.InvariantCulture));
                default:
                    return value;
            }
        }
        
        public static object GetPropertyDefaultValue(string propertyTypeName)
        {
            switch (propertyTypeName)
            {
                case "string":
                case "longtext":
                case "richtext":
                case "email":
                case "hostname":
                case "uri":
                case "json_object":
                    return string.Empty;
                case "number":
                    return 0.0d;
                case "integer":
                    return 0;
                case "boolean":
                    return false;
                case "array":
                    return Array.Empty<object>();
                case "object":
                    return new {};
                case "date":
                case "datetime":
                    return DateTime.Now;
                case "color":
                    return "#FFFFFF";
                default:
                    return null;
            }
        }

        #endregion
    }
}