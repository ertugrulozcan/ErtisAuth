using System.Collections.Generic;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Identity;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Applications
{
	public class Application : MembershipBoundedResource, IUtilizer, IHasSysInfo
	{
		#region Fields

		private string slug;

		#endregion
		
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
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
			set => this.slug = Slugifier.Slugify(value, Slugifier.Options.Ignore('_'));
		}

		[JsonProperty("role")]
		public string Role { get; set; }
		
		[JsonProperty("permissions")]
		public IEnumerable<string> Permissions { get; set; }
		
		[JsonProperty("forbidden")]
		public IEnumerable<string> Forbidden { get; set; }
		
		[JsonProperty("sys")]
		public SysModel Sys { get; set; }
		
		[JsonIgnore] 
		public Utilizer.UtilizerType UtilizerType => Utilizer.UtilizerType.Application;

		#endregion
	}
}