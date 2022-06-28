using System.Collections.Generic;

namespace ErtisAuth.Infrastructure.Mapping
{
    public class CustomMapperCollection
    {
        #region Properties

        private Dictionary<string, IMapper> MapperDictionary { get; }

        #endregion

        #region Constructors

        public CustomMapperCollection()
        {
            this.MapperDictionary = new Dictionary<string, IMapper>();
        }

        #endregion

        #region Methods

        public CustomMapperCollection Add<TIn, TOut>(CustomMapper<TIn, TOut> mapper)
            where TIn : class
            where TOut : class
        {
            var key = $"{typeof(TIn).FullName}_{typeof(TOut).FullName}";
            if (this.MapperDictionary.ContainsKey(key))
            {
                this.MapperDictionary[key] = mapper;
            }
            else
            {
                this.MapperDictionary.Add(key, mapper);
            }

            return this;
        }

        public bool Contains<TIn, TOut>()
        {
            var key = $"{typeof(TIn).FullName}_{typeof(TOut).FullName}";
            return this.MapperDictionary.ContainsKey(key);
        }
        
        public IMapper<TIn, TOut> GetMapper<TIn, TOut>()
            where TIn : class
            where TOut : class
        {
            var key = $"{typeof(TIn).FullName}_{typeof(TOut).FullName}";
            return this.MapperDictionary[key] as IMapper<TIn, TOut>;
        }
        
        public IMapper GetMapper(string key)
        {
            return this.MapperDictionary[key];
        }
        
        #endregion
    }
}