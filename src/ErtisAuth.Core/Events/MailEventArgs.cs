using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Core.Events;

public class MailEventArgs : EventArgs
{
	#region Properties
	
	public MailMessage Mail { get; }
	
	/// <summary>
	/// The reason of the failure; null when the mail is sent
	/// </summary>
	public Exception? Error { get; }
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mail"></param>
	/// <param name="error"></param>
	public MailEventArgs(MailMessage mail, Exception? error = null)
	{
		this.Mail = mail;
		this.Error = error;
	}
	
	#endregion
}
