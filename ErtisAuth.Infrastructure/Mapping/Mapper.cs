using System.Collections.Generic;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dto.Models.Applications;
using ErtisAuth.Dto.Models.Events;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Dto.Models.Providers;
using ErtisAuth.Dto.Models.Resources;
using ErtisAuth.Dto.Models.Roles;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Infrastructure.Mapping.Impls;

namespace ErtisAuth.Infrastructure.Mapping
{
	public sealed class Mapper : IMapper
	{
		#region Singleton

		private static Mapper self;

		public static Mapper Current
		{
			get
			{
				if (self == null)
					self = new Mapper();

				return self;
			}
		}

		#endregion

		#region Properties

		private IMapper Implementation { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		private Mapper()
		{
			var mappings = new MappingCollection();
			
			mappings.Add<SysModelDto, SysModel>();
			mappings.Add<SysModel, SysModelDto>();
			mappings.Add<MembershipDto, Membership>();
			mappings.Add<Membership, MembershipDto>();
			mappings.Add<UserDto, User>();
			mappings.Add<User, UserDto>();
			mappings.Add<UserDto, UserWithPassword>();
			mappings.Add<UserWithPassword, UserDto>();
			mappings.Add<User, UserWithPassword>();
			mappings.Add<UserWithPassword, User>();
			mappings.Add<ApplicationDto, Application>();
			mappings.Add<Application, ApplicationDto>();
			mappings.Add<RoleDto, Role>();
			mappings.Add<Role, RoleDto>();
			mappings.Add<OAuthProviderDto, OAuthProvider>();
			mappings.Add<OAuthProvider, OAuthProviderDto>();
			mappings.Add<RevokedTokenDto, RevokedToken>();
			mappings.Add<RevokedToken, RevokedTokenDto>();
			mappings.Add<EventDto, ErtisAuthEvent>();
			mappings.Add<ErtisAuthEvent, EventDto>();
			
			this.Implementation = new TinyMapperImpl(mappings);
		}

		#endregion
		
		#region Methods

		public TOut Map<TIn, TOut>(TIn instance)
		{
			if (instance == null)
			{
				return default;
			}
			
			return this.Implementation.Map<TIn, TOut>(instance);
		}
		
		public IEnumerable<TOut> MapCollection<TIn, TOut>(IEnumerable<TIn> collection)
		{
			if (collection != null)
			{
				foreach (var instance in collection)
				{
					yield return this.Implementation.Map<TIn, TOut>(instance);
				}
			}
		}

		#endregion
	}
}