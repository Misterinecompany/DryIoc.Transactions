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

	    public bool TryAddMapping(Type proxyType, Type implementationType)
	    {
		    if (_Storage.ContainsKey(proxyType))
			    return false;

			_Storage.Add(proxyType, implementationType);
		    return true;
	    }

	    public Type GetImplementationType(Type proxyType)
	    {
		    return _Storage[proxyType];
	    }
    }
}
