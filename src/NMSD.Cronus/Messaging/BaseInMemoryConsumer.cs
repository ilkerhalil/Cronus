using System;
using System.Linq;
using System.Collections.Generic;
using NMSD.Cronus.UnitOfWork;

namespace NMSD.Cronus.Messaging
{
    public abstract class BaseInMemoryConsumer<TMessage, TMessageHandler> : IConsumer<TMessageHandler>
        where TMessage : IMessage
        where TMessageHandler : IMessageHandler
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(BaseInMemoryConsumer<TMessage, TMessageHandler>));

        public IUnitOfWorkFactory UnitOfWorkFactory { get; set; }

        protected Dictionary<Type, HashSet<Type>> RegisteredHandlers = new Dictionary<Type, HashSet<Type>>();

        protected Dictionary<Type, Dictionary<Type, LateBoundVoidMethod>> handlerCallbacks = new Dictionary<Type, Dictionary<Type, LateBoundVoidMethod>>();

        private Dictionary<Type, List<Func<TMessage, IUnitOfWorkPerMessage, bool>>> handlers = new Dictionary<Type, List<Func<TMessage, IUnitOfWorkPerMessage, bool>>>();

        public Dictionary<Type, List<Func<TMessage, IUnitOfWorkPerMessage, bool>>> Handlers { get { return handlers; } set { handlers = value; } }

        public virtual void RegisterHandler(Type messageType, Type messageHandlerType, Func<Type, TMessageHandler> handlerFactory)
        {
            if (!RegisteredHandlers.ContainsKey(messageHandlerType))
                RegisteredHandlers.Add(messageHandlerType, new HashSet<Type>());
            RegisteredHandlers[messageHandlerType].Add(messageType);

            if (!handlers.ContainsKey(messageType))
                handlers[messageType] = new List<Func<TMessage, IUnitOfWorkPerMessage, bool>>();

            handlers[messageType].Add((x, y) => Handle(x, y, messageHandlerType, handlerFactory));

            if (!handlerCallbacks.ContainsKey(messageHandlerType))
            {
                var callbacks = new Dictionary<Type, LateBoundVoidMethod>();
                var handleMethodInfos = messageHandlerType.GetMethods().Where(x => x.Name == "Handle");
                foreach (var mi in handleMethodInfos)
                {
                    var callback = DelegateFactory.Create(mi);
                    callbacks.Add(mi.GetParameters().First().ParameterType, callback);
                }
                handlerCallbacks.Add(messageHandlerType, callbacks);
            }
        }

        public abstract void Start(int numberOfWorkers);

        public abstract void Stop();

        protected bool Handle(TMessage message, IUnitOfWorkPerBatch uowBatch)
        {
            var unitOfWork = UnitOfWorkFactory.NewMessage();
            unitOfWork.UoWBatch = uowBatch;
            if (unitOfWork != null)
                unitOfWork.Begin();
            List<Func<TMessage, IUnitOfWorkPerMessage, bool>> availableHandlers;
            if (handlers.TryGetValue(message.GetType(), out availableHandlers))
            {
                foreach (var handleMethod in availableHandlers)
                {
                    var result = handleMethod(message, unitOfWork);
                    if (result == false)
                        return result;
                }
            }
            if (unitOfWork != null)
            {
                unitOfWork.Commit();
                unitOfWork.Dispose();
            }
            return true;
        }

        protected bool Handle(TMessage message, IUnitOfWorkPerMessage uowMessage, Type eventHandlerType, Func<Type, TMessageHandler> handlerFactory)
        {
            var unitOfWork = UnitOfWorkFactory.NewHandler();
            unitOfWork.UoWMessage = uowMessage;
            unitOfWork.Begin();
            var handler = handlerFactory(eventHandlerType);
            handler = unitOfWork.Resolver.ResolveDependancies(handler);
            handlerCallbacks[eventHandlerType][message.GetType()](handler, new object[] { message });
            unitOfWork.Commit();
            if (log.IsInfoEnabled)
                log.Info("HANDLE => " + message.ToString());

            return true;
        }
    }
}