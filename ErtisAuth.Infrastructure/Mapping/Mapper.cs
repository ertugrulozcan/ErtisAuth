using System;
using System.Collections.Generic;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dto.Models.Events;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Dto.Models.Memberships;
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
			var typeMap = new Dictionary<Type, Type>
			{
				{ typeof(SysModelDto), typeof(SysModel) },
				{ typeof(SysModel), typeof(SysModelDto) },
				{ typeof(MembershipDto), typeof(Membership) },
				{ typeof(Membership), typeof(MembershipDto) },
				{ typeof(UserDto), typeof(User) },
				{ typeof(User), typeof(UserDto) },
				{ typeof(RoleDto), typeof(Role) },
				{ typeof(Role), typeof(RoleDto) },
				{ typeof(RevokedTokenDto), typeof(RevokedToken) },
				{ typeof(RevokedToken), typeof(RevokedTokenDto) },
				{ typeof(EventDto), typeof(ErtisAuthEvent) },
				{ typeof(ErtisAuthEvent), typeof(EventDto) },
			};
			
			this.Implementation = new TinyMapperImpl(typeMap);
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