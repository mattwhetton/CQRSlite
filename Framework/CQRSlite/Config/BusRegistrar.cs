using CQRSlite.Bus;
using CQRSlite.Commands;
using CQRSlite.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CQRSlite.Config
{
    public class BusRegistrar
    {
        private readonly IServiceLocator _serviceLocator;

        public BusRegistrar(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
        }

        public void Register(params Assembly[] assemblies)
        {
            var registrar = _serviceLocator.GetService<IHandlerRegistrar>();

            foreach (var executorsAssembly in assemblies)
            {
                var executorTypes = executorsAssembly
                    .GetTypes()
                    .Select(t => new { Type = t, Interfaces = ResolveMessageHandlerInterface(t) })
                    .Where(e => e.Interfaces != null && e.Interfaces.Any());

                foreach (var executorType in executorTypes)
                {
                    foreach (var @interface in executorType.Interfaces)
                    {
                        InvokeHandler(@interface, registrar, executorType.Type);
                    }
                }
            }
        }
        public void Register(params Type[] typesFromAssemblyContainingMessages)
        {
            Register(typesFromAssemblyContainingMessages.Select(t => t.GetTypeInfo().Assembly).ToArray());
        }

        private void InvokeHandler(Type @interface, IHandlerRegistrar registrar, Type executorType)
        {
            var commandType = @interface.GetGenericArguments()[0];

            var registerExecutorMethod = registrar
                .GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(mi => mi.Name == "RegisterHandler")
                .Where(mi => mi.IsGenericMethod)
                .Where(mi => mi.GetGenericArguments().Length == 1)
                .Single(mi => mi.GetParameters().Length == 1)
                .MakeGenericMethod(commandType);

            var del = new Func<dynamic, Task>(x =>
            {
                dynamic handler = _serviceLocator.GetService(executorType);
                if(handler == null)
                    throw new InvalidOperationException($"Cannot finds handler: {executorType}");
                return handler.Handle(x);
            });

            registerExecutorMethod.Invoke(registrar, new object[] { del });
        }

        private static IEnumerable<Type> ResolveMessageHandlerInterface(Type type)
        {
            return type
                .GetInterfaces()
                .Where(i => i.GetTypeInfo().IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                                                || i.GetGenericTypeDefinition() == typeof(IEventHandler<>)));
        }
    }
}
