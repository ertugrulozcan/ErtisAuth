using System.Collections.Generic;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.GeoLocation;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Dto.Models.Applications;
using ErtisAuth.Dto.Models.Events;
using ErtisAuth.Dto.Models.GeoLocation;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Dto.Models.Mailing;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Dto.Models.Providers;
using ErtisAuth.Dto.Models.Resources;
using ErtisAuth.Dto.Models.Roles;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Dto.Models.Webhooks;
using ErtisAuth.Infrastructure.Mapping.Extensions;
using ErtisAuth.Infrastructure.Mapping.Impls;

namespace ErtisAuth.Infrastructure.Mapping
{
	public sealed class Mapper : IMapper
	{
		#region Singleton

		private static Mapper _self;

		public static Mapper Current => _self ??= new Mapper();

		#endregion

		#region Properties

		private IMapper Implementation { get; }
		
		private CustomMapperCollection CustomMappers { get; }

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
			mappings.Add<User, UserWithPasswordHash>();
			mappings.Add<UserWithPasswordHash, User>();
			mappings.Add<ApplicationDto, Application>();
			mappings.Add<Application, ApplicationDto>();
			mappings.Add<RoleDto, Role>();
			mappings.Add<Role, RoleDto>();
			mappings.Add<ActiveTokenDto, ActiveToken>();
			mappings.Add<ActiveToken, ActiveTokenDto>();
			mappings.Add<RevokedTokenDto, RevokedToken>();
			mappings.Add<RevokedToken, RevokedTokenDto>();
			mappings.Add<EventDto, ErtisAuthEvent>();
			mappings.Add<ErtisAuthEvent, EventDto>();
			mappings.Add<WebhookDto, Webhook>();
			mappings.Add<Webhook, WebhookDto>();
			mappings.Add<WebhookRequestDto, WebhookRequest>();
			mappings.Add<WebhookRequest, WebhookRequestDto>();
			mappings.Add<ClientInfoDto, ClientInfo>();
			mappings.Add<ClientInfo, ClientInfoDto>();
			mappings.Add<GeoLocationInfoDto, GeoLocationInfo>();
			mappings.Add<GeoLocationInfo, GeoLocationInfoDto>();
			mappings.Add<CoordinateDto, Coordinate>();
			mappings.Add<Coordinate, CoordinateDto>();
			this.Implementation = new TinyMapperImpl(mappings);

			this.CustomMappers = new CustomMapperCollection();
			this.CustomMappers
				.Add(new CustomMapper<Membership, MembershipDto>((model) => model.ToDto()))
				.Add(new CustomMapper<MembershipDto, Membership>((dto) => dto.ToModel()))
				.Add(new CustomMapper<UserType, UserTypeDto>((model) => model.ToDto()))
				.Add(new CustomMapper<UserTypeDto, UserType>((dto) => dto.ToModel()))
				.Add(new CustomMapper<User, UserDto>((model) => model.ToDto()))
				.Add(new CustomMapper<UserDto, User>((dto) => dto.ToModel()))
				.Add(new CustomMapper<UserWithPasswordHash, UserDto>((model) => model.ToDto()))
				.Add(new CustomMapper<UserDto, UserWithPasswordHash>((dto) => new UserWithPasswordHash(dto.ToModel()) { PasswordHash = dto.PasswordHash }))
				.Add(new CustomMapper<Webhook, WebhookDto>((model) => model.ToDto()))
				.Add(new CustomMapper<WebhookDto, Webhook>((dto) => dto.ToModel()))
				.Add(new CustomMapper<MailHook, MailHookDto>((model) => model.ToDto()))
				.Add(new CustomMapper<MailHookDto, MailHook>((dto) => dto.ToModel()))
				.Add(new CustomMapper<Provider, ProviderDto>((model) => model.ToDto()))
				.Add(new CustomMapper<ProviderDto, Provider>((dto) => dto.ToModel()))
				.Add(new CustomMapper<TokenCodePolicy, TokenCodePolicyDto>((model) => model.ToDto()))
				.Add(new CustomMapper<TokenCodePolicyDto, TokenCodePolicy>((dto) => dto.ToModel()))
				.Add(new CustomMapper<OneTimePassword, OneTimePasswordDto>((model) => model.ToDto()))
				.Add(new CustomMapper<OneTimePasswordDto, OneTimePassword>((dto) => dto.ToModel()));
		}

		#endregion
		
		#region Methods

		public TOut Map<TIn, TOut>(TIn instance)
			where TIn : class
			where TOut : class
		{
			if (instance == null)
			{
				return default;
			}
			
			if (this.CustomMappers.Contains<TIn, TOut>())
			{
				var mapperKey = $"{instance.GetType().FullName}_{typeof(TOut).FullName}";
				var mapper = this.CustomMappers.GetMapper(mapperKey) ?? this.CustomMappers.GetMapper<TIn, TOut>();
				return mapper.Map<TIn, TOut>(instance);
			}
			
			return this.Implementation.Map<TIn, TOut>(instance);
		}
		
		public IEnumerable<TOut> MapCollection<TIn, TOut>(IEnumerable<TIn> collection)
			where TIn : class
			where TOut : class
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