using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using Ertis.MongoDB.Repository;
using Ertis.Schema.Dynamics.Legacy;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Infrastructure.Helpers;
using MongoDB.Bson;

namespace ErtisAuth.Infrastructure.Services
{
    public abstract class DynamicObjectCrudService : IDynamicObjectCrudService
    {
        #region Services

        private readonly IDynamicMongoRepository _repository;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repository"></param>
        protected DynamicObjectCrudService(IDynamicMongoRepository repository)
        {
            this._repository = repository;
        }

        #endregion
        
        #region Read Methods

        public virtual async Task<DynamicObject> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            var item = await this._repository.FindOneAsync(id, cancellationToken: cancellationToken);
            return item == null ? null : new DynamicObject(item);
        }

        public virtual async Task<DynamicObject> FindOneAsync(params IQuery[] queries)
        {
            var query = QueryBuilder.Where(queries);
            var matches = await this._repository.FindAsync(query.ToString(), sorting: null);
            var item = matches.Items.FirstOrDefault();
            return item == null ? null : new DynamicObject(item);
        }

        public virtual async Task<IPaginationCollection<DynamicObject>> GetAsync(
            IEnumerable<IQuery> queries,
            int? skip = null, 
            int? limit = null, 
            bool withCount = false, 
            string orderBy = null,
            SortDirection? sortDirection = null, 
            CancellationToken cancellationToken = default)
        {
            var query = QueryBuilder.Where(queries);
            var paginatedCollection = await this._repository.FindAsync(query.ToString(), skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
            return new PaginationCollection<DynamicObject>
            {
                Count = paginatedCollection.Count,
                Items = paginatedCollection.Items.Select(x => new DynamicObject(x))
            };
        }

        public virtual async Task<IPaginationCollection<DynamicObject>> QueryAsync(
            string query, 
            int? skip = null, 
            int? limit = null, 
            bool? withCount = null, 
            string orderBy = null,
            SortDirection? sortDirection = null, 
            IDictionary<string, bool> selectFields = null, 
            CancellationToken cancellationToken = default)
        {
            var paginatedCollection = await this._repository.QueryAsync(query, skip, limit, withCount, orderBy, sortDirection, selectFields, cancellationToken: cancellationToken);
            return new PaginationCollection<DynamicObject>
            {
                Count = paginatedCollection.Count,
                Items = paginatedCollection.Items.Select(x => new DynamicObject(x))
            };
        }
        
        #endregion
        
        #region Crate Methods
        
        public virtual async Task<DynamicObject> CreateAsync(DynamicObject model, CancellationToken cancellationToken = default)
        {
            var bsonDocument = BsonDocument.Create(model.ToDynamic());
            var insertedDocument = await this._repository.InsertAsync(bsonDocument, cancellationToken: cancellationToken) as BsonDocument;
            return DynamicObject.Create(BsonTypeMapper.MapToDotNetValue(insertedDocument) as Dictionary<string, object>);
        }

        #endregion
        
        #region Update Methods

        public virtual async Task<DynamicObject> UpdateAsync(string id, DynamicObject model, CancellationToken cancellationToken = default)
        {
            var bsonDocument = BsonDocument.Create(model.ToDynamic());
            var updatedDocument = await this._repository.UpdateAsync(bsonDocument, id, cancellationToken: cancellationToken);
            return updatedDocument != null ? await this.GetAsync(id, cancellationToken: cancellationToken) : null;
        }

        #endregion
        
        #region Delete Methods

        public virtual async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var isDeleted = await this._repository.DeleteAsync(id, cancellationToken: cancellationToken);
            return isDeleted;
        }

        #endregion
        
        #region Aggregation Methods

        public dynamic Aggregate(string membershipId, string aggregationStagesJson)
        {
            return this._repository.Aggregate(QueryHelper.InjectMembershipIdToAggregation(aggregationStagesJson, membershipId));
        }
		
        public async Task<dynamic> AggregateAsync(string membershipId, string aggregationStagesJson, CancellationToken cancellationToken = default)
        {
            return await this._repository.AggregateAsync(QueryHelper.InjectMembershipIdToAggregation(aggregationStagesJson, membershipId), cancellationToken: cancellationToken);
        }
		
        #endregion
    }
}