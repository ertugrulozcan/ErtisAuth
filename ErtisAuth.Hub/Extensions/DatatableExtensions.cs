using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ErtisAuth.Hub.Constants;
using Ertis.Core.Collections;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Models.Webhooks;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.Hub.Extensions
{
    public static class DatatableExtensions
    {
        #region DateTime Methods

        private static string DateTimeToString(this DateTime dateTime)
        {
            return dateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("en-US"));
        }

        private static string DateTimeToString(this DateTime? dateTime)
        {
            return dateTime?.ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("en-US"));
        }

        #endregion

        #region Pagination Methods

        public static void ExtractPaginationParameters(
            this ControllerBase controller,
            string[] columns,
            out int? skip,
            out int? limit,
            out int? itemCountPerPage,
            out string orderBy,
            out SortDirection sortDirection,
            out string searchKeyword)
        {
            var queryDictionary = controller.Request.Query.ToDictionary(x => x.Key, y => y.Value.ToString());
            controller.ExtractPaginationParameters(queryDictionary, columns, out skip, out limit, out itemCountPerPage, out orderBy, out sortDirection, out searchKeyword);
        }

        public static void ExtractPaginationParameters(
            this ControllerBase controller,
            IDictionary<string, string> parameters,
            string[] columns,
            out int? skip,
            out int? limit,
            out int? itemCountPerPage,
            out string orderBy,
            out SortDirection sortDirection,
            out string searchKeyword)
        {
            if (parameters.ContainsKey("start") && int.TryParse(parameters["start"], out int skipValue))
            {
                skip = skipValue;
            }
            else
            {
                skip = null;
            }

            if (parameters.ContainsKey("length") && int.TryParse(parameters["length"], out int limitValue))
            {
                limit = limitValue;
            }
            else
            {
                limit = null;
            }

            if (parameters.ContainsKey("draw") && int.TryParse(parameters["draw"], out int drawValue))
            {
                itemCountPerPage = drawValue;
            }
            else
            {
                itemCountPerPage = null;
            }

            if (parameters.ContainsKey("order[0][column]") && int.TryParse(parameters["order[0][column]"], out int orderByColumnIndex) && orderByColumnIndex >= 0 && orderByColumnIndex < columns.Length)
            {
                orderBy = columns[orderByColumnIndex];
            }
            else
            {
                orderBy = null;
            }

            if (parameters.ContainsKey("order[0][dir]"))
            {
                if (parameters["order[0][dir]"] == "desc")
                {
                    sortDirection = SortDirection.Descending;
                }
                else
                {
                    sortDirection = SortDirection.Ascending;
                }
            }
            else
            {
                sortDirection = SortDirection.Ascending;
            }

            if (parameters.ContainsKey("search[value]") && !string.IsNullOrEmpty(parameters["search[value]"]))
            {
                searchKeyword = parameters["search[value]"];
            }
            else
            {
                searchKeyword = null;
            }
        }

        #endregion

        #region User Extensions

        public static object[] GetDataTableProperties(this User user)
        {
            const string photoUrl = AssetConstants.UserProfileImagePlaceHolderPath;

            return new object[]
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Username,
				user.EmailAddress,
                user.Role,
                user.Sys?.CreatedAt != null ? user.Sys.CreatedAt.DateTimeToString() : string.Empty,
                user.Sys?.CreatedBy ?? string.Empty,
                user.Sys?.ModifiedAt != null ? user.Sys.ModifiedAt.DateTimeToString() : string.Empty,
                user.Sys?.ModifiedBy ?? string.Empty,
                photoUrl
            };
        }

        #endregion

        #region Role Extensions

        public static object[] GetDataTableProperties(this Role role)
        {
            return new object[]
            {
                role.Id,
                role.Name,
                role.Description,
                role.Sys?.CreatedAt != null ? role.Sys.CreatedAt.DateTimeToString() : string.Empty,
                role.Sys?.CreatedBy ?? string.Empty,
                role.Sys?.ModifiedAt != null ? role.Sys.ModifiedAt.DateTimeToString() : string.Empty,
                role.Sys?.ModifiedBy ?? string.Empty,
            };
        }

        #endregion

        #region Application Extensions

        public static object[] GetDataTableProperties(this Application application)
        {
            return new object[]
            {
                application.Id,
                application.Name,
                application.Role,
                application.Sys?.CreatedAt != null ? application.Sys.CreatedAt.DateTimeToString() : string.Empty,
                application.Sys?.CreatedBy ?? string.Empty,
                application.Sys?.ModifiedAt != null ? application.Sys.ModifiedAt.DateTimeToString() : string.Empty,
                application.Sys?.ModifiedBy ?? string.Empty,
            };
        }

        #endregion

        #region Membership Extensions

        public static object[] GetDataTableProperties(this Membership membership)
        {
            return new object[]
            {
                membership.Id,
                membership.Name,
                membership.Sys?.CreatedAt != null ? membership.Sys.CreatedAt.DateTimeToString() : string.Empty,
                membership.Sys?.CreatedBy ?? string.Empty,
                membership.Sys?.ModifiedAt != null ? membership.Sys.ModifiedAt.DateTimeToString() : string.Empty,
                membership.Sys?.ModifiedBy ?? string.Empty,
            };
        }

        #endregion

        #region Provider Extensions

        public static object[] GetDataTableProperties(this OAuthProvider provider)
        {
            return new object[]
            {
                provider.Id,
                provider.Name,
                provider.Description,
                provider.Sys?.CreatedAt != null ? provider.Sys.CreatedAt.DateTimeToString() : string.Empty,
                provider.Sys?.CreatedBy ?? string.Empty,
                provider.Sys?.ModifiedAt != null ? provider.Sys.ModifiedAt.DateTimeToString() : string.Empty,
                provider.Sys?.ModifiedBy ?? string.Empty,
            };
        }

        #endregion

        #region Webhook Extensions

        public static object[] GetDataTableProperties(this Webhook webhook)
        {
            return new object[]
            {
                webhook.Id,
                webhook.Name,
                webhook.Event,
                webhook.Status,
                webhook.Sys?.CreatedAt != null ? webhook.Sys.CreatedAt.DateTimeToString() : string.Empty,
                webhook.Sys?.CreatedBy ?? string.Empty,
                webhook.Sys?.ModifiedAt != null ? webhook.Sys.ModifiedAt.DateTimeToString() : string.Empty,
                webhook.Sys?.ModifiedBy ?? string.Empty,
            };
        }

        #endregion
        
        #region ActiveToken Extensions

        public static object[] GetDataTableProperties(this ActiveToken activeToken, bool isCurrentSession = false)
        {
            return new object[]
            {
                activeToken.Id,
                activeToken.UserId,
                activeToken.FirstName,
                activeToken.LastName,
                activeToken.UserName,
                activeToken.EmailAddress,
                activeToken.CreatedAt,
                activeToken.ExpiresIn,
                activeToken.ExpireTime,
                activeToken.ClientInfo,
                isCurrentSession
            };
        }

        #endregion
    }
}