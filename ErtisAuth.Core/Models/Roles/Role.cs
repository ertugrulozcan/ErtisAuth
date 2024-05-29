using System.Collections.Generic;
using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Roles
{
	public class Role : MembershipBoundedResource, IHasSysInfo
	{
		#region Fields

		private string slug;

		#endregion
		
		#region Properties

		[JsonProperty("name")]
		[JsonPropertyName("name")]
		public string Name { get; set; }
		
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

		[JsonProperty("permissions")]
		[JsonPropertyName("permissions")]
		public IEnumerable<string> Permissions { get; set; }
		
		[JsonProperty("forbidden")]
		[JsonPropertyName("forbidden")]
		public IEnumerable<string> Forbidden { get; set; }
		
		[JsonProperty("sys")]
		[JsonPropertyName("sys")]
		public SysModel Sys { get; set; }
		
		#endregion
	}
}