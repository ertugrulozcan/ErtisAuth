using System;
using System.Collections.Generic;
using System.Dynamic;
using Ertis.Core.Collections;

namespace ErtisAuth.WebAPI.Helpers
{
	public static class QueryHelper
	{
		#region Methods

		public static IPaginationCollection<dynamic> FixTimeZoneOffsetInQueryResult(IPaginationCollection<dynamic> dtos)
		{
			if (dtos.Items != null)
			{
				var timeZoneOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

				var items = new List<dynamic>();
				foreach (var dtoItem in dtos.Items)
				{
					if (dtoItem is IDictionary<string, object> propertyDictionary)
					{
						var expandoObjectDictionary = new Dictionary<string, object>();
						foreach (var (propertyName, value) in propertyDictionary)
						{
							var propertyValue = value;
							
							// Property fix operations
							if (propertyValue is DateTime dateTimeProperty)
							{
								propertyValue = dateTimeProperty.Add(timeZoneOffset);
							}	
							
							expandoObjectDictionary.Add(propertyName, propertyValue);
						}
						
						// Convert Dictionary to ExpandoObject
						var expandoObject = new ExpandoObject();
						var eoColl = (ICollection<KeyValuePair<string, object>>)expandoObject;
						foreach (var kvp in expandoObjectDictionary)
						{
							eoColl.Add(kvp);
						}

						dynamic dynamicObject = expandoObject;
						items.Add(dynamicObject);
					}
				}

				return new PaginationCollection<dynamic>
				{
					Count = dtos.Count,
					Items = items
				};
			}
			
			return dtos;
		}

		#endregion
	}
}