using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Dto.Models.Users;

namespace ErtisAuth.Infrastructure.Mapping.Impls
{
	public class AutoMapperImpl : IMapper
	{
		#region Properties

		private AutoMapper.IMapper Mapper { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public AutoMapperImpl()
		{
			var mapperConfiguration = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>());
			this.Mapper = mapperConfiguration.CreateMapper();
		}

		#endregion
		
		#region Methods

		public TOut Map<TIn, TOut>(TIn instance)
		{
			return this.Mapper.Map<TOut>(instance);
		}

		#endregion
		
		public class AutoMapperProfile : AutoMapper.Profile
		{
			public AutoMapperProfile()
			{
				CreateMap<MembershipDto, Membership>();
				CreateMap<Membership, MembershipDto>();
				
				CreateMap<UserDto, User>();
				CreateMap<User, UserDto>();
			}
		}
	}
}