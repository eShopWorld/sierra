using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sierra.Api
{
    public class ActorDirectCallOptions
    {
        /// <summary>
        /// The optional list of assemblies which contains interfaces of actors. If not
        /// provided or if it's empty all referenced assemblies are used.
        /// </summary>
        public List<Assembly> InterfaceAssemblies { get; set; }

        /// <summary>
        /// The context used to locate actors.
        /// </summary>
        public StatelessServiceContext StatelessServiceContext { get; set; }
    }

    public static class ActorDirectCallExtensions
    {
        /// <summary>
        /// Enables handling of HTTP requests which are directly used to 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseActorDirectCall(this IApplicationBuilder builder, ActorDirectCallOptions options)
        {
            return builder.UseMiddleware<ActorDirectCallMiddleware>(options);
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
        private readonly Lazy<IEnumerable<Assembly>> _assemblies;
        private ConcurrentDictionary<string, ActorMethod> _actorMethods
            = new ConcurrentDictionary<string, ActorMethod>(StringComparer.OrdinalIgnoreCase);

        public ActorDirectCallMiddleware(RequestDelegate next, ActorDirectCallOptions options)
        {
            _next = next;
            _options = options;
            _assemblies = new Lazy<IEnumerable<Assembly>>(
                () => GetAssemblies(_options.InterfaceAssemblies));
        }

        public Task Invoke(HttpContext context)
        {
            var isTest = context.Request.Path.StartsWithSegments(
                "/test",
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
                if (!"application/json".Equals(context.Request.ContentType)) // TODO: is it enough?
                {
                    throw new Exception("only the application/json content type is accepted");
                }

                var actorMethod = FindActorMethdod(remaining);

                var jsonSerializedParameter = await ReadAsStringAsync(context.Request.Body);

                var parameterValue = JsonConvert.DeserializeObject(jsonSerializedParameter, actorMethod.Parameter.ParameterType);

                var result = await CallActor(actorMethod.InterfaceType, actorMethod.Method, parameterValue);

                if (result != null)
                {
                    context.Response.ContentType = "application/json";
                    var resultJson = JsonConvert.SerializeObject(result);
                    await context.Response.WriteAsync(resultJson);
                }
                else
                {
                    context.Response.StatusCode = 204;
                }
            }
            catch (Exception ex) // TODO: should some custom errors be used?
            {
                context.Response.StatusCode = 500;
                var errorMessage = new StringBuilder("Call failed because: ");
                var err = ex;
                while (err != null)
                {
                    errorMessage.Append(err.Message);
                    err = err.InnerException;
                }

                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(errorMessage.ToString());
            }
        }

        private ActorMethod FindActorMethdod(PathString remaining)
        {
            var remainingText = remaining.ToString();
            if (!_actorMethods.TryGetValue(remainingText, out var actorMethod))
            {

                var match = Regex.Match(remainingText, @"^/(\w+)/(\w+)$", RegexOptions.Compiled);
                if (!match.Success)
                {
                    throw new Exception("The request path doesn't match the required pattern.");
                }

                var actorName = match.Groups[1].Value;
                var methodName = match.Groups[2].Value;

                var interfaceType = FindActorInterface(_assemblies.Value, actorName);

                var method = interfaceType.GetMethod(methodName,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

                if (method == null)
                {
                    throw new Exception($"The interface {interfaceType.FullName} has no method {methodName}.");
                }

                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    throw new Exception(
                        $"The method {method.Name} with {parameters.Length} parameters is not supported.");
                }

                actorMethod = new ActorMethod(interfaceType, method, parameters[0]);
                _actorMethods[remainingText] = actorMethod;
            }

            return actorMethod;
        }

        private static async Task<string> ReadAsStringAsync(Stream stream)
        {
            using (var textStream = new StreamReader(stream, Encoding.UTF8))
            {
                return await textStream.ReadToEndAsync();
            }
        }

        private IEnumerable<Assembly> GetAssemblies(List<Assembly> assemblies)
        {
            if (assemblies == null || assemblies.Count == 0)
            {
                return Assembly
                    .GetEntryAssembly()
                    .GetReferencedAssemblies()
                    .Select(Assembly.Load)
                    .ToList();
            }
            else
            {
                return assemblies;
            }
        }


        private static Type FindActorInterface(IEnumerable<Assembly> actorInterfacesAssemblies, string actorName)
        {
            var actorInterfaceType = typeof(IActor);
            var interfaceName = $"I{actorName}Actor";
            var foundInterfaces = actorInterfacesAssemblies
                .SelectMany(x => x.GetExportedTypes())
                .Where(x => x.IsInterface && actorInterfaceType.IsAssignableFrom(x))
                .Where(x => interfaceName.Equals(x.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (foundInterfaces.Count == 0)
            {
                throw new Exception($"The interface {interfaceName} has not been found.");
            }

            if (foundInterfaces.Count > 1)
            {
                throw new Exception($"Found {foundInterfaces.Count} matching the name {interfaceName}.");
            }

            return foundInterfaces[0];
        }

        private async Task<object> CallActor(Type interfaceType, MethodInfo method, object parameterValue)
        {
            var actorId = ActorId.CreateRandom();

            var serviceName = interfaceType.Name + "Service";
            if (serviceName.StartsWith('I'))
                serviceName = serviceName.Substring(1);

            var actorUri = new Uri($"{_options.StatelessServiceContext.CodePackageActivationContext.ApplicationName}/{serviceName}");

            var actorProxy = CreateUntypedActorProxy(interfaceType, actorId, actorUri);

            var task = (Task)method.Invoke(actorProxy, new object[] { parameterValue });
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to call the actor. ", ex);
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
    }
}