using System.Collections.Generic;
using System.Linq;
using Ertis.Schema.Types;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Infrastructure.Extensions
{
    public static class UserTypeExtensions
    {
        #region Methods

        public static IEnumerable<IFieldInfo> MergeUserTypeProperties(this UserType contentType1, UserType contentType2)
        {
            var properties = new List<IFieldInfo>();
            properties.AddRange(contentType1.Properties);

            foreach (var fieldInfo in contentType2.Properties)
            {
                var currentFieldInfo = properties.FirstOrDefault(x => x.Name == fieldInfo.Name);
                if (currentFieldInfo != null)
                {
                    if (fieldInfo.IsVirtual)
                    {
                        if (currentFieldInfo.Type != fieldInfo.Type)
                        {
                            throw ErtisAuthException.VirtualFieldTypeCanNotOverwrite(fieldInfo.Name);
                        }
                    }
                    else
                    {
                        throw ErtisAuthException.DuplicateFieldWithBaseType(fieldInfo.Name);   
                    }
                }
                else
                {
                    properties.Add(fieldInfo);
                }
            }

            return properties;
        }

        #endregion
    }
}