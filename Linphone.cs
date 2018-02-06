using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;

namespace sipdotnet
{
	public class Linphone
	{

        #if (WINDOWS)
                const string LIBNAME = "linphone.dll";
        #else
		        const string LIBNAME = "liblinphone";
        #endif

        #region Import

        /// <summary>
        /// Disable a sip transport
        /// </summary>
        const int LC_SIP_TRANSPORT_DISABLED = 0;

        /// <summary>
        /// Randomly chose a sip port for this transport
        /// </summary>
        const int LC_SIP_TRANSPORT_RANDOM = -1;

        /// <summary>
        /// Don't create any server socket for this transport, ie don't bind on any port
        /// </summary>
        const int LC_SIP_TRANSPORT_DONTBIND = -2;

        /// <summary>
        /// Linphone core SIP transport ports
        /// http://www.linphone.org/docs/liblinphone/struct__LinphoneSipTransports.html
        /// </summary>
		struct LCSipTransports
		{
            /// <summary>
            /// UDP port to listening on, negative value if not set
            /// </summary>
			public int udp_port;

            /// <summary>
            /// TCP port to listening on, negative value if not set
            /// </summary>
			public int tcp_port;

            /// <summary>
            /// DTLS port to listening on, negative value if not set
            /// </summary>
			public int dtls_port;

            /// <summary>
            /// TLS port to listening on, negative value if not set
            /// </summary>
			public int tls_port;
		};

        /// <summary>
        /// Describes proxy registration states
        /// http://www.linphone.org/docs/liblinphone/group__proxies.html
        /// </summary>
		public enum LinphoneRegistrationState
		{
            /// <summary>
            /// Initial state for registrations
            /// </summary>
			LinphoneRegistrationNone,

            /// <summary>
            /// Registration is in progress
            /// </summary>
			LinphoneRegistrationProgress,

            /// <summary>
            /// Registration is successful
            /// </summary>
			LinphoneRegistrationOk,

            /// <summary>
            /// Unregistration succeeded
            /// </summary>
			LinphoneRegistrationCleared,

            /// <summary>
            /// Registration failed
            /// </summary>
			LinphoneRegistrationFailed
        };

        /// <summary>
        /// Logging level
        /// https://github.com/BelledonneCommunications/ortp/blob/master/include/ortp/logging.h
        /// https://github.com/BelledonneCommunications/bctoolbox/blob/master/include/bctoolbox/logging.h
        /// </summary>
        public enum OrtpLogLevel
        {
            DEBUG = 1,
            TRACE = 1 << 1,
            MESSAGE = 1 << 2,
            WARNING = 1 << 3,
            ERROR = 1 << 4,
            FATAL = 1 << 5,
            END = 1 << 6
        };
        
        /// <summary>
        /// Represents the different state a call can reach into
        /// http://www.linphone.org/docs/liblinphone/group__call__control.html
        /// </summary>
		public enum LinphoneCallState
		{
            /// <summary>
            /// Initial call state
            /// </summary>
			LinphoneCallIdle,

            /// <summary>
            /// This is a new incoming call
            /// </summary>
			LinphoneCallIncomingReceived,

            /// <summary>
            /// An outgoing call is started
            /// </summary>
			LinphoneCallOutgoingInit,

            /// <summary>
            /// An outgoing call is in progress
            /// </summary>
			LinphoneCallOutgoingProgress,

            /// <summary>
            /// An outgoing call is ringing at remote end
            /// </summary>
			LinphoneCallOutgoingRinging,

            /// <summary>
            /// An outgoing call is proposed early media
            /// </summary>
			LinphoneCallOutgoingEarlyMedia,

            /// <summary>
            /// Connected, the call is answered
            /// </summary>
			LinphoneCallConnected,

            /// <summary>
            /// The media streams are established and running
            /// </summary>
			LinphoneCallStreamsRunning,

            /// <summary>
            /// The call is pausing at the initiative of local end
            /// </summary>
			LinphoneCallPausing,

            /// <summary>
            /// The call is paused, remote end has accepted the pause
            /// </summary>
			LinphoneCallPaused,

            /// <summary>
            /// The call is being resumed by local end
            /// </summary>
			LinphoneCallResuming,

            /// <summary>
            /// <The call is being transfered to another party, resulting in a new outgoing call to follow immediately
            /// </summary>
			LinphoneCallRefered,

            /// <summary>
            /// The call encountered an error
            /// </summary>
			LinphoneCallError,

            /// <summary>
            /// The call ended normally
            /// </summary>
			LinphoneCallEnd,

            /// <summary>
            /// The call is paused by remote end
            /// </summary>
			LinphoneCallPausedByRemote,

            /// <summary>
            /// The call's parameters change is requested by remote end, used for example when video is added by remote
            /// </summary>
			LinphoneCallUpdatedByRemote,

            /// <summary>
            /// We are proposing early media to an incoming call
            /// </summary>
			LinphoneCallIncomingEarlyMedia,

            /// <summary>
            /// A call update has been initiated by us
            /// </summary>
			LinphoneCallUpdating,

            /// <summary>
            /// The call object is no more retained by the core
            /// </summary>
            LinphoneCallReleased
        };

        /// <summary>
        /// Policy to use to pass through NATs/firewalls.
        /// https://github.com/BelledonneCommunications/linphone/blob/master/coreapi/private.h
        /// </summary>
        struct LinphoneNatPolicy
        {
            public IntPtr baseObject;
            public IntPtr user_data;
            public IntPtr lc;
            public IntPtr stun_resolver_context;
            public IntPtr stun_addrinfo;
            public IntPtr stun_server;
            public IntPtr stun_server_username;
            public IntPtr refObject;
            public IntPtr stun_enabled;
            public IntPtr turn_enabled;
            public IntPtr ice_enabled;
            public IntPtr upnp_enabled;
        };

        /// <summary>
        /// Holds all callbacks that the application should implement. None is mandatory.
        /// http://www.linphone.org/docs/liblinphone/struct__LinphoneCoreVTable.html
        /// </summary>
        struct LinphoneCoreVTable
		{
            /// <summary>
            /// Notifies global state changes
            /// </summary>
			public IntPtr global_state_changed;

            /// <summary>
            /// Notifies registration state changes
            /// </summary>
			public IntPtr registration_state_changed;

            /// <summary>
            /// Notifies call state changes
            /// </summary>
			public IntPtr call_state_changed;

            /// <summary>
            /// Notify received presence events
            /// </summary>
			public IntPtr notify_presence_received;

            /// <summary>
            /// Notify received presence events
            /// </summary>
            public IntPtr notify_presence_received_for_uri_or_tel;

            /// <summary>
            /// Notify about pending presence subscription request
            /// </summary>
            public IntPtr new_subscription_requested;

            /// <summary>
            /// Ask the application some authentication information
            /// </summary>
			public IntPtr auth_info_requested;

            /// <summary>
            /// Ask the application some authentication information
            /// </summary>
            public IntPtr authentication_requested;

            /// <summary>
            /// Notifies that call log list has been updated
            /// </summary>
            public IntPtr call_log_updated;

            /// <summary>
            /// A message is received, can be text or external body
            /// </summary>
			public IntPtr message_received;

            /// <summary>
            /// An encrypted message is received but we can't decrypt it
            /// </summary>
            public IntPtr message_received_unable_decrypt;

            /// <summary>
            /// An is-composing notification has been received
            /// </summary>
            public IntPtr is_composing_received;

            /// <summary>
            /// A dtmf has been received received
            /// </summary>
			public IntPtr dtmf_received;

            /// <summary>
            /// An out of call refer was received
            /// </summary>
			public IntPtr refer_received;

            /// <summary>
            /// Notifies on change in the encryption of call streams
            /// </summary>
			public IntPtr call_encryption_changed;

            /// <summary>
            /// Notifies when a transfer is in progress
            /// </summary>
			public IntPtr transfer_state_changed;

            /// <summary>
            /// A LinphoneFriend's BuddyInfo has changed
            /// </summary>
			public IntPtr buddy_info_updated;

            /// <summary>
            /// Notifies on refreshing of call's statistics.
            /// </summary>
			public IntPtr call_stats_updated;

            /// <summary>
            /// Notifies an incoming informational message received.
            /// </summary>
			public IntPtr info_received;

            /// <summary>
            /// Notifies subscription state change
            /// </summary>
			public IntPtr subscription_state_changed;

            /// <summary>
            /// Notifies a an event notification, see linphone_core_subscribe()
            /// </summary>
			public IntPtr notify_received;

            /// <summary>
            /// Notifies publish state change (only from #LinphoneEvent api)
            /// </summary>
			public IntPtr publish_state_changed;

            /// <summary>
            /// Notifies configuring status changes
            /// </summary>
			public IntPtr configuring_status;

            /// <summary>
            /// Callback that notifies various events with human readable text (deprecated)
            /// </summary>
            [System.Obsolete]
            public IntPtr display_status;

            /// <summary>
            /// Callback to display a message to the user (deprecated)
            /// </summary>
            [System.Obsolete]
			public IntPtr display_message;

            /// <summary>
            /// Callback to display a warning to the user (deprecated)
            /// </summary>
            [System.Obsolete]
            public IntPtr display_warning;

            [System.Obsolete]
            public IntPtr display_url;

            /// <summary>
            /// Notifies the application that it should show up
            /// </summary>
            [System.Obsolete]
            public IntPtr show;

            /// <summary>
            /// Use #message_received instead <br> A text message has been received
            /// </summary>
            [System.Obsolete]
            public IntPtr text_received;

            /// <summary>
            /// Callback to store file received attached to a LinphoneChatMessage
            /// </summary>
            [System.Obsolete]
            public IntPtr file_transfer_recv;

            /// <summary>
            /// Callback to collect file chunk to be sent for a LinphoneChatMessage
            /// </summary>
            [System.Obsolete]
            public IntPtr file_transfer_send;

            /// <summary>
            /// Callback to indicate file transfer progress
            /// </summary>
            [System.Obsolete]
            public IntPtr file_transfer_progress_indication;

            /// <summary>
            /// Callback to report IP network status (I.E up/down)
            /// </summary>
            public IntPtr network_reachable;

            /// <summary>
            /// Callback to upload collected logs
            /// </summary>
            public IntPtr log_collection_upload_state_changed;

            /// <summary>
            /// Callback to indicate log collection upload progress
            /// </summary>
            public IntPtr log_collection_upload_progress_indication;

            public IntPtr friend_list_created;

            public IntPtr friend_list_removed;

            /// <summary>
            /// User data associated with the above callbacks
            /// </summary>
            public IntPtr user_data;
        };

        #region Initializing

        // http://www.linphone.org/docs/liblinphone/group__initializing.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        [System.Obsolete]
		static extern void linphone_core_enable_logs (IntPtr FILE);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        [System.Obsolete]
        static extern void linphone_core_disable_logs ();

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        [System.Obsolete]
        static extern IntPtr linphone_core_new (IntPtr vtable, string config_path, string factory_config_path, IntPtr userdata);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_core_unref (IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_core_iterate (IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_core_set_log_level (OrtpLogLevel loglevel);

        #endregion

        #region Proxies

        // http://www.linphone.org/docs/liblinphone/group__proxies.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_core_create_proxy_config (IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int linphone_proxy_config_set_identity (IntPtr obj, string identity);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int linphone_proxy_config_set_server_addr (IntPtr obj, string server_addr);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_proxy_config_enable_register (IntPtr obj, bool val);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int linphone_core_add_proxy_config (IntPtr lc, IntPtr cfg);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_core_set_default_proxy_config (IntPtr lc, IntPtr config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        [System.Obsolete]
        static extern int linphone_core_get_default_proxy (IntPtr lc, ref IntPtr config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern bool linphone_proxy_config_is_registered (IntPtr config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_proxy_config_edit (IntPtr config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int linphone_proxy_config_done (IntPtr config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_proxy_config_set_nat_policy (IntPtr cfg, IntPtr policy);

        #endregion

        #region Network

        // http://www.linphone.org/docs/liblinphone/group__network__parameters.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int linphone_core_set_sip_transports (IntPtr lc, IntPtr tr_config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_core_create_nat_policy (IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_nat_policy_unref (IntPtr natpolicy);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_nat_policy_ref (IntPtr natpolicy);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_nat_policy_clear(IntPtr policy);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_nat_policy_enable_stun(IntPtr policy, bool enable);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_nat_policy_enable_turn(IntPtr policy, bool enable);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_nat_policy_enable_ice(IntPtr policy, bool enable);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_nat_policy_enable_upnp(IntPtr policy, bool enable);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_nat_policy_set_stun_server(IntPtr policy, string stun_server);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_nat_policy_set_stun_server_username(IntPtr policy, string username);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_nat_policy_resolve_stun_server(IntPtr policy);

        #endregion

        #region SIP

        // http://www.linphone.org/docs/liblinphone/group__linphone__address.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        [System.Obsolete]
        static extern void linphone_address_destroy (IntPtr u);

        #endregion

        #region Miscenalleous

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_core_set_user_agent (IntPtr lc, string ua_name, string version);

        #endregion

        #region Calls

        // http://www.linphone.org/docs/liblinphone/group__call__control.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_core_create_call_params (IntPtr lc, IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_call_params_enable_video (IntPtr lc, bool enabled);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_call_params_enable_early_media_sending (IntPtr lc, bool enabled);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_core_invite_with_params (IntPtr lc, string url, IntPtr callparams);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_call_get_params (IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_call_params_ref(IntPtr callparams);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_call_params_unref (IntPtr callparams);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int linphone_core_terminate_call (IntPtr lc, IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_call_ref(IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_call_unref (IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int linphone_core_terminate_all_calls (IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_call_get_remote_address_as_string (IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int linphone_core_accept_call_with_params (IntPtr lc, IntPtr call, IntPtr callparams);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_call_params_set_record_file (IntPtr callparams, string filename);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr linphone_call_params_get_record_file (IntPtr callparams);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_call_params_enable_audio (IntPtr callparams, bool enabled);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int linphone_call_send_dtmfs (IntPtr call, string dtmfs);

        #endregion

        #region Authentication

        // http://www.linphone.org/docs/liblinphone/group__authentication.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_core_add_auth_info (IntPtr lc, IntPtr info);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr linphone_auth_info_new (string username, string userid, string passwd, string ha1, string realm, string domain);

        #endregion

        #region Calls miscenalleous

        // http://www.linphone.org/docs/liblinphone/group__call__misc.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_call_start_recording (IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_call_stop_recording (IntPtr call);

        #endregion

        #region Media

        // http://www.linphone.org/docs/liblinphone/group__media__parameters.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_play_file (IntPtr lc, string file);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		static extern void linphone_core_set_record_file (IntPtr lc, string file);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_core_set_ring (IntPtr lc, string file);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_core_set_remote_ringback_tone (IntPtr lc, string file);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void linphone_core_set_ringback (IntPtr lc, string file);

        #endregion

        #endregion

        class LinphoneCall : Call
		{
			IntPtr linphoneCallPtr;

			public IntPtr LinphoneCallPtr {
				get {
                    return linphoneCallPtr;
				}
				set {
					linphoneCallPtr = value;
				}
			}

			public void SetCallType (CallType type)
			{
				this.calltype = type;
			}

			public void SetCallState (CallState state)
			{
				this.callstate = state;
			}

			public void SetFrom (string from)
			{
				this.from = from;
			}

			public void SetTo (string to)
			{
				this.to = to;
			}

            public void SetRecordFile(string recordfile)
            {
                this.recordfile = recordfile;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void LinphoneCoreRegistrationStateChangedCb (IntPtr lc, IntPtr cfg, LinphoneRegistrationState cstate, string message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void LinphoneCoreCallStateChangedCb (IntPtr lc, IntPtr call, LinphoneCallState cstate, string message);

        LinphoneCoreRegistrationStateChangedCb registration_state_changed;
		LinphoneCoreCallStateChangedCb call_state_changed;
		IntPtr linphoneCore, proxy_cfg, auth_info, t_configPtr, vtablePtr, natPolicy;
		Thread coreLoop;
		bool running = true;
		string identity, server_addr;
		LinphoneCoreVTable vtable;
        LCSipTransports t_config;

		List<LinphoneCall> calls = new List<LinphoneCall> ();

        public delegate void RegistrationStateChangedDelegate(LinphoneRegistrationState state);
        public event RegistrationStateChangedDelegate RegistrationStateChangedEvent;

        public delegate void CallStateChangedDelegate(Call call);
        public event CallStateChangedDelegate CallStateChangedEvent;

        public delegate void ErrorDelegate(Call call, string message);
        public event ErrorDelegate ErrorEvent;

        private bool logsEnabled = false;
        public bool LogsEnabled { get => logsEnabled; set => logsEnabled = value; }
        public delegate void LogDelegate(string message);
        private event LogDelegate logEventHandler;
        public event LogDelegate LogEvent
        {
            add
            {
                if (logEventHandler == null && LogsEnabled)
                {
                    linphone_core_enable_logs(IntPtr.Zero);
                    linphone_core_set_log_level(OrtpLogLevel.DEBUG);
                }
                logEventHandler += value;
            }

            remove
            {
                logEventHandler -= value;
                if (logEventHandler == null)
                {
                    linphone_core_disable_logs();
                    linphone_core_set_log_level(OrtpLogLevel.END);
                }
            }
        }


        LinphoneCall FindCall (IntPtr call)
		{
			return calls.Find (delegate(LinphoneCall obj) {
				return (obj.LinphoneCallPtr == call);
			});
		}

		void SetTimeout (Action callback, int miliseconds)
		{
			System.Timers.Timer timeout = new System.Timers.Timer ();
			timeout.Interval = miliseconds;
			timeout.AutoReset = false;
			timeout.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) => {
				callback ();
			};
			timeout.Start ();
		}

        IntPtr createDefaultCallParams()
        {
            if (linphoneCore == IntPtr.Zero || !running)
                throw new InvalidOperationException("linphoneCore not started");

            IntPtr callParams = linphone_core_create_call_params(linphoneCore, IntPtr.Zero);
            callParams = linphone_call_params_ref(callParams);
            linphone_call_params_enable_video(callParams, false);
            linphone_call_params_enable_audio(callParams, true);
            linphone_call_params_enable_early_media_sending(callParams, true);

            return callParams;
        }

        Linphone()
        {
            linphone_core_disable_logs();
            linphone_core_set_log_level(OrtpLogLevel.END);
        }


        
        public void CreatePhone (string username, string password, string server, int port, string agent, string version,
            bool use_stun, bool use_turn, bool use_ice, bool use_upnp, string stun_server)
		{
            running = true;

            registration_state_changed = new LinphoneCoreRegistrationStateChangedCb(OnRegistrationChanged);
            call_state_changed = new LinphoneCoreCallStateChangedCb(OnCallStateChanged);

#pragma warning disable 0612
            vtable = new LinphoneCoreVTable()
            {
                global_state_changed = IntPtr.Zero,
                registration_state_changed = Marshal.GetFunctionPointerForDelegate(registration_state_changed),
                call_state_changed = Marshal.GetFunctionPointerForDelegate(call_state_changed),
                notify_presence_received = IntPtr.Zero,
                notify_presence_received_for_uri_or_tel = IntPtr.Zero,
                new_subscription_requested = IntPtr.Zero,
                auth_info_requested = IntPtr.Zero,
                authentication_requested = IntPtr.Zero,
                call_log_updated = IntPtr.Zero,
                message_received = IntPtr.Zero,
                message_received_unable_decrypt = IntPtr.Zero,
                is_composing_received = IntPtr.Zero,
                dtmf_received = IntPtr.Zero,
                refer_received = IntPtr.Zero,
                call_encryption_changed = IntPtr.Zero,
                transfer_state_changed = IntPtr.Zero,
                buddy_info_updated = IntPtr.Zero,
                call_stats_updated = IntPtr.Zero,
                info_received = IntPtr.Zero,
                subscription_state_changed = IntPtr.Zero,
                notify_received = IntPtr.Zero,
                publish_state_changed = IntPtr.Zero,
                configuring_status = IntPtr.Zero,
                display_status = IntPtr.Zero,
                display_message = IntPtr.Zero,
                display_warning = IntPtr.Zero,
                display_url = IntPtr.Zero,
                show = IntPtr.Zero,
                text_received = IntPtr.Zero,
                file_transfer_recv = IntPtr.Zero,
                file_transfer_send = IntPtr.Zero,
                file_transfer_progress_indication = IntPtr.Zero,
                network_reachable = IntPtr.Zero,
                log_collection_upload_state_changed = IntPtr.Zero,
                log_collection_upload_progress_indication = IntPtr.Zero,
                friend_list_created = IntPtr.Zero,
                friend_list_removed = IntPtr.Zero,
                user_data = IntPtr.Zero
            };
#pragma warning restore 0612

            vtablePtr = Marshal.AllocHGlobal(Marshal.SizeOf(vtable));
            Marshal.StructureToPtr(vtable, vtablePtr, false); 

            linphoneCore = linphone_core_new(vtablePtr, null, null, IntPtr.Zero);
            
            coreLoop = new Thread(LinphoneMainLoop);
            coreLoop.IsBackground = false;
            coreLoop.Start();

            t_config = new LCSipTransports()
			{
				udp_port = LC_SIP_TRANSPORT_RANDOM,
				tcp_port = LC_SIP_TRANSPORT_RANDOM,
				dtls_port = LC_SIP_TRANSPORT_RANDOM,
				tls_port = LC_SIP_TRANSPORT_RANDOM
			};
			t_configPtr = Marshal.AllocHGlobal(Marshal.SizeOf(t_config));
			Marshal.StructureToPtr (t_config, t_configPtr, false);
			linphone_core_set_sip_transports (linphoneCore, t_configPtr);

            linphone_core_set_user_agent (linphoneCore, agent, version);

            identity = "sip:" + username + "@" + server;
			server_addr = "sip:" + server + ":" + port.ToString();

			auth_info = linphone_auth_info_new (username, null, password, null, null, null);
			linphone_core_add_auth_info (linphoneCore, auth_info);

            natPolicy = linphone_core_create_nat_policy (linphoneCore);
            natPolicy = linphone_nat_policy_ref (natPolicy);
            linphone_nat_policy_enable_stun (natPolicy, use_stun);
            linphone_nat_policy_enable_turn (natPolicy, use_turn);
            linphone_nat_policy_enable_ice (natPolicy, use_ice);
            linphone_nat_policy_enable_upnp (natPolicy, use_upnp);
            if (!string.IsNullOrEmpty(stun_server))
            {
                linphone_nat_policy_set_stun_server (natPolicy, stun_server);
                linphone_nat_policy_resolve_stun_server (natPolicy);
            }

            proxy_cfg = linphone_core_create_proxy_config (linphoneCore);
			linphone_proxy_config_set_identity (proxy_cfg, identity);
			linphone_proxy_config_set_server_addr (proxy_cfg, server_addr);
			linphone_proxy_config_enable_register (proxy_cfg, true);

            linphone_proxy_config_set_nat_policy (proxy_cfg, natPolicy);

            linphone_core_add_proxy_config (linphoneCore, proxy_cfg);
            linphone_core_set_default_proxy_config (linphoneCore, proxy_cfg);
        }
       
        public void DestroyPhone ()
		{
            RegistrationStateChangedEvent?.Invoke(LinphoneRegistrationState.LinphoneRegistrationProgress); // disconnecting

            linphone_core_terminate_all_calls (linphoneCore);

			SetTimeout (delegate {

				if (linphone_proxy_config_is_registered (proxy_cfg)) {
					linphone_proxy_config_edit (proxy_cfg);
					linphone_proxy_config_enable_register (proxy_cfg, false);
					linphone_proxy_config_done (proxy_cfg);
				}

				SetTimeout (delegate {
					running = false;
				}, 5000);

			}, 2000);
		}

        void LinphoneMainLoop()
        {
            while (running)
            {
                linphone_core_iterate(linphoneCore); // roll
                System.Threading.Thread.Sleep(100);
            }

            linphone_nat_policy_unref (natPolicy);
            linphone_core_unref(linphoneCore);

            if (vtablePtr != IntPtr.Zero)
                Marshal.FreeHGlobal(vtablePtr);
            if (t_configPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(t_configPtr);
            registration_state_changed = null;
            call_state_changed = null;
            linphoneCore = proxy_cfg = auth_info = t_configPtr = IntPtr.Zero;
            coreLoop = null;
            identity = null;
            server_addr = null;

            RegistrationStateChangedEvent?.Invoke(LinphoneRegistrationState.LinphoneRegistrationCleared);

        }
        
        public void SendDTMFs (Call call, string dtmfs)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                return;
            }

            LinphoneCall linphonecall = (LinphoneCall)call;
            linphone_call_send_dtmfs (linphonecall.LinphoneCallPtr, dtmfs);
        }

        public void SetRingbackSound (string file)
        {
            if (linphoneCore == IntPtr.Zero || !running)
            {
                ErrorEvent?.Invoke(null, "Cannot modify configuration when Linphone Core is not working.");
                return;
            }

            linphone_core_set_ringback (linphoneCore, file);
        }

        public void SetIncomingRingSound (string file)
        {
            if (linphoneCore == IntPtr.Zero || !running)
            {
                ErrorEvent?.Invoke(null, "Cannot modify configuration when Linphone Core is not working.");
                return;
            }

            linphone_core_set_ring (linphoneCore, file);
        }
        
        public void TerminateCall (Call call)
		{
			if (call == null)
				throw new ArgumentNullException ("call");

			if (linphoneCore == IntPtr.Zero || !running) {
                ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                return;
			}

			LinphoneCall linphonecall = (LinphoneCall) call;
			linphone_core_terminate_call (linphoneCore, linphonecall.LinphoneCallPtr);
        }

		public void MakeCall (string uri)
		{
            MakeCallAndRecord(uri, null, false);
        }

		public void MakeCallAndRecord (string uri, string filename, bool startRecordInstantly = true)
		{
			if (linphoneCore == IntPtr.Zero || !running) {
                ErrorEvent?.Invoke(null, "Cannot make or receive calls when Linphone Core is not working.");
                return;
			}

            IntPtr callParams = createDefaultCallParams();

            if (!string.IsNullOrEmpty(filename)) 
                linphone_call_params_set_record_file (callParams, filename);

            IntPtr call = linphone_core_invite_with_params (linphoneCore, uri, callParams);
			if (call == IntPtr.Zero) {
                ErrorEvent?.Invoke(null, "Cannot call.");
                return;
			}

            linphone_call_ref(call);
            if (startRecordInstantly)
            {
                linphone_call_start_recording(call);
            }
		}

        public void StartRecording (Call call)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                return;
            }

            LinphoneCall linphonecall = (LinphoneCall)call;
            if (!string.IsNullOrEmpty(linphonecall.GetRecordfile()))
                linphone_call_start_recording (linphonecall.LinphoneCallPtr);
        }

        public void PauseRecording (Call call)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                return;
            }

            LinphoneCall linphonecall = (LinphoneCall)call;
            if (!string.IsNullOrEmpty(linphonecall.GetRecordfile()))
                linphone_call_stop_recording (linphonecall.LinphoneCallPtr);
        }

		public void ReceiveCallAndRecord (Call call, string filename, bool startRecordInstantly = true)
		{
			if (call == null)
				throw new ArgumentNullException ("call");

			if (linphoneCore == IntPtr.Zero || !running) {
                ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                return;
			}

			LinphoneCall linphonecall = (LinphoneCall) call;
            linphone_call_ref(linphonecall.LinphoneCallPtr);

            IntPtr callParams = createDefaultCallParams();

            if (!string.IsNullOrEmpty(filename))
                linphone_call_params_set_record_file (callParams, filename);

            linphone_core_accept_call_with_params (linphoneCore, linphonecall.LinphoneCallPtr, callParams);
            if (startRecordInstantly)
            {
                linphone_call_start_recording(linphonecall.LinphoneCallPtr);
            }
		}

		public void ReceiveCall (Call call)
		{
            ReceiveCallAndRecord(call, null, false);
        }

		void OnRegistrationChanged (IntPtr lc, IntPtr cfg, LinphoneRegistrationState cstate, string message) 
		{
			if (linphoneCore == IntPtr.Zero || !running) return;
            
            logEventHandler?.Invoke("OnRegistrationChanged: " + cstate);

            RegistrationStateChangedEvent?.Invoke(cstate);
        }

		void OnCallStateChanged (IntPtr lc, IntPtr call, LinphoneCallState cstate, string message)
		{
			if (linphoneCore == IntPtr.Zero || !running) return;
            logEventHandler?.Invoke("OnCallStateChanged: " + cstate);

            Call.CallState newstate = Call.CallState.None;
			Call.CallType newtype = Call.CallType.None;
			string from = "";
			string to = "";
            string recordfile = "";
            IntPtr recordfileStringPtr, addressStringPtr;

            IntPtr callParams = linphone_call_get_params (call);
            recordfileStringPtr = linphone_call_params_get_record_file (callParams);
            if (recordfileStringPtr != IntPtr.Zero)
                recordfile = Marshal.PtrToStringAnsi(recordfileStringPtr);

            // detecting direction, state and source-destination data by state
            switch (cstate) {
				case LinphoneCallState.LinphoneCallIncomingReceived:
				case LinphoneCallState.LinphoneCallIncomingEarlyMedia:
					newstate = Call.CallState.Loading;
					newtype = Call.CallType.Incoming;
                    addressStringPtr = linphone_call_get_remote_address_as_string(call);
                    if (addressStringPtr != IntPtr.Zero) from = Marshal.PtrToStringAnsi(addressStringPtr);
					to = identity;
					break;

				case LinphoneCallState.LinphoneCallConnected:
				case LinphoneCallState.LinphoneCallStreamsRunning:
				case LinphoneCallState.LinphoneCallPausedByRemote:
				case LinphoneCallState.LinphoneCallUpdatedByRemote:
					newstate = Call.CallState.Active;
					break;

				case LinphoneCallState.LinphoneCallOutgoingInit:
				case LinphoneCallState.LinphoneCallOutgoingProgress:
				case LinphoneCallState.LinphoneCallOutgoingRinging:
				case LinphoneCallState.LinphoneCallOutgoingEarlyMedia:
					newstate = Call.CallState.Loading;
					newtype = Call.CallType.Outcoming;
                    addressStringPtr = linphone_call_get_remote_address_as_string(call);
                    if (addressStringPtr != IntPtr.Zero) to = Marshal.PtrToStringAnsi(addressStringPtr);
					from = this.identity;
					break;

                case LinphoneCallState.LinphoneCallError:
                    newstate = Call.CallState.Error;
                    break;

				case LinphoneCallState.LinphoneCallReleased:
				case LinphoneCallState.LinphoneCallEnd:
					newstate = Call.CallState.Completed;
                    if (!string.IsNullOrEmpty(recordfile))
                        linphone_call_stop_recording(call);
					break;

				default:
					break;
			}

            IntPtr callref = linphone_call_ref(call);

            LinphoneCall existCall = FindCall (callref);

			if (existCall == null) {
				existCall = new LinphoneCall ();
				existCall.SetCallState (newstate);
				existCall.SetCallType (newtype);
				existCall.SetFrom (from);
				existCall.SetTo (to);
                existCall.SetRecordFile (recordfile);
                existCall.LinphoneCallPtr = callref;

				calls.Add (existCall);

                CallStateChangedEvent?.Invoke(existCall);
            } else {
				if (existCall.GetState () != newstate) {
					existCall.SetCallState (newstate);
                    CallStateChangedEvent?.Invoke(existCall);
                }
			}

            if (cstate == LinphoneCallState.LinphoneCallReleased)
            {
                linphone_call_unref (existCall.LinphoneCallPtr);
                calls.Remove(existCall);
            }
        }

	}
}

