using System;
using System.Collections.Generic;
using System.Linq;
using ErtisAuth.Core.Models.Events;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ErtisAuth.Hub.ViewModels.Events
{
    public class EventsViewModel : ViewModelBase
	{
		#region Properties

		public List<SelectListItem> EventTypeFilterList { get; set; } = FillEventTypeFilterList();

		#endregion

		#region Methods

		private static List<SelectListItem> FillEventTypeFilterList()
		{
			var eventTypeFilterList = new List<SelectListItem> { new SelectListItem("All", "*", true) };
			var ertisAuthEventTypes = Enum.GetValues(typeof(ErtisAuthEventType)).Cast<ErtisAuthEventType>();
			foreach (var ertisAuthEventType in ertisAuthEventTypes)
			{
				eventTypeFilterList.Add(GetErtisAuthEventSelectListItem(ertisAuthEventType));
			}
			
			return eventTypeFilterList;
		}

		private static SelectListItem GetErtisAuthEventSelectListItem(ErtisAuthEventType ertisAuthEventType)
		{
			switch (ertisAuthEventType)
			{
				case ErtisAuthEventType.TokenGenerated:
					return new SelectListItem("Generate Token", ertisAuthEventType.ToString());
				case ErtisAuthEventType.TokenRefreshed:
					return new SelectListItem("Refresh Token", ertisAuthEventType.ToString());
				case ErtisAuthEventType.TokenVerified:
					return new SelectListItem("Verify Token", ertisAuthEventType.ToString());
				case ErtisAuthEventType.TokenRevoked:
					return new SelectListItem("Revoke Token", ertisAuthEventType.ToString());
				case ErtisAuthEventType.UserCreated:
					return new SelectListItem("Create User", ertisAuthEventType.ToString());
				case ErtisAuthEventType.UserUpdated:
					return new SelectListItem("Update User", ertisAuthEventType.ToString());
				case ErtisAuthEventType.UserDeleted:
					return new SelectListItem("Delete User", ertisAuthEventType.ToString());
				case ErtisAuthEventType.UserPasswordReset:
					return new SelectListItem("Password Reset", ertisAuthEventType.ToString());
				case ErtisAuthEventType.UserPasswordChanged:
					return new SelectListItem("Password Change", ertisAuthEventType.ToString());
				case ErtisAuthEventType.ApplicationCreated:
					return new SelectListItem("Create Application", ertisAuthEventType.ToString());
				case ErtisAuthEventType.ApplicationUpdated:
					return new SelectListItem("Update Application", ertisAuthEventType.ToString());
				case ErtisAuthEventType.ApplicationDeleted:
					return new SelectListItem("Delete Application", ertisAuthEventType.ToString());
				case ErtisAuthEventType.RoleCreated:
					return new SelectListItem("Create Role", ertisAuthEventType.ToString());
				case ErtisAuthEventType.RoleUpdated:
					return new SelectListItem("Update Role", ertisAuthEventType.ToString());
				case ErtisAuthEventType.RoleDeleted:
					return new SelectListItem("Delete Role", ertisAuthEventType.ToString());
				case ErtisAuthEventType.ProviderCreated:
					return new SelectListItem("Create Provider", ertisAuthEventType.ToString());
				case ErtisAuthEventType.ProviderUpdated:
					return new SelectListItem("Update Provider", ertisAuthEventType.ToString());
				case ErtisAuthEventType.ProviderDeleted:
					return new SelectListItem("Delete Provider", ertisAuthEventType.ToString());
				case ErtisAuthEventType.WebhookCreated:
					return new SelectListItem("Create Webhook", ertisAuthEventType.ToString());
				case ErtisAuthEventType.WebhookUpdated:
					return new SelectListItem("Update Webhook", ertisAuthEventType.ToString());
				case ErtisAuthEventType.WebhookDeleted:
					return new SelectListItem("Delete Webhook", ertisAuthEventType.ToString());
				case ErtisAuthEventType.WebhookRequestSent:
					return new SelectListItem("Webhook Request Sent", ertisAuthEventType.ToString());
				case ErtisAuthEventType.WebhookRequestFailed:
					return new SelectListItem("Webhook Request Failed", ertisAuthEventType.ToString());
				default:
					throw new ArgumentOutOfRangeException(nameof(ertisAuthEventType), ertisAuthEventType, null);
			}
		}

		#endregion
	}
}