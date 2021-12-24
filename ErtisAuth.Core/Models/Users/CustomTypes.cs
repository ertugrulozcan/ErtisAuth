using System.Collections.Generic;
using System.Dynamic;

namespace ErtisAuth.Core.Models.Users
{
    public static class CustomTypes
	{
		#region Methods

		internal static ICollection<dynamic> GetList()
		{
			return new dynamic[]
			{
				new 
				{
					name = "longtext",
					schema = new
					{
						type = "string"
					}
				},
				new 
				{
					name = "richtext",
					schema = new
					{
						type = "string"
					}
				},
				new 
				{
					name = "date",
					schema = new
					{
						type = "string",
						format = "date"
					}
				},
				new 
				{
					name = "datetime",
					schema = new
					{
						type = "string",
						format = "date-time"
					}
				},
				new 
				{
					name = "email",
					schema = new
					{
						type = "string",
						format = "email"
					}
				},
				new 
				{
					name = "hostname",
					schema = new
					{
						type = "string",
						format = "hostname"
					}
				},
				new 
				{
					name = "uri",
					schema = new
					{
						type = "string",
						format = "uri"
					}
				},
				new 
				{
					name = "color",
					schema = new
					{
						type = "string",
						pattern = "(?:#|0x)(?:[a-f0-9]{3}|[a-f0-9]{6})\\b|(?:rgb|hsl)a?\\([^\\)]*\\)"
					}
				},
				new 
				{
					name = "location",
					schema = new
					{
						type = "object",
						properties = new
						{
							latitude = new 
							{
								type = "number",
								minimum = -90,
								maximum = 90
							},
							longitude = new
							{
								type = "number",
								minimum = -180,
								maximum = 180
							}
						},
						required = new [] { "latitude", "longitude" }
					}
				},
				new 
				{
					name = "json_object",
					schema = new
					{
						type = "string"
					}
				},
				new 
				{
					name = "single_reference",
					schema = new
					{
						type = "object",
						properties = new
						{
							_id = new 
							{
								type = "string"
							}
						},
						required = new [] { "_id" }
					}
				},
				new 
				{
					name = "multiple_reference",
					schema = new
					{
						type = "object",
						properties = new
						{
							references = new
							{
								type = "array",
								items = new
								{
									type = "object",
									properties = new
									{
										_id = new 
										{
											type = "string"
										}
									},
									required = new [] { "_id" }
								}
							}
						}
					}
				}
			};
		}
		
		internal static dynamic GetReferences(IEnumerable<dynamic> customTypeCollection)
		{
			var expandoObject = new ExpandoObject();
			var expandoObjectProperties = (ICollection<KeyValuePair<string, dynamic>>) expandoObject;
			foreach (var customType in customTypeCollection)
			{
				expandoObjectProperties.Add(new KeyValuePair<string, dynamic>(customType.name, customType.schema));
			}

			dynamic customTypes = expandoObject;
			return customTypes;
		}

		#endregion
	}
}