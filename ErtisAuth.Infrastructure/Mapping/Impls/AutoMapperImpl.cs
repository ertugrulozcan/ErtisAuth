namespace ErtisAuth.Infrastructure.Mapping.Impls
{
	internal class AutoMapperImpl : MapperBase, IMapper
	{
		#region Properties

		private AutoMapper.IMapper Mapper { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public AutoMapperImpl(MappingCollection typeMap) : base(typeMap)
		{
			var profile = new AutoMapperProfile(typeMap);
			var mapperConfiguration = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile(profile), null);
			this.Mapper = mapperConfiguration.CreateMapper();
		}

		#endregion
		
		#region Methods

		public TOut Map<TIn, TOut>(TIn instance)
			where TIn : class
			where TOut : class
		{
			return this.Mapper.Map<TOut>(instance);
		}

		#endregion

		#region Profiles

		public class AutoMapperProfile : AutoMapper.Profile
		{
			public AutoMapperProfile(MappingCollection typeMap)
			{
				foreach (var typePair in typeMap.Mappings)
				{
					var sourceType = typePair.Key;
					var destinationType = typePair.Value;
					this.CreateMap(sourceType, destinationType);	
				}
			}
		}

		#endregion
	}
}