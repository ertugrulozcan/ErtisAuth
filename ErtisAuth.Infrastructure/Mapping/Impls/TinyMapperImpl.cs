using System;
using System.Collections.Generic;
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
		public TinyMapperImpl(IDictionary<Type, Type> typeMap) : base(typeMap)
		{
			foreach (var typePair in typeMap)
			{
				var sourceType = typePair.Key;
				var destinationType = typePair.Value;
				TinyMapper.Bind(sourceType, destinationType);	
			}
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