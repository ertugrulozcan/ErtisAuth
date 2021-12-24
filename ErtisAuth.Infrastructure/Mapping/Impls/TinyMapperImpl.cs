using Nelibur.ObjectMapper;

namespace ErtisAuth.Infrastructure.Mapping.Impls
{
	internal class TinyMapperImpl : MapperBase, IMapper
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeMap"></param>
		public TinyMapperImpl(MappingCollection typeMap) : base(typeMap)
		{
			foreach (var typePair in typeMap.Mappings)
			{
				var sourceType = typePair.Key;
				var destinationType = typePair.Value;
				TinyMapper.Bind(sourceType, destinationType);	
			}
		}

		#endregion
		
		#region Methods

		public TOut Map<TIn, TOut>(TIn instance)
			where TIn : class
			where TOut : class
		{
			return TinyMapper.Map<TOut>(instance);
		}

		#endregion
	}
}