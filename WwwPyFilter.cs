///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// WwwPyFilter
//
// Copyright ©2008 Liam Kirton <liam@int3.ws>
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// WwwPyFilter.cs
//
// Created: 09/01/2008
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using IronPython.Runtime;
using IronPython.Runtime.Types;
using IronPython.Runtime.Operations;
using IronPython.Hosting;
using IronPython.Modules;

using WwwProxy;

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace WwwPyFilter
{
    class WwwPyFilter
    {
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static ManualResetEvent exitEvent_ = null;
        static WwwProxy.WwwProxy wwwProxy_ = null;

        static Mutex pythonMutex_ = null;
        static PythonEngine pythonEngine_ = null;
        static EngineModule wwwPyFilterMod_ = null;
        static object wwwPyFilterClassObject_ = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            exitEvent_.Set();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("WwwPyFilter {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.WriteLine("Copyright ©2008 Liam Kirton <liam@int3.ws>");
            Console.WriteLine();

            pythonMutex_ = new Mutex();

            StartPython();
            
            try
            {
                pythonEngine_.ExecuteFile("WwwPyFilter.py", wwwPyFilterMod_);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                Console.WriteLine();
                Console.WriteLine(e.StackTrace);

                pythonEngine_.Shutdown();
                return;
            }

            UserType wwwPyFilterType = wwwPyFilterMod_.Globals["WwwPyFilter"] as UserType;
            wwwPyFilterClassObject_ = Ops.Call(wwwPyFilterType);

            wwwProxy_ = WwwProxy.WwwProxy.GetInstance();
            wwwProxy_.Connect += new WwwProxy.WwwProxyConnectEvent(WwwProxy_Connect);
            wwwProxy_.Disconnect += new WwwProxy.WwwProxyConnectEvent(WwwProxy_Disconnect);
            wwwProxy_.Error += new WwwProxy.WwwProxyErrorEvent(WwwProxy_Error);
            wwwProxy_.Request += new WwwProxy.WwwProxyRequestEvent(WwwProxy_Request);
            wwwProxy_.Response += new WwwProxy.WwwProxyResponseEvent(WwwProxy_Response);

            ushort localPort = 8080;
            string[] remoteProxy = null;

            for(int i = 0; i < args.Length; ++i)
            {
                switch(i)
                {
                    case 0:
                        localPort = Convert.ToUInt16(args[i]);
                        break;
                    case 1:
                        remoteProxy = args[i].Split(':');
                        break;
                }
            }

            wwwProxy_.Start(localPort, (remoteProxy != null) ? new IPEndPoint(IPAddress.Parse(remoteProxy[0]), Convert.ToUInt16(remoteProxy[1])) : null);

            Console.WriteLine("Running. Press Ctrl+C to Quit.");
            Console.WriteLine();

            exitEvent_ = new ManualResetEvent(false);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            exitEvent_.WaitOne();

            Console.WriteLine("Closing.");

            wwwProxy_.Stop();
            StopPython();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static void StartPython()
        {
            try
            {
                pythonMutex_.WaitOne();

                pythonEngine_ = new PythonEngine();

                string dll = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName + "\\WwwProxy.dll";
                Assembly wwwProxyAssembly = Assembly.LoadFile(dll);
                pythonEngine_.LoadAssembly(wwwProxyAssembly);

                wwwPyFilterMod_ = pythonEngine_.CreateModule("wwwpyfilter", true);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                Console.WriteLine();
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                pythonMutex_.ReleaseMutex();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static void StopPython()
        {
            try
            {
                pythonMutex_.WaitOne();

                wwwPyFilterMod_ = null;

                pythonEngine_.Shutdown();
                pythonEngine_ = null;
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                Console.WriteLine();
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                pythonMutex_.ReleaseMutex();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static void WwwProxy_Connect(IPEndPoint ipEndPoint)
        {
            
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static void WwwProxy_Disconnect(IPEndPoint ipEndPoint)
        {
            
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static void WwwProxy_Error(IPEndPoint ipEndPoint, WwwProxy.ProxyRequest request, Exception exception, string error)
        {
            Console.WriteLine("WwwProxy_Error(): {0}", error);

            if (exception != null)
            {
                if (exception is SocketException)
                {
                    if (((SocketException)exception).ErrorCode == 10048)
                    {
                        exitEvent_.Set();
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static void WwwProxy_Request(IPEndPoint ipEndPoint, WwwProxy.ProxyRequest request)
        {
            try
            {
                pythonMutex_.WaitOne();

                object[] args = new object[1];
                args[0] = request;

                object rtnValue = Ops.Invoke(wwwPyFilterClassObject_, SymbolTable.StringToId("request_filter"), args);
                if(rtnValue is bool)
                {
                    if((bool)rtnValue)
                    {
                        wwwProxy_.Pass(request);
                    }
                    else
                    {
                        wwwProxy_.Drop(request);
                    }
                }
                else
                {
                    Console.WriteLine("WwwProxy_Response Warning: request_filter returned non-bool");
                    wwwProxy_.Pass(request);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                Console.WriteLine();
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                pythonMutex_.ReleaseMutex();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static void WwwProxy_Response(IPEndPoint ipEndPoint, WwwProxy.ProxyRequest request, WwwProxy.ProxyResponse response)
        {
            try
            {
                pythonMutex_.WaitOne();

                object[] args = new object[2];
                args[0] = request;
                args[1] = response;

                object rtnValue = Ops.Invoke(wwwPyFilterClassObject_, SymbolTable.StringToId("response_filter"), args);
                if(rtnValue is bool)
                {
                    if(!response.Completable || (bool)rtnValue)
                    {
                        wwwProxy_.Pass(response);
                    }
                    else
                    {
                        wwwProxy_.Drop(response);
                    }
                }
                else
                {
                    Console.WriteLine("WwwProxy_Response Warning: response_filter returned non-bool");
                    wwwProxy_.Pass(response);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                Console.WriteLine();
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                pythonMutex_.ReleaseMutex();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
