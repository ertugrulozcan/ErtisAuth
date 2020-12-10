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
			this.Implementation = new TinyMapperImpl();
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

		#endregion
	}
}