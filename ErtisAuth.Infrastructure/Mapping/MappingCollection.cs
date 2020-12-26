using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ErtisAuth.Infrastructure.Mapping
{
	public class MappingCollection
	{
		#region Properties

		private List<KeyValuePair<Type, Type>> TypePairList { get; }
		
		public ReadOnlyCollection<KeyValuePair<Type, Type>> Mappings { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public MappingCollection()
		{
			this.TypePairList = new List<KeyValuePair<Type, Type>>();
			this.Mappings = new ReadOnlyCollection<KeyValuePair<Type, Type>>(this.TypePairList);
		}

		#endregion
		
		#region Methods

		public void Add<T1, T2>()
		{
			this.TypePairList.Add(new KeyValuePair<Type, Type>(typeof(T1), typeof(T2)));
		}

		#endregion
	}
}