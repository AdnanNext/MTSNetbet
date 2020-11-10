using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Sportradar.MTS.SDK.API;
using Sportradar.MTS.SDK.Entities.Builders;
using Sportradar.MTS.SDK.Entities.Enums;
using Sportradar.MTS.SDK.Entities.EventArguments;
using Sportradar.MTS.SDK.Entities.Interfaces;

namespace isBetradarMTS.Utilities
{
    public class MtsSession
    {
        private readonly ILog _log;
        private IMtsSdk _mtsSdk;
        private IBuilderFactory _factory;

        private MTSTicketsHandlers _ticketHandler;
        private TicketService _ticketService;
        private Timer _timer;
        private readonly object _updateTicketsLock = new object();
        private readonly TimeSpan _updateInterval;
        private volatile bool _isUpdatingTickets = false;
        public MtsSession(ILog log)
        {
            _log = log;
            int ticketSendingInterval = 1;
            try
            {
                ticketSendingInterval = Convert.ToInt32(AppSettings.GetAppSettings("TicketSendingInterval"));
            }
            catch (Exception ex)
            {
                _log.Error("Error in reading app settings: ", ex);
            }
            _updateInterval = _updateInterval = TimeSpan.FromSeconds(ticketSendingInterval);
        }
        /// <summary>
        /// Starts the MTS Sdk Session
        /// </summary>
        public void Start()
        {
            try
            {
                _log.Info("Running the MTS SDK Non-Blocking mode.");

                _log.Info("Retrieving configuration from application configuration file.");
                var config = MtsSdk.GetConfiguration();

                _log.Info("Creating root MTS SDK instance.");
                _mtsSdk = new MtsSdk(config);

                #region Testing Section

                ////handles database related operations
                //_ticketService = new TicketService(_log);
                ////handles all MTS events
                //_factory = _mtsSdk.BuilderFactory;
                //_ticketHandler = new MTSTicketsHandlers(_mtsSdk, _factory, _log);
                //TestSendTicket();

                #endregion

                _log.Info("Attaching to events.");
                AttachToFeedEvents(_mtsSdk);

                _log.Info("Opening the sdk instance (creating and opening connection to the AMPQ broker)");
                _mtsSdk.Open();
                _factory = _mtsSdk.BuilderFactory;

                //handles database related operations
                _ticketService = new TicketService(_log);
                //handles all MTS events
                _ticketHandler = new MTSTicketsHandlers(_mtsSdk, _factory, _log);

                //starting timer to get ticket updates from database
                _timer = new Timer(TicketUpdatesTimer_Tick, null, _updateInterval, _updateInterval);

                _log.Info("MTS SDK started successfully. Hit <enter> to quit if in console mode.");
                Console.WriteLine(string.Empty);
                Console.ReadLine();
            }
            catch (Exception error)
            {
                EventLog.WriteEntry("Service ", error.ToString(), EventLogEntryType.Error);
            }
        }
        private void TicketUpdatesTimer_Tick(object state)
        {
            lock (_updateTicketsLock)
            {
                if (!_isUpdatingTickets)
                {
                    _isUpdatingTickets = true;
                    var updatedTickets = _ticketService.GetUpdatedTickets();
                    if (updatedTickets != null && updatedTickets.Count > 0)
                    {
                        //ticket sending will go here
                        //Task.Run(() =>
                        //{
                            _ticketHandler.SendNewTickets(updatedTickets);
                        //});
                    }
                    Console.WriteLine("Sending tickets....");
                    _ticketService.CheckSentTickets();
                    _isUpdatingTickets = false;
                }
            }
        }

        //private void TestSendTicket()
        //{
        //    var tkService = new TicketService(_log);
        //    var updatedTickets = tkService.GetUpdatedTickets();
        //    if (updatedTickets != null && updatedTickets.Count > 0)
        //    {
        //        //ticket sending will go here
        //        Task.Run(() =>
        //        {
        //            _ticketHandler.SendNewTickets(updatedTickets);
        //        });
        //    }
        //}
        private void AttachToFeedEvents(IMtsSdk mtsSdk)
        {
            if (mtsSdk == null)
            {
                throw new ArgumentNullException(nameof(mtsSdk));
            }
            _log.Info("Attaching to events");
            mtsSdk.SendTicketFailed += OnSendTicketFailed;
            mtsSdk.TicketResponseReceived += OnTicketResponseReceived;
            mtsSdk.UnparsableTicketResponseReceived += OnUnparsableTicketResponseReceived;
            mtsSdk.TicketResponseTimedOut += OnTicketResponseTimedOut;
        }
        private void OnTicketResponseReceived(object sender, TicketResponseReceivedEventArgs e)
        {
            _log.Info($"Received {e.Type}Response for ticket '{e.Response.TicketId}'.");

            if (e.Type == TicketResponseType.Ticket)
            {
                var task = _ticketHandler.HandleTicketResponse((ITicketResponse)e.Response);
                if (task != null)
                {
                    task.Wait();
                    task.Dispose();
                }
            }
            else if (e.Type == TicketResponseType.TicketCancel)
            {
                var task2 = _ticketHandler.HandleTicketCancelResponse((ITicketCancelResponse)e.Response);
                if (task2 != null)
                {
                    task2.Wait();
                    task2.Dispose();
                }
            }
            else if (e.Type == TicketResponseType.TicketCashout)
            {
                var task3 = _ticketHandler.HandleTicketCashoutResponse((ITicketCashoutResponse)e.Response);
                if (task3 != null)
                {
                    task3.Wait();
                    task3.Dispose();
                }
            }
        }
        private void OnUnparsableTicketResponseReceived(object sender, UnparsableMessageEventArgs e)
        {
            _log.Info($"Received unparsable ticket response: {e.Body}.");
        }
        private void OnSendTicketFailed(object sender, TicketSendFailedEventArgs e)
        {
            _log.Info($"Sending ticket '{e.TicketId}' failed.");
            var task = _ticketHandler.HandleFailureTimeout(e.TicketId);
            if (task != null)
            {
                task.Wait();
                task.Dispose();
            }            
        }
        private void OnTicketResponseTimedOut(object sender, TicketMessageEventArgs e)
        {
            _log.Info($"Sending ticket '{e.TicketId}' failed due to timeout.");
            var task = _ticketHandler.HandleFailureTimeout(e.TicketId);
            if (task != null)
            {
                task.Wait();
                task.Dispose();
            }
        }
        /// <summary>
        /// Stops the MTS Session detaches all events registered and disposes the session
        /// </summary>
        public void Stop()
        {
            _log.Info("Detaching from events");
            DetachFromFeedEvents(_mtsSdk);

            _log.Info("Closing the connection and disposing the instance");
            _mtsSdk.Close();

            _log.Info("MTS SDK stopped.");
        }
        private void DetachFromFeedEvents(IMtsSdk mtsSdk)
        {
            if (mtsSdk == null)
            {
                throw new ArgumentNullException(nameof(mtsSdk));
            }

            _log.Info("Detaching from events");
            mtsSdk.SendTicketFailed -= OnSendTicketFailed;
            mtsSdk.TicketResponseReceived -= OnTicketResponseReceived;
            mtsSdk.UnparsableTicketResponseReceived -= OnUnparsableTicketResponseReceived;
            mtsSdk.TicketResponseTimedOut -= OnTicketResponseTimedOut;
        }
    }
}
