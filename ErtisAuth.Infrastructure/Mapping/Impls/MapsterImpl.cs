using Mapster;

namespace ErtisAuth.Infrastructure.Mapping.Impls
{
	public class MapsterImpl : IMapper
	{
		#region Methods

		public TOut Map<TIn, TOut>(TIn instance)
		{
			return instance.Adapt<TOut>();
		}

		#endregion
	}
}