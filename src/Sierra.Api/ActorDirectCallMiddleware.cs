namespace Sierra.Api
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Eshopworld.Core;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Newtonsoft.Json;


    /// <summary>
    /// The parameters required by <see cref="ActorDirectCallMiddleware"/>;
    /// </summary>
    public class ActorDirectCallOptions
    {
        /// <summary>
        /// The optional list of assemblies which contains interfaces of actors. If not
        /// provided or if it's empty all referenced assemblies are used.
        /// </summary>
        // ReSharper disable once CollectionNeverUpdated.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public List<Assembly> InterfaceAssemblies { get; set; }

        /// <summary>
        /// The context used to locate actors.
        /// </summary>
        public StatelessServiceContext StatelessServiceContext { get; set; }

        /// <summary>
        /// The request path which is handled by the middleware.
        /// </summary>
        public PathString PathPrefix { get; set; } = new PathString("/test");
    }

    /// <summary>
    /// Extension methods used by <see cref="ActorDirectCallMiddleware"/>.
    /// </summary>
    public static class ActorDirectCallExtensions
    {
        /// <summary>
        /// Enables handling of HTTP requests which are directly used to 
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">The middleware's parameters.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseActorDirectCall(this IApplicationBuilder app, ActorDirectCallOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.StatelessServiceContext == null)
            {
                throw new ArgumentException("The StatelessServiceContext property must not be null", nameof(options));
            }

            if (options.PathPrefix.Value == null)
            {
                throw new ArgumentException("The PathPrefix property must not be null", nameof(options));
            }

            if (options.PathPrefix.Value.Length < 2 || options.PathPrefix.Value.EndsWith("/"))
            {
                throw new ArgumentException("The value of the PathPrefix property is invalid.", nameof(options));
            }

            return app.UseMiddleware<ActorDirectCallMiddleware>(options);
        }
    }

    /// <summary>
    /// The simple middleware which can send a request to a new actor of the specified type.
    /// </summary>
    /// <remarks>
    /// By design only actors with a specific interface schema are supported.
    /// </remarks>
    public class ActorDirectCallMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ActorDirectCallOptions _options;
        private readonly IBigBrother _bigBrother;
        private readonly Lazy<Dictionary<string, ActorMethod>> _actorMethods;

        /// <summary>
        /// Constructs the instance of <see cref="ActorDirectCallMiddleware"/>.
        /// </summary>
        /// <param name="next">The next request middleware handler.</param>
        /// <param name="options">The middleware parameters.</param>
        /// <param name="bigBrother">The telemetry sink.</param>
        public ActorDirectCallMiddleware(RequestDelegate next, ActorDirectCallOptions options, IBigBrother bigBrother)
        {
            _next = next;
            _options = options;
            _bigBrother = bigBrother;
            _actorMethods = new Lazy<Dictionary<string, ActorMethod>>(
                () => CreateActorMethodsDictionary(GetAssemblies(_options.InterfaceAssemblies)));
        }

        /// <summary>
        /// Processes the HTTP request.
        /// </summary>
        /// <param name="context">The HTTP request context</param>
        /// <returns>The task.</returns>
        public Task Invoke(HttpContext context)
        {
            var isTest = context.Request.Path.StartsWithSegments(
                _options.PathPrefix,
                StringComparison.OrdinalIgnoreCase,
                out var remaining);
            if (!isTest)
            {
                return _next(context);
            }

            // TODO: any authorization?

            return PrepareActorCall(context, remaining);
        }

        private async Task PrepareActorCall(HttpContext context, PathString remaining)
        {
            try
            {
                //as per rfc, this may contain charset (and indeed other parameters)
                if (!context.Request.ContentType.StartsWith("application/json", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ActorCallFailedException("only the application/json content type is accepted");
                }

                var actorMethod = FindActorMethod(remaining);

                var jsonSerializedParameter = await ReadAsStringAsync(context.Request.Body);

                var parameterValue = JsonConvert.DeserializeObject(jsonSerializedParameter, actorMethod.Parameter.ParameterType);

                var actorId = context.Request.Query["actorId"];

                var result = await CallActor(actorId, actorMethod.InterfaceType, actorMethod.Method, parameterValue);

                if (result != null)
                {
                    context.Response.ContentType = "application/json";
                    var resultJson = JsonConvert.SerializeObject(result);
                    await context.Response.WriteAsync(resultJson);
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                }
            }
            catch (Exception ex)
            {
                _bigBrother.Publish(ex.ToExceptionEvent());

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var error = new
                {
                    errors = ListInnerExceptions(ex)
                        .Where(x => !(x is AggregateException ag && ag.InnerExceptions.Count == 1))
                        .Select(x => new { message = x.Message, type = x.GetType().FullName })
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(error));
            }
        }

        private static IEnumerable<Exception> ListInnerExceptions(Exception ex)
        {
            do
            {
                yield return ex;
            } while ((ex = ex.InnerException) != null);
        }

        private ActorMethod FindActorMethod(PathString remaining)
        {
            var remainingText = remaining.ToString();
            if (_actorMethods.Value.TryGetValue(remainingText, out var actorMethod))
                return actorMethod;

            // No valid method matched the request. Find out why.
            var match = Regex.Match(remainingText, @"^/(\w+)/(\w+)$", RegexOptions.Compiled);
            if (!match.Success)
            {
                throw new ActorCallFailedException("The request path doesn't match the required pattern.");
            }

            var actorName = match.Groups[1].Value;
            var methodName = match.Groups[2].Value;

            var actorInterfaces = _actorMethods.Value
                .Values
                .GroupBy(x => x.InterfaceType)
                .Select(x => x.Key.Name)
                .OrderBy(x => x)
                .ToList();

            throw new ActorCallFailedException(
                $"Failed to find the I{actorName}Actor interface with a valid {methodName} method. Recognized interfaces are: {string.Join(", ", actorInterfaces)}.");
        }

        private static async Task<string> ReadAsStringAsync(Stream stream)
        {
            using (var textStream = new StreamReader(stream, Encoding.UTF8))
            {
                return await textStream.ReadToEndAsync();
            }
        }

        private static Dictionary<string, ActorMethod> CreateActorMethodsDictionary(IEnumerable<Assembly> assemblies)
        {
            var methods = from assembly in GetAssemblies(assemblies)
                          from interfaceType in FindActorInterfaces(assembly)
                          from methodProperty in FindCallableActorMethods(interfaceType)
                          select new ActorMethod(interfaceType, methodProperty.method, methodProperty.parameter);

            return methods.ToDictionary(
                x => $"/{GetCoreActorName(x.InterfaceType.Name)}/{x.Method.Name}",
                x => x,
                StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<Assembly> GetAssemblies(IEnumerable<Assembly> assemblies)
        {
            var list = assemblies?.ToList();
            if (list == null || list.Count == 0)
            {
                return Assembly
                    .GetEntryAssembly()
                    .GetReferencedAssemblies()
                    .Select(Assembly.Load)
                    .ToList();
            }
            else
            {
                return list;
            }
        }

        private static IEnumerable<Type> FindActorInterfaces(Assembly assembly)
        {
            var actorInterfaceType = typeof(IActor);
            return from t in assembly.GetExportedTypes()
                   where GetCoreActorName(t.Name) != null
                   where t.IsInterface && actorInterfaceType.IsAssignableFrom(t)
                   select t;
        }

        private static string GetCoreActorName(string interfaceName)
        {
            var match = Regex.Match(interfaceName, @"I(\w+)Actor", RegexOptions.Compiled);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static IEnumerable<(MethodInfo method, ParameterInfo parameter)> FindCallableActorMethods(
            Type interfaceType)
        {
            return
                from m in interfaceType.GetMethods(
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)
                let p = m.GetParameters()
                where p.Length == 1
                select (m, p[0]);
        }

        private async Task<object> CallActor(string actorId, Type interfaceType, MethodInfo method, object parameterValue)
        {
            var actorIdNative = actorId != null ? new ActorId(actorId) : ActorId.CreateRandom();

            var serviceName = interfaceType.Name + "Service";
            if (serviceName.StartsWith('I'))
                serviceName = serviceName.Substring(1);

            var actorUri = new Uri($"{_options.StatelessServiceContext.CodePackageActivationContext.ApplicationName}/{serviceName}");

            var actorProxy = CreateUntypedActorProxy(interfaceType, actorIdNative, actorUri);

            var task = (Task)method.Invoke(actorProxy, new[] { parameterValue });
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                throw new ActorCallFailedException("Failed to call the actor. ", ex);
            }

            var resultProperty = task.GetType().GetProperty("Result");

            return resultProperty?.GetValue(task);
        }

        private static object CreateUntypedActorProxy(Type interfaceType, ActorId actorId, Uri serviceUri)
        {
            var method = typeof(ActorDirectCallMiddleware).GetMethod(nameof(CreateActorProxyHelper), BindingFlags.NonPublic | BindingFlags.Static);
            var genericMethod = method.MakeGenericMethod(interfaceType);
            return genericMethod.Invoke(null, new object[] { actorId, serviceUri });
        }

        private static T CreateActorProxyHelper<T>(ActorId actorId, Uri serviceUri)
            where T : IActor
        {
            return ActorProxy.Create<T>(actorId, serviceUri);
        }

        private sealed class ActorMethod
        {
            public Type InterfaceType { get; }

            public MethodInfo Method { get; }

            public ParameterInfo Parameter { get; }

            public ActorMethod(Type interfaceType, MethodInfo method, ParameterInfo parameter)
            {
                InterfaceType = interfaceType;
                Method = method;
                Parameter = parameter;
            }
        }

        /// <summary>
        /// The internal exception used by <see cref="ActorDirectCallMiddleware"/>.
        /// </summary>
        [Serializable]
        public sealed class ActorCallFailedException : Exception
        {
            /// <inheritdoc />
            public ActorCallFailedException(string message)
                : base(message)
            {
            }

            /// <inheritdoc />
            public ActorCallFailedException(string message, Exception inner)
                : base(message, inner)
            {
            }

            private ActorCallFailedException(
                System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }
    }
}
