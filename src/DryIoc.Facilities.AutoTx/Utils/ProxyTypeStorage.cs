using System;
using System.Collections.Generic;

namespace DryIoc.Facilities.AutoTx.Utils
{
    public class ProxyTypeStorage
    {
	    private readonly Dictionary<Type, Type> _Storage;

	    public ProxyTypeStorage()
	    {
			_Storage = new Dictionary<Type, Type>();
	    }

	    public void AddMapping(Type proxyType, Type implementationType)
	    {
			_Storage.Add(proxyType, implementationType);
	    }

	    public Type GetImplementationType(Type proxyType)
	    {
		    return _Storage[proxyType];
	    }
    }
}
