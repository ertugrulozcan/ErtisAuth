using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Dto.Models.Mailing;

public class MailHookDto : EntityBase, IHasMembership, IHasSysDto
{
    #region Properties
	
    [BsonElement("name")]
    public string? Name { get; set; }
    
    [BsonElement("slug")]
    public string? Slug { get; set; }
	
    [BsonElement("description")]
    public string? Description { get; set; }
	
    [BsonElement("event")]
    public string? Event { get; set; }
	
    [BsonElement("membership_id")]
    public required string MembershipId { get; set; }
	
    [BsonElement("status")]
    public string? Status { get; set; }
	
    [BsonElement("mailSubject")]
    public string? MailSubject { get; set; }
    
    [BsonElement("mailTemplate")]
    public string? MailTemplate { get; set; }
    
    [BsonElement("fromName")]
    public string? FromName { get; set; }
    
    [BsonElement("fromAddress")]
    public string? FromAddress { get; set; }
    
    [BsonElement("sendToUtilizer")]
    public bool SendToUtilizer { get; set; }
    
    [BsonElement("recipients")]
    public RecipientDto[]? Recipients { get; set; }
    
    [BsonElement("mailProvider")]
    public string? MailProvider { get; set; }
	
    [BsonElement("variables")]
    public MailHookVariableDto[]? Variables { get; set; }
    
    [BsonElement("sys")]
    public SysModelDto? Sys { get; set; }
	
    #endregion
}

public class MailHookVariableDto
{
	#region Properties
	
	[BsonElement("key")]
	public string? Key { get; set; }
	
	[BsonElement("value")]
	public string? Value { get; set; }
	
	#endregion
}