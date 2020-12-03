using System;
using System.Net;
using Ertis.Extensions.AspNetCore.Exceptions;
using ErtisAuth.WebAPI.Constants;

namespace ErtisAuth.WebAPI.Exceptions
{
	public class ErtisAuthException : ErtisException
	{
		#region Properties

		public new ErrorCodes ErrorCode { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor 1
		/// </summary>
		/// <param name="errorCode"></param>
		/// <param name="statusCode"></param>
		protected ErtisAuthException(ErrorCodes errorCode, HttpStatusCode statusCode) : base(statusCode, errorCode.ToString())
		{
			this.ErrorCode = errorCode;
		}

		/// <summary>
		/// Constructor 2
		/// </summary>
		/// <param name="errorCode"></param>
		/// <param name="statusCode"></param>
		/// <param name="message"></param>
		protected ErtisAuthException(ErrorCodes errorCode, HttpStatusCode statusCode, string message) : base(statusCode, message, errorCode.ToString())
		{
			this.ErrorCode = errorCode;
		}

		/// <summary>
		/// Constructor 3
		/// </summary>
		/// <param name="errorCode"></param>
		/// <param name="statusCode"></param>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		protected ErtisAuthException(ErrorCodes errorCode, HttpStatusCode statusCode, string message, Exception innerException) : base(statusCode, message, errorCode.ToString(), innerException)
		{
			this.ErrorCode = errorCode;
		}

		#endregion
	}
}