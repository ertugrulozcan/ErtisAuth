using Newtonsoft.Json;

namespace ErtisAuth.Hub.Models.DataTables
{
    public class DataTableResponseModel
    {
    	#region Properties

    	[JsonProperty("draw")]
    	public int ItemCountPerPage { get; set; }

    	[JsonProperty("recordsTotal")]
    	public long TotalCount { get; set; }
    	
    	[JsonProperty("recordsFiltered")]
    	public long FilteredCount { get; set; }
    	
    	[JsonProperty("error")]
    	public string ErrorMessage { get; set; }

    	[JsonProperty("data")]
    	public object[][] Data { get; set; }
    	
    	#endregion
    }
}