using System.Linq;
using Ertis.Core.Collections;
using Ertis.Net.Http;

namespace ErtisAuth.Sdk.Helpers
{
	internal static class QueryStringHelper
	{
		internal static IQueryString GetQueryString(
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null)
		{
			var queryString = QueryString.Create();
			if (skip != null)
				queryString.Add("skip", skip.Value);
			if (limit != null)
				queryString.Add("limit", limit.Value);
			if (withCount != null)
				queryString.Add("with_count", withCount.Value.ToString().ToLower());

			if (!string.IsNullOrEmpty(orderBy))
			{
				var sortQueryParam = orderBy;
				if (sortDirection == SortDirection.Descending)
					sortQueryParam += "%20desc";
				
				queryString.Add("sort", sortQueryParam);
			}

			return queryString;
		}
		
		internal static IQueryString GetQueryString(
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			Sorting sorting = null)
		{
			var queryString = QueryString.Create();
			if (skip != null)
				queryString.Add("skip", skip.Value);
			if (limit != null)
				queryString.Add("limit", limit.Value);
			if (withCount != null)
				queryString.Add("with_count", withCount.Value.ToString().ToLower());
			
			if (sorting != null)
			{
				queryString.Add("sort", string.Join(';', sorting.Select(x => x.SortDirection == SortDirection.Descending ? $"{x.OrderBy}%20desc" : x.OrderBy)));
			}
			
			return queryString;
		}
	}
}