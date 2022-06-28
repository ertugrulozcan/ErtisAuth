using System;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Attributes
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ServiceLifetimeAttribute : Attribute
    {
        #region Fields

        public ServiceLifetime Lifetime { get; } 

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceLifetime"></param>
        public ServiceLifetimeAttribute(ServiceLifetime serviceLifetime)
        {
            this.Lifetime = serviceLifetime;
        }

        #endregion
    }
}