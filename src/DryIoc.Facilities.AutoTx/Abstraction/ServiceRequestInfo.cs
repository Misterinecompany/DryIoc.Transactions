using System;

namespace DryIoc.Facilities.AutoTx.Abstraction
{
	public class ServiceRequestInfo
	{
		private const string ProxyNamespace = "Castle.Proxies";

		public ServiceRequestInfo(RequestInfo requestInfo)
		{
			RequestInfo = requestInfo;
		}

		public RequestInfo RequestInfo { get; }

		public Type ImplementationType
		{
			get
			{
				var implementationType = RequestInfo.ImplementationType;
				return implementationType.Namespace == ProxyNamespace ? implementationType.BaseType : implementationType;
			}
		}
	}
}
