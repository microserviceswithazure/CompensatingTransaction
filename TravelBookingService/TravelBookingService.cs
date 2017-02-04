using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace TravelBookingService
{
    using System.Fabric.Description;
    using System.Net;
    using System.Text;

    using Microsoft.ServiceFabric.Services.Client;

    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class TravelBookingService : StatelessService
    {
        private StatelessServiceContext context;

        public TravelBookingService(StatelessServiceContext context)
            : base(context)
        {
            this.context = context;

        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[] { new ServiceInstanceListener(context => this.CreateInputListener(context)) };
        }

        private ICommunicationListener CreateInputListener(ServiceContext context)
        {
            EndpointResourceDescription inputEndpoint = context.CodePackageActivationContext.GetEndpoint("WebEndpoint");
            string uriPrefix = String.Format("{0}://+:{1}/travelservice/", inputEndpoint.Protocol, inputEndpoint.Port);
            string uriPublished = uriPrefix.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);
            return new HttpCommunicationListener(uriPrefix, uriPublished, this.ProcessInputRequest);
        }

        private Task ProcessInputRequest(HttpListenerContext context, CancellationToken cancellationToken)
        {
            byte[] buffer = Encoding.UTF8.GetBytes("Hello World");
            context.Response.AddHeader("Connection", "close");
            context.Response.ContentType = "application/json; charset=utf-8";

            for (int i = 0; i < 10; ++i)
            {
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Flush();
            }

            context.Response.Close();

         

            return Task.FromResult(1);
        }
    }
}
