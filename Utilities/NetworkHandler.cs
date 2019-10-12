using RoR2.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

class NetworkHandler
{

    public static FieldInfo get_serverMessageHandlers = typeof(NetworkMessageHandlerAttribute).GetField("serverMessageHandlers", BindingFlags.NonPublic | BindingFlags.Static);
    public static FieldInfo get_clientMessageHandlers = typeof(NetworkMessageHandlerAttribute).GetField("clientMessageHandlers", BindingFlags.NonPublic | BindingFlags.Static);
    public static FieldInfo get_messageHandler = typeof(NetworkMessageHandlerAttribute).GetField("messageHandler", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void RegisterNetworkHandlerAttributes()
    {

        List<NetworkMessageHandlerAttribute> serverMessageHandlers = (List<NetworkMessageHandlerAttribute>)get_serverMessageHandlers.GetValue(null);
        List<NetworkMessageHandlerAttribute> clientMessageHandlers = (List<NetworkMessageHandlerAttribute>)get_clientMessageHandlers.GetValue(null);


        HashSet<short> hashSet = new HashSet<short>();
        Type[] types = typeof(NetworkHandler).Assembly.GetTypes();
        for (int i = 0; i < types.Length; i++)
        {
            foreach (MethodInfo methodInfo in types[i].GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object[] customAttributes = methodInfo.GetCustomAttributes(false);
                foreach (var attribute in customAttributes.OfType<NetworkMessageHandlerAttribute>())
                {
                    NetworkMessageHandlerAttribute networkMessageHandlerAttribute = attribute as NetworkMessageHandlerAttribute;

                    NetworkMessageDelegate messageHandler = null;
                    if (networkMessageHandlerAttribute != null)
                    {
                        messageHandler = (NetworkMessageDelegate)Delegate.CreateDelegate(typeof(NetworkMessageDelegate), methodInfo);
                        if (messageHandler != null)
                        {
                            if (networkMessageHandlerAttribute.client)
                            {
                                clientMessageHandlers.Add(networkMessageHandlerAttribute);
                                hashSet.Add(networkMessageHandlerAttribute.msgType);
                            }
                            if (networkMessageHandlerAttribute.server)
                            {
                                serverMessageHandlers.Add(networkMessageHandlerAttribute);
                                hashSet.Add(networkMessageHandlerAttribute.msgType);
                            }
                        }
                        if (messageHandler == null)
                        {
                            Debug.LogWarningFormat("Could not register message handler for {0}. The function signature is likely incorrect.", new object[]
                            {
                                methodInfo.Name
                            });
                        }
                        get_messageHandler.SetValue(networkMessageHandlerAttribute, messageHandler);
                        if (!networkMessageHandlerAttribute.client && !networkMessageHandlerAttribute.server)
                        {
                            Debug.LogWarningFormat("Could not register message handler for {0}. It is marked as neither server nor client.", new object[]
                            {
                                methodInfo.Name
                            });
                        }
                    }
                }
            }
        }
    }
}