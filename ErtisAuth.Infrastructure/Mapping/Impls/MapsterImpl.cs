using Mapster;

namespace ErtisAuth.Infrastructure.Mapping.Impls
{
	public class MapsterImpl : MapperBase, IMapper
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeMap"></param>
		public MapsterImpl(MappingCollection typeMap) : base(typeMap)
		{
			
		}

		#endregion
		
		#region Methods

		public TOut Map<TIn, TOut>(TIn instance)
		{
			return instance.Adapt<TOut>();
		}

		#endregion
	}
}