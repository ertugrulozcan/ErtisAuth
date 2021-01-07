namespace ErtisAuth.WebAPI.Models
{
	public interface IApiVersionOptions
	{
		#region Properties

		int Major { get; }
		
		int Minor { get; }

		#endregion
	}
	
	public class ApiVersionOptions : IApiVersionOptions
	{
		#region Properties

		public int Major { get; set; }
		
		public int Minor { get; set; }

		#endregion

		#region Method

		public override string ToString()
		{
			if (this.Minor > 0)
			{
				return $"{this.Major}.{this.Minor}";
			}
			
			return this.Major.ToString();
		}

		#endregion
	}
}