﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Load_Balancing
{
    class Program
    {

        // This class is used in the worker thread to pass information in
        public class threadStateInfo
        {

            public string strUrl; // the URL we will be making a request on

            public ManualResetEvent evtReset; // used to signal this worker thread is finished

            public int iReqNumber; // used to indicate which request this is

        }

        // this does the work of making the http request from a worker thread
        static void ThreadProc(Object stateInfo)
        {
            threadStateInfo tsInfo = stateInfo as threadStateInfo;
            try
            {
                HttpWebRequest aReq = WebRequest.Create(tsInfo.strUrl) as HttpWebRequest;

                aReq.Timeout = 4000;  // I want the request to only take 4 seconds, otherwise there is some problem

                Console.WriteLine("Begin Request {0}, created in : {1}", tsInfo.iReqNumber,DateTime.Now.TimeOfDay);

                HttpWebResponse aResp = aReq.GetResponse() as HttpWebResponse;

                System.Threading.Thread.Sleep(500); //simulate a half second delay for the server to process the request

                aResp.Close(); // if you do not do this close, you will timeout for sure!  The socket will not be freed.

                Console.WriteLine("End Request {0} , {1}", tsInfo.iReqNumber, DateTime.Now.TimeOfDay);
            }
            catch (WebException theEx)
            {
                Console.WriteLine("Exception for Request {0}: {1} , try to end in : {2}", tsInfo.iReqNumber, theEx.Message, DateTime.Now.TimeOfDay);
            }
            //signal the main thread this request is done
            tsInfo.evtReset.Set();
            
        }

        public static void Main(string[] args)
        {
            //if (args.Length < 1)
            //{
            //    showusage();
            //    return;
            //}

            // The number of worker threads to make simultaneous requests (do not go over 64)       
            int numberRequests = 40;

            // Used to keep the Main thread alive until all requests are done
            ManualResetEvent[] manualEvents = new ManualResetEvent[numberRequests];

            // Get the URI from the command line.
            string httpSite = "https://refservice/RefService/refservice.svc/web/Customer/1";

            // 2 is Default Connection Limit
            ServicePointManager.DefaultConnectionLimit = 5;
            ServicePointManager.MaxServicePointIdleTime = 500;

            // Get the current settings for the Thread Pool
            int minWorker, minIOC;

            ThreadPool.GetMinThreads(out minWorker, out minIOC);

            // without setting the thread pool up, there was enough of a delay to cause timeouts!
            ThreadPool.SetMinThreads(numberRequests, minIOC);

            for (int i = 0; i < numberRequests; i++)
            {
                //Create a class to pass info to the thread proc
                threadStateInfo theInfo = new threadStateInfo();

                manualEvents[i] = new ManualResetEvent(false); // thread done event

                //manualEvents[i].Reset();
                theInfo.evtReset = manualEvents[i];

                theInfo.iReqNumber = i; //just to track what request this is

                theInfo.strUrl = httpSite; // the URL to open

                ThreadPool.QueueUserWorkItem(ThreadProc, theInfo);  //Let the thread pool do the work
            }
            // Wait until the ManualResetEvent is set so that the application
            // does not exit until after the callback is called.
            // can wait on a maximum of 64 handles here.
            WaitHandle.WaitAll(manualEvents);

            Console.WriteLine("done!");
            Console.ReadLine();
        }
    }
}
