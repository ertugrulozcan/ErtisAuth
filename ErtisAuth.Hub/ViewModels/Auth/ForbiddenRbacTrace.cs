using System.Collections.Generic;

namespace ErtisAuth.Hub.ViewModels.Auth
{
    public static class ForbiddenRbacTrace
    {
        private static readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>();

        public static void Push(string key, string value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary.Add(key, value);
            }
            else
            {
                _dictionary[key] = value;
            }
        }
		
        public static string Pop(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
			
            if (_dictionary.ContainsKey(key))
            {
                var value = _dictionary[key];
                _dictionary.Remove(key);
                return value;
            }

            return null;
        }
    }
}