using System.Reflection;
using System.Runtime.ExceptionServices;
using ErtisAuth.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// An installation whose token service fails like an unreachable database (TimeoutException) when it verifies
/// <see cref="OutageToken"/>; every other call is served as usual.
/// </summary>
public sealed class OutageErtisAuthInstance : ErtisAuthInstance
{
	#region Constants
	
	public const string OutageToken = "simulate-database-outage";
	
	#endregion
	
	#region Constructors
	
	public OutageErtisAuthInstance(MongoDbContainerFixture mongo) : base(mongo)
	{
	
	}
	
	#endregion
	
	#region Methods
	
	protected override void ConfigureTestServices(IServiceCollection services)
	{
		var descriptor = services.Single(x => x.ServiceType == typeof(ITokenService));
		services.Remove(descriptor);
		services.AddSingleton(serviceProvider =>
		{
			var tokenService = (ITokenService) ActivatorUtilities.CreateInstance(serviceProvider, descriptor.ImplementationType!);
			return FailingTokenService.Create(tokenService);
		});
	}
	
	#endregion
	
	#region Nested Types
	
	/// <summary>
	/// Decorates the token service: verifying the outage token throws, everything else is delegated.
	/// </summary>
	public class FailingTokenService : DispatchProxy
	{
		private ITokenService _inner = null!;
		
		public static ITokenService Create(ITokenService inner)
		{
			var proxy = Create<ITokenService, FailingTokenService>();
			((FailingTokenService) (object) proxy)._inner = inner;
			return proxy;
		}
		
		protected override object? Invoke(MethodInfo? method, object?[]? args)
		{
			if (method!.Name.StartsWith("Verify") && args?.FirstOrDefault() is OutageToken)
			{
				throw new TimeoutException("A timeout occurred after 30000ms selecting a server");
			}
			
			try
			{
				return method.Invoke(this._inner, args);
			}
			catch (TargetInvocationException ex) when (ex.InnerException != null)
			{
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				throw;
			}
		}
	}
	
	#endregion
}