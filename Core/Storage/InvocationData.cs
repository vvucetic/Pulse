using Core.Common;
using Core.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Storage
{
    public class InvocationData
    {
        public InvocationData(string type, string method, string parameterTypes, string arguments)
        {
            Type = type;
            Method = method;
            ParameterTypes = parameterTypes;
            Arguments = arguments;
        }

        public string Type { get; }
        public string Method { get; }
        public string ParameterTypes { get; }
        public string Arguments { get; set; }

        public Job Deserialize()
        {
            try
            {
                var type = System.Type.GetType(Type, throwOnError: true, ignoreCase: true);
                var parameterTypes = JobHelper.FromJson<Type[]>(ParameterTypes);
                var method = type.GetNonOpenMatchingMethod(Method, parameterTypes);

                if (method == null)
                {
                    throw new InvalidOperationException(
                        $"The type `{type.FullName}` does not contain a method with signature `{Method}({String.Join(", ", parameterTypes.Select(x => x.Name))})`");
                }

                var serializedArguments = JobHelper.FromJson<string[]>(Arguments);
                var arguments = DeserializeArguments(method, serializedArguments);

                return new Job(type, method, arguments);
            }
            catch (Exception ex)
            {
                throw new InternalErrorException("Could not load the job. See inner exception for the details.", ex);
            }
        }

        internal static object[] DeserializeArguments(MethodInfo methodInfo, string[] arguments)
        {
            var parameters = methodInfo.GetParameters();
            var result = new List<object>(arguments.Length);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var argument = arguments[i];
                
                object value = DeserializeArgument(argument, parameter.ParameterType);

                result.Add(value);
            }

            return result.ToArray();
        }

        private static object DeserializeArgument(string argument, Type type)
        {
            object value;
            try
            {
                value = argument != null
                    ? JobHelper.FromJson(argument, type)
                    : null;
            }
            catch (Exception jsonException)
            {
                if (type == typeof(object))
                {
                    // Special case for handling object types, because string can not
                    // be converted to object type.
                    value = argument;
                }
                else
                {
                    try
                    {
                        var converter = TypeDescriptor.GetConverter(type);

                        // ReferenceConverter can't correctly convert the serialized
                        // data. This may happen when FromJson method threw an exception,
                        // we should rethrow it instead of trying to deserialize.
                        if (converter.GetType() == typeof(ReferenceConverter))
                        {
                            ExceptionDispatchInfo.Capture(jsonException).Throw();
                            throw;
                        }

                        value = converter.ConvertFromInvariantString(argument);
                    }
                    catch (Exception)
                    {
                        ExceptionDispatchInfo.Capture(jsonException).Throw();
                        throw;
                    }
                }
            }
            return value;
        }

        public static InvocationData Serialize(Job job)
        {
            return new InvocationData(
                job.Type.AssemblyQualifiedName,
                job.Method.Name,
                JobHelper.ToJson(job.Method.GetParameters().Select(x => x.ParameterType).ToArray()),
                JobHelper.ToJson(SerializeArguments(job.Arguments)));
        }

        internal static string[] SerializeArguments(IReadOnlyCollection<object> arguments)
        {
            var serializedArguments = new List<string>(arguments.Count);
            foreach (var argument in arguments)
            {
                string value;

                if (argument != null)
                {
                    if (argument is DateTime)
                    {
                        value = ((DateTime)argument).ToString("o", CultureInfo.InvariantCulture);
                    }
                    else if (argument is CancellationToken)
                    {
                        // CancellationToken type instances are substituted with ShutdownToken 
                        // during the background job performance, so we don't need to store 
                        // their values.
                        value = null;
                    }
                    else
                    {
                        value = JobHelper.ToJson(argument);
                    }
                }
                else
                {
                    value = null;
                }

                // Logic, related to optional parameters and their default values, 
                // can be skipped, because it is impossible to omit them in 
                // lambda-expressions (leads to a compile-time error).

                serializedArguments.Add(value);
            }

            return serializedArguments.ToArray();
        }

    }
}
