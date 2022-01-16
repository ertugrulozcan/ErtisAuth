using System;
using System.Dynamic;
using Ertis.CMS.Core.Models.Contents;
using Ertis.CMS.Dynamics.Extensions;

namespace ErtisAuth.Hub.Models
{
    public class CustomTypeField
    {
        #region Properties

        public string Name { get; private set; }
        
        public string Title { get; private set; }
        
        public string Description { get; private set; }
        
        public FieldType Type { get; private set; }
        
        public bool IsRequired { get; private set; }
        
        public object Value { get; private set; }

        #endregion

        #region Methods

        public static CustomTypeField Create(ExpandoObject field, object value)
        {
            var customTypeField = new CustomTypeField
            {
                Value = value
            };
            
            try
            {
                var fieldType = field.GetProperty<string>("type");
                customTypeField.Type = Enum.Parse<FieldType>(fieldType);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Custom type field type could not read from dynamic payload!");
                throw new Exception("Field type must be have a type!", ex);
            }
            
            try
            {
                var fieldName = field.GetProperty<string>("name");
                customTypeField.Name = fieldName;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Custom type field name could not read from dynamic payload!");
                throw new Exception("Field type must be have a name!", ex);
            }
            
            try
            {
                var fieldTitle = field.GetProperty<string>("title");
                customTypeField.Title = fieldTitle;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Custom type field title could not read from dynamic payload!");
                Console.WriteLine(ex);
            }
            
            try
            {
                var fieldDescription = field.GetProperty<string>("description");
                customTypeField.Description = fieldDescription;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Custom type field description could not read from dynamic payload!");
                Console.WriteLine(ex);
            }
            
            try
            {
                var isRequired = field.GetProperty<bool>("required");
                customTypeField.IsRequired = isRequired;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Custom type field description could not read from dynamic payload!");
                Console.WriteLine(ex);
            }

            return customTypeField;
        }

        #endregion
    }
}