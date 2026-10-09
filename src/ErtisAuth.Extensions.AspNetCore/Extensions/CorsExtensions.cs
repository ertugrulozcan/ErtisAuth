using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Extensions.AspNetCore.Extensions;

public static class CorsExtensions
{
	#region Constants
	
	private const string CORS_POLICY_KEY = "cors-policy";
	
	#endregion
	
	#region Methods
	
	public static void AddCORS(this IServiceCollection services)
	{
		services.AddCors(options =>
		{
			options.AddPolicy(CORS_POLICY_KEY,
				policy =>
				{
					policy
						.AllowAnyOrigin()
						.AllowAnyMethod()
						.AllowAnyHeader();
				});
		});
	}
	
	public static void UseCORS(this IApplicationBuilder applicationBuilder)
	{
		applicationBuilder.UseCors(CORS_POLICY_KEY);
	}
	
	#endregion
}