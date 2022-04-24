using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace ErtisAuth.Infrastructure.Helpers
{
    public static class QueryHelper
    {
        #region Methods

        private static string InjectValueToQuery<TDto>(string query, string key, string value)
        {
            var filterDefinition = new JsonFilterDefinition<TDto>(query);
            var queryJObject = JObject.Parse(filterDefinition.Json);
            if (queryJObject.ContainsKey("where"))
            {
                if (queryJObject["where"] is JObject whereClauseNode)
                {
                    if (whereClauseNode.ContainsKey(key))
                    {
                        whereClauseNode[key] = value;
                    }
                    else
                    {
                        whereClauseNode.Add(key, value);
                    }
					
                    return queryJObject.ToString();
                }
            }
            else
            {
                if (queryJObject.ContainsKey(key))
                {
                    queryJObject[key] = value;
                }
                else
                {
                    queryJObject.Add(key, value);
                }
					
                return queryJObject.ToString();
            }

            return query;
        }
        
        public static string InjectMembershipIdToQuery<TDto>(string query, string membershipId)
        {
            return InjectValueToQuery<TDto>(query, "membership_id", membershipId);
        }
        
        public static string InjectLocaleToQuery<TDto>(string query, string languageCode)
        {
            return InjectValueToQuery<TDto>(query, "locale", languageCode);
        }

        #endregion
    }
}