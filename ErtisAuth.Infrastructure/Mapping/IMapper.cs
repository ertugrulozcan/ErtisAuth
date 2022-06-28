namespace ErtisAuth.Infrastructure.Mapping
{
	public interface IMapper
	{
		TOut Map<TIn, TOut>(TIn instance)
			where TIn : class
			where TOut : class;
	}
	
	public interface IMapper<in TIn, out TOut> : IMapper
		where TIn : class
		where TOut : class
	{
		TOut Map(TIn instance);
	}
}