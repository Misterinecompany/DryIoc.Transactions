using System;
using Castle.DynamicProxy;

namespace DryIoc.Facilities.AutoTx.Utils
{
	public class ParentServiceRequestInfo
	{
		private readonly ProxyTypeStorage _ProxyTypeStorage;

		public ParentServiceRequestInfo(Request requestInfo, ProxyTypeStorage proxyTypeStorage)
		{
			_ProxyTypeStorage = proxyTypeStorage;
			RequestInfo = requestInfo.Parent.Parent;
		}

		public Request RequestInfo { get; }

		public Type ImplementationType
		{
			get
			{
				var implementationType = RequestInfo.ImplementationType;
				var result = ProxyUtil.IsProxyType(implementationType)
					? _ProxyTypeStorage.GetImplementationType(implementationType)
					: implementationType;
				return result;
			}
		}
	}
}
