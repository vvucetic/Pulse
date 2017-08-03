using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Common
{

    public static class JobHelper
    {
        //private static JsonSerializerSettings _serializerSettings;

        //public static void SetSerializerSettings(JsonSerializerSettings setting)
        //{
        //    _serializerSettings = setting;
        //}
        public static JsonSerializerSettings GetDefaultSettings()
        {
            var jsonSerializerSettings = JsonConvert.DefaultSettings != null ? JsonConvert.DefaultSettings() : new JsonSerializerSettings();
            if (jsonSerializerSettings != null)
            {
                if (jsonSerializerSettings.Converters == null)
                    jsonSerializerSettings.Converters = new List<JsonConverter>();
                jsonSerializerSettings.Converters.Add(new JobConverter());
            }
            return jsonSerializerSettings;
        }

        public static string ToJson(object value)
        {
            return value != null
                ? JsonConvert.SerializeObject(value, GetDefaultSettings())
                : null;
        }

        public static T FromJson<T>(string value)
        {
            return value != null
                ? JsonConvert.DeserializeObject<T>(value, GetDefaultSettings())
                : default(T);            
        }

        public static object FromJson(string value, Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return value != null
                ? JsonConvert.DeserializeObject(value, type, GetDefaultSettings())
                : null;
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime FromTimestamp(long value)
        {
            return Epoch.AddSeconds(value);
        }
        
        public static string SerializeDateTime(DateTime value)
        {
            return value.ToString("o", CultureInfo.InvariantCulture);
        }

        public static DateTime DeserializeDateTime(string value)
        {
            if (long.TryParse(value, out long timestamp))
            {
                return FromTimestamp(timestamp);
            }

            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        public static DateTime? DeserializeNullableDateTime(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            return DeserializeDateTime(value);
        }
    }
}
