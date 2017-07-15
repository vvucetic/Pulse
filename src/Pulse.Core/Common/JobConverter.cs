using Pulse.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pulse.Core.Storage;

namespace Pulse.Core.Common
{
    public class JobConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Job);
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Load JObject from stream
            JObject jObject = JObject.Load(reader);

            //var parameterTypes
            //var methodName = jObject.GetValue("Method").ToObject<string>();
            //var type = System.Type.GetType(jObject.GetValue("Method").ToObject<string>(), throwOnError: true, ignoreCase: true); 
            //var method = type.GetNonOpenMatchingMethod(methodName, parameterTypes);

            //if (method == null)
            //{
            //    throw new InvalidOperationException(
            //        $"The type `{type.FullName}` does not contain a method with signature `{method}({String.Join(", ", parameterTypes.Select(x => x.Name))})`");
            //}

            // Create target object based on JObject
            var target = new InvocationData(
                type: jObject.GetValue("Type").ToObject<string>(),
                method: jObject.GetValue("Method").ToObject<string>(),
                arguments: jObject.GetValue("Arguments").ToObject<string>(),
                parameterTypes: jObject.GetValue("ParameterTypes").ToObject<string>()
                );

            // Populate the object properties
            //serializer.Populate(jObject.CreateReader(), target);

            return target.Deserialize();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Job job)
            {
                serializer.Serialize(writer, InvocationData.Serialize(job));
            }
        }
    }
}
