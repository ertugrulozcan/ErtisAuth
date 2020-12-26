using System;
using System.Collections.Generic;

namespace ErtisAuth.Infrastructure.Mapping
{
	public abstract class MapperBase
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeMap"></param>
		protected MapperBase(IDictionary<Type, Type> typeMap)
		{
			
		}

		#endregion
	}
}