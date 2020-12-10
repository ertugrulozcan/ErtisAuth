using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Dto.Models.Users;
using MongoDB.Bson;
using Nelibur.ObjectMapper;

namespace ErtisAuth.Infrastructure.Mapping.Impls
{
	public class TinyMapperImpl : IMapper
	{
		#region Constructors

		public TinyMapperImpl()
		{
			TinyMapper.Bind<MembershipDto, Membership>();
			TinyMapper.Bind<Membership, MembershipDto>();
			
			TinyMapper.Bind<UserDto, User>();
			TinyMapper.Bind<User, UserDto>();
		}

		#endregion
		
		#region Methods

		public TOut Map<TIn, TOut>(TIn instance)
		{
			return TinyMapper.Map<TOut>(instance);
		}

		#endregion
	}
}