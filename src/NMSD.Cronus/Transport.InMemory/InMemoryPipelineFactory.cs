﻿using System;
using NMSD.Cronus.Transports;

namespace NMSD.Cronus.Transport.InMemory
{
    public class InMemoryPipelineFactory : IPipelineFactory
    {
        private readonly IPipelineNameConvention pipelineNameConvention;

        public InMemoryPipelineFactory(IPipelineNameConvention pipelineNameConvention)
        {
            this.pipelineNameConvention = pipelineNameConvention;
        }

        public IPipeline GetPipeline(string pipelineName)
        {
            return InMemoryQueue.Current.GetOrAddPipeline(pipelineName);
        }

        public IPipeline GetPipeline(Type messageType)
        {
            string pipelineName = pipelineNameConvention.GetPipelineName(messageType);
            return GetPipeline(pipelineName);
        }
    }
}