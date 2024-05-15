using System;
using Ertis.Data.Repository;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Dto.Models;
using ErtisAuth.Dto.Models.Resources;

namespace ErtisAuth.Infrastructure.Adapters
{
	public class SysUpserter : IRepositoryActionBinder
	{
		#region Services

		private readonly IScopeOwnerAccessor scopeOwnerAccessor;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="scopeOwnerAccessor"></param>
		public SysUpserter(IScopeOwnerAccessor scopeOwnerAccessor)
		{
			this.scopeOwnerAccessor = scopeOwnerAccessor;
		}

		#endregion
		
		#region Methods

		public TEntity BeforeInsert<TEntity>(TEntity entity)
		{
			try
			{
				if (entity != null && entity is IHasSysDto dto)
				{
					dto.Sys = new SysModelDto
					{
						CreatedAt = DateTime.Now,
						CreatedBy = this.scopeOwnerAccessor.GetRequestOwner()
					};	
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
			
			return entity;
		}

		public TEntity AfterInsert<TEntity>(TEntity entity)
		{
			return entity;
		}

		public TEntity BeforeUpdate<TEntity>(TEntity entity)
		{
			try
			{
				if (entity != null && entity is IHasSysDto dto)
				{
					var requestOwner = this.scopeOwnerAccessor.GetRequestOwner();
					if (dto.Sys != null)
					{
						dto.Sys.ModifiedAt = DateTime.Now;
						dto.Sys.ModifiedBy = requestOwner;
					}
					else
					{
						dto.Sys = new SysModelDto
						{
							CreatedAt = DateTime.Now,
							CreatedBy = requestOwner,
							ModifiedAt = DateTime.Now,
							ModifiedBy = requestOwner
						};	
					}	
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
			
			return entity;
		}

		public TEntity AfterUpdate<TEntity>(TEntity entity)
		{
			return entity;
		}

		#endregion
	}
}