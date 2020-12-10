using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Data.Models;
using Ertis.MongoDB.Repository;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public abstract class GenericCrudService<TModel, TDto> : IGenericCrudService<TModel> where TDto : IEntity<string>
	{
		#region Services

		protected readonly IMongoRepository<TDto> repository;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="repository"></param>
		protected GenericCrudService(IMongoRepository<TDto> repository)
		{
			this.repository = repository;
		}

		#endregion
		
		#region Methods

		public TModel Get(string id)
		{
			var dto = this.repository.FindOne(id);
			return Mapper.Current.Map<TDto, TModel>(dto);
		}
		
		public async Task<TModel> GetAsync(string id)
		{
			var dto = await this.repository.FindOneAsync(id);
			return Mapper.Current.Map<TDto, TModel>(dto);
		}
		
		public IPaginationCollection<TModel> Get(int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var paginatedDtoCollection = this.repository.Find(expression: null, skip, limit, withCount, orderBy, sortDirection);
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
		
		public async Task<IPaginationCollection<TModel>> GetAsync(int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var paginatedDtoCollection = await this.repository.FindAsync(expression: null, skip, limit, withCount, orderBy, sortDirection);
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
	}
}