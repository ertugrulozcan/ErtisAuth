namespace ErtisAuth.Infrastructure.Mapping;

public class CustomMapper<TIn, TOut> : IMapper<TIn, TOut> 
    where TIn : class
    where TOut : class
{
    #region Properties
    
    private Func<TIn, TOut> Converter { get; }
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="converter"></param>
    public CustomMapper(Func<TIn, TOut> converter)
    {
        this.Converter = converter;
    }
    
    #endregion
    
    #region Methods
    
    public TOut Map(TIn instance)
    {
        return this.Converter(instance);
    }
    
    public TOut1 Map<TIn1, TOut1>(TIn1 instance)
        where TIn1 : class
        where TOut1 : class
    {
        if (typeof(TIn1) == typeof(TIn) || 
            typeof(TIn1).BaseType == typeof(TIn) || 
            typeof(TIn).BaseType == typeof(TIn1) && 
            typeof(TOut1) == typeof(TOut) || 
            typeof(TOut1).BaseType == typeof(TOut) || 
            typeof(TOut).BaseType == typeof(TOut1))
        {
            if (instance is TIn obj)
            {
                var tout = this.Map(obj);
                if (tout is TOut1 result)
                {
                    return result;
                }
            }
        }
        
        throw new InvalidCastException("This mapper is not compatible for generic types");
    }
    
    #endregion
}