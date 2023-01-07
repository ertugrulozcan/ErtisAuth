using System;
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

        public static string InjectMembershipIdToAggregation(string query, string membershipId)
        {
        	try
        	{
        		var jArray = JArray.Parse(query);
        	
        		var hasMatchToken = false;
        		foreach (var jToken in jArray)
        		{
        			if (jToken is JObject jObject)
        			{
        				if (jObject.ContainsKey("$match") && jObject["$match"] is JObject)
        				{
        					hasMatchToken = true;
        				}
        			}
        		}

        		if (hasMatchToken)
        		{
        			foreach (var jToken in jArray)
        			{
        				if (jToken is JObject jObject)
        				{
        					if (jObject.ContainsKey("$match") && jObject["$match"] is JObject matchTokenObject)
        					{
        						if (matchTokenObject.ContainsKey("membership_id") && matchTokenObject["membership_id"] != null)
        						{
        							if (matchTokenObject["membership_id"].Value<string>() == membershipId)
        							{
        								return query;
        							}
        							else
        							{
        								matchTokenObject["membership_id"] = new JValue(membershipId);
        								break;
        							}
        						}
        						else
        						{
        							matchTokenObject.AddFirst(new JProperty("membership_id", new JValue(membershipId)));
        							break;
        						}
        					}
        				}
        			}
        		}
        		else
        		{
        			var membershipIdToken = new JProperty("membership_id", new JValue(membershipId));
        			var matchToken = new JObject
        			{
        				["$match"] = new JObject(membershipIdToken)
        			};
        		
        			jArray.AddFirst(matchToken);	
        		}
        	
        		return jArray.ToString();
        	}
        	catch (Exception ex)
        	{
        		Console.WriteLine("InjectMembershipIdToAggregation method occured an error: " + ex.Message);
        	}

        	return query;
        }

        #endregion
    }
}