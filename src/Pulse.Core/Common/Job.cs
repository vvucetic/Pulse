using Pulse.Common;
using Pulse.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core.Common
{
    public class Job //: ISerializable
    {
        public Type Type { get; }

        public MethodInfo Method { get; }

        public IReadOnlyList<object> Arguments { get; }

        public Job(Type type, MethodInfo method, params object[] arguments)
        {
            this.Type = type;
            this.Method = method;
            this.Arguments = arguments;
        }

        //public Job(SerializationInfo info, StreamingContext text)
        //{
        //    var invocationData = (InvocationData)info.GetValue("job", typeof(InvocationData));
        //    var job = invocationData.Deserialize();
        //    this.Arguments = job.Arguments;
        //    this.Type = job.Type;
        //    this.Method = job.Method;
        //}

        public static Job FromExpression(Expression<Action> methodCall)
        {
            return FromExpression(methodCall, null);
        }

        public static Job FromExpression(Expression<Func<Task>> methodCall)
        {
            return FromExpression(methodCall, null);
        }

        public static Job FromExpression<TType>(Expression<Action<TType>> methodCall)
        {
            return FromExpression(methodCall, typeof(TType));
        }

        public static Job FromExpression<TType>(Expression<Func<TType, Task>> methodCall)
        {
            return FromExpression(methodCall, typeof(TType));
        }

        private static Job FromExpression(LambdaExpression methodCall, Type explicitType)
        {
            if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new ArgumentException("Expression body should be of type `MethodCallExpression`", nameof(methodCall));
            }

            var type = explicitType ?? callExpression.Method.DeclaringType;
            var method = callExpression.Method;

            if (explicitType == null && callExpression.Object != null)
            {
                //Creating a job that is based on a scope variable.We should infer its
                // type and method based on its value, and not from the expression tree.

                // TODO: BREAKING: Consider removing this special case entirely.
                // People consider that the whole object is serialized, this is not true.

                var objectValue = GetExpressionValue(callExpression.Object);
                if (objectValue == null)
                {
                    throw new InvalidOperationException("Expression object should be not null.");
                }

                // TODO: BREAKING: Consider using `callExpression.Object.Type` expression instead.
                type = objectValue.GetType();

                // If an expression tree is based on interface, we should use its own
                // MethodInfo instance, based on the same method name and parameter types.
                method = type.GetNonOpenMatchingMethod(
                    callExpression.Method.Name,
                    callExpression.Method.GetParameters().Select(x => x.ParameterType).ToArray());
            }

            return new Job(type, method, GetExpressionValues(callExpression.Arguments));
        }

        private static object[] GetExpressionValues(IEnumerable<Expression> expressions)
        {
            return expressions.Select(GetExpressionValue).ToArray();
        }

        private static object GetExpressionValue(Expression expression)
        {
            var constantExpression = expression as ConstantExpression;

            return constantExpression != null
                ? constantExpression.Value
                : CachedExpressionCompiler.Evaluate(expression);
        }

        public override string ToString()
        {
            return $"{Type.ToGenericTypeString()}.{Method.Name}";
        }

        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    info.AddValue("job", InvocationData.Serialize(this));
        //}
    }
}
