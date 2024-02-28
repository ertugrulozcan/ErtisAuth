using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Ertis.MongoDB.Attributes;
using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Roles
{
	// ReSharper disable once ClassNeverInstantiated.Global
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	public class RoleDto : EntityBase, IHasMembership, IHasSysDto
	{
		#region Properties

		[Searchable]
		[BsonElement("name")]
		public string Name { get; set; }
		
		[BsonElement("slug")]
		public string Slug { get; set; }
		
		[BsonElement("description")]
		public string Description { get; set; }
		
		[BsonElement("membership_id")]
		public string MembershipId { get; set; }
		
		[BsonElement("permissions")]
		public IEnumerable<string> Permissions { get; set; }
		
		[BsonElement("forbidden")]
		public IEnumerable<string> Forbidden { get; set; }
		
		[BsonElement("sys")]
		public SysModelDto Sys { get; set; }
		
		#endregion
	}
}