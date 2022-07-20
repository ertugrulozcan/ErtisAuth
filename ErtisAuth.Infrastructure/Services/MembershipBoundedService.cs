using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Data.Models;
using Ertis.MongoDB.Queries;
using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public abstract class MembershipBoundedService<TModel, TDto> : IMembershipBoundedService<TModel> 
		where TModel : class, Core.Models.IHasMembership, Core.Models.IHasIdentifier
		where TDto : class, IEntity<string>, Dto.Models.IHasMembership
	{
		#region Services

		protected readonly IMembershipService membershipService;
		protected readonly IMongoRepository<TDto> repository;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="repository"></param>
		protected MembershipBoundedService(IMembershipService membershipService, IMongoRepository<TDto> repository)
		{
			this.membershipService = membershipService;
			this.repository = repository;
		}

		#endregion
		
		#region Query Methods

		public IPaginationCollection<dynamic> Query(
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string sortField = null,
			SortDirection? sortDirection = null, 
			IDictionary<string, bool> selectFields = null)
		{
			return this.repository.Query(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		public async ValueTask<IPaginationCollection<dynamic>> QueryAsync(
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string sortField = null,
			SortDirection? sortDirection = null, 
			IDictionary<string, bool> selectFields = null)
		{
			return await this.repository.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
		
		#region Get Methods

		public virtual TModel Get(string membershipId, string id)
		{
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var dto = this.repository.FindOne(x => x.Id == id && x.MembershipId == membershipId);
			return Mapper.Current.Map<TDto, TModel>(dto);
		}

		public virtual async ValueTask<TModel> GetAsync(string membershipId, string id)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var dto = await this.repository.FindOneAsync(x => x.Id == id && x.MembershipId == membershipId);
			return Mapper.Current.Map<TDto, TModel>(dto);
		}
		
		protected async ValueTask<TModel> GetAsync(string membershipId, Expression<Func<TDto, bool>> expression)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var entities = await this.repository.FindAsync(expression);
			var entity = entities.Items.FirstOrDefault(x => x.MembershipId == membershipId);
			return Mapper.Current.Map<TDto, TModel>(entity);
		}
		
		public virtual IPaginationCollection<TModel> Get(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var paginatedDtoCollection = this.repository.Find(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
			if (paginatedDtoCollection?.Items != null)
			{
				return new PaginationCollection<TModel>
				{
					Items = paginatedDtoCollection.Items.Select(x => Mapper.Current.Map<TDto, TModel>(x)),
					Count = paginatedDtoCollection.Count
				};
			}
			else
			{
				return new PaginationCollection<TModel>();
			}
		}
		
		public virtual async ValueTask<IPaginationCollection<TModel>> GetAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var paginatedDtoCollection = await this.repository.FindAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
			if (paginatedDtoCollection?.Items != null)
			{
				return new PaginationCollection<TModel>
				{
					Items = paginatedDtoCollection.Items.Select(x => Mapper.Current.Map<TDto, TModel>(x)),
					Count = paginatedDtoCollection.Count
				};
			}
			else
			{
				return new PaginationCollection<TModel>();
			}
		}

		public T Get<T>(string membershipId, string id) where T : class, Core.Models.IHasMembership
		{
			return this.Get(membershipId, id) as T;
		}
		
		public async ValueTask<T> GetAsync<T>(string membershipId, string id) where T : class, Core.Models.IHasMembership
		{
			return await this.GetAsync(membershipId, id) as T;
		}

		public IPaginationCollection<T> Get<T>(
			string membershipId, 
			int? skip, 
			int? limit, 
			bool withCount,
			string orderBy, 
			SortDirection? sortDirection) 
			where T : class, Core.Models.IHasMembership
		{
			return this.Get(membershipId, skip, limit, withCount, orderBy, sortDirection) as IPaginationCollection<T>;
		}

		public async ValueTask<IPaginationCollection<T>> GetAsync<T>(
			string membershipId, 
			int? skip,
			int? limit, 
			bool withCount, 
			string orderBy, 
			SortDirection? sortDirection)
			where T : class, Core.Models.IHasMembership
		{
			return await this.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection) as IPaginationCollection<T>;
		}

		#endregion

		#region Search Methods

		public IPaginationCollection<TModel> Search(
			string membershipId, 
			string keyword,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string sortField = null,
			SortDirection? sortDirection = null)
		{
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var textSearchLanguage = TextSearchLanguage.None;
			if (!string.IsNullOrEmpty(membership.DefaultLanguage) && TextSearchLanguage.All.Any(x => x.ISO6391Code == membership.DefaultLanguage))
			{
				textSearchLanguage = TextSearchLanguage.All.FirstOrDefault(x => x.ISO6391Code == membership.DefaultLanguage);
			}

			var textSearchOptions = new TextSearchOptions
			{
				Language = textSearchLanguage,
				IsCaseSensitive = true,
				IsDiacriticSensitive = false
			};
			
			var paginatedDtoCollection = this.repository.Search(keyword, textSearchOptions, skip, limit, withCount, sortField, sortDirection);
			if (paginatedDtoCollection?.Items != null)
			{
				return new PaginationCollection<TModel>
				{
					Items = paginatedDtoCollection.Items.Select(x => Mapper.Current.Map<TDto, TModel>(x)),
					Count = paginatedDtoCollection.Count
				};
			}
			else
			{
				return new PaginationCollection<TModel>();
			}
		}

		public async ValueTask<IPaginationCollection<TModel>> SearchAsync(
			string membershipId, 
			string keyword,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string sortField = null,
			SortDirection? sortDirection = null)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			var textSearchLanguage = TextSearchLanguage.None;
			if (!string.IsNullOrEmpty(membership.DefaultLanguage) && TextSearchLanguage.All.Any(x => x.ISO6391Code == membership.DefaultLanguage))
			{
				textSearchLanguage = TextSearchLanguage.All.FirstOrDefault(x => x.ISO6391Code == membership.DefaultLanguage);
			}

			var textSearchOptions = new TextSearchOptions
			{
				Language = textSearchLanguage,
				IsCaseSensitive = true,
				IsDiacriticSensitive = false
			};
			
			var paginatedDtoCollection = await this.repository.SearchAsync(keyword, textSearchOptions, skip, limit, withCount, sortField, sortDirection);
			if (paginatedDtoCollection?.Items != null)
			{
				return new PaginationCollection<TModel>
				{
					Items = paginatedDtoCollection.Items.Select(x => Mapper.Current.Map<TDto, TModel>(x)),
					Count = paginatedDtoCollection.Count
				};
			}
			else
			{
				return new PaginationCollection<TModel>();
			}
		}

		#endregion

		#region Aggregation Methods

		public dynamic Aggregate(string aggregateStagesJson)
		{
			return this.repository.Aggregate(aggregateStagesJson);
		}
		
		public async Task<dynamic> AggregateAsync(string aggregateStagesJson)
		{
			return await this.repository.AggregateAsync(aggregateStagesJson);
		}

		#endregion
	}
}