using System;
using System.Collections.Generic;
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
		public AutoMapperImpl(IDictionary<Type, Type> typeMap) : base(typeMap)
		{
			var profile = new AutoMapperProfile(typeMap);
			var mapperConfiguration = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile(profile));
			this.Mapper = mapperConfiguration.CreateMapper();
		}

		#endregion
		
		#region Methods

		public TOut Map<TIn, TOut>(TIn instance)
		{
			return this.Mapper.Map<TOut>(instance);
		}

		#endregion

		#region Profiles

		public class AutoMapperProfile : AutoMapper.Profile
		{
			public AutoMapperProfile(IDictionary<Type, Type> typeMap)
			{
				foreach (var typePair in typeMap)
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