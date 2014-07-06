using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;

namespace sipdotnet
{
	public class Linphone
	{
	#region Import

		#if (WINDOWS)
		const string LIBNAME = "liblinphone-6.dll";
		#else
		const string LIBNAME = "liblinphone";
		#endif

		// from /usr/local/include/linphone/linphonecore.h

		const int LC_SIP_TRANSPORT_RANDOM = -1;
		const int LC_SIP_TRANSPORT_DISABLED = 0;

		struct LCSipTransports
		{
			public int udp_port; // udp port to listening on, negative value if not set
			public int tcp_port; // tcp port to listening on, negative value if not set
			public int dtls_port; // dtls port to listening on, negative value if not set
			public int tls_port; // tls port to listening on, negative value if not set
		};

		public enum LinphoneRegistrationState
		{
			LinphoneRegistrationNone, // Initial state for registrations
			LinphoneRegistrationProgress, // Registration is in progress
			LinphoneRegistrationOk,	// Registration is successful
			LinphoneRegistrationCleared, // Unregistration succeeded
			LinphoneRegistrationFailed	// Registration failed
		};

		public enum LinphoneCallState
		{
			LinphoneCallIdle, // Initial call state
			LinphoneCallIncomingReceived, // This is a new incoming call
			LinphoneCallOutgoingInit, // An outgoing call is started
			LinphoneCallOutgoingProgress, // An outgoing call is in progress
			LinphoneCallOutgoingRinging, // An outgoing call is ringing at remote end
			LinphoneCallOutgoingEarlyMedia, // An outgoing call is proposed early media
			LinphoneCallConnected, // <Connected, the call is answered
			LinphoneCallStreamsRunning, // The media streams are established and running
			LinphoneCallPausing, // The call is pausing at the initiative of local end
			LinphoneCallPaused, // The call is paused, remote end has accepted the pause
			LinphoneCallResuming, // The call is being resumed by local end
			LinphoneCallRefered, // <The call is being transfered to another party, resulting in a new outgoing call to follow immediately
			LinphoneCallError, // The call encountered an error
			LinphoneCallEnd, // The call ended normally
			LinphoneCallPausedByRemote, // The call is paused by remote end
			LinphoneCallUpdatedByRemote, // The call's parameters change is requested by remote end, used for example when video is added by remote
			LinphoneCallIncomingEarlyMedia, // We are proposing early media to an incoming call
			LinphoneCallUpdating, // A call update has been initiated by us
            LinphoneCallReleased // The call object is no more retained by the core
		};

		struct LinphoneCoreVTable
		{
			public IntPtr global_state_changed; //<Notifies global state changes
			public IntPtr registration_state_changed; // Notifies registration state changes
			public IntPtr call_state_changed; // Notifies call state changes
			public IntPtr notify_presence_received; // Notify received presence events
			public IntPtr new_subscription_requested; // Notify about pending presence subscription request
			public IntPtr auth_info_requested; // Ask the application some authentication information
			public IntPtr call_log_updated; // Notifies that call log list has been updated
			public IntPtr message_received; // A message is received, can be text or external body
			public IntPtr is_composing_received; // An is-composing notification has been received
			public IntPtr dtmf_received; // A dtmf has been received received
			public IntPtr refer_received; // An out of call refer was received
			public IntPtr call_encryption_changed; // Notifies on change in the encryption of call streams
			public IntPtr transfer_state_changed; // Notifies when a transfer is in progress
			public IntPtr buddy_info_updated; // A LinphoneFriend's BuddyInfo has changed
			public IntPtr call_stats_updated; // Notifies on refreshing of call's statistics.
			public IntPtr info_received; // Notifies an incoming informational message received.
			public IntPtr subscription_state_changed; // Notifies subscription state change
			public IntPtr notify_received; // Notifies a an event notification, see linphone_core_subscribe()
			public IntPtr publish_state_changed; // Notifies publish state change (only from #LinphoneEvent api)
			public IntPtr configuring_status; // Notifies configuring status changes
			public IntPtr display_status; // @deprecated Callback that notifies various events with human readable text.
			public IntPtr display_message; // @deprecated Callback to display a message to the user
			public IntPtr display_warning; // @deprecated Callback to display a warning to the user
			public IntPtr display_url; // @deprecated
			public IntPtr show; // @deprecated Notifies the application that it should show up
			public IntPtr text_received; // @deprecated, use #message_received instead <br> A text message has been received
		};

		[DllImport(LIBNAME)]
		static extern void linphone_core_enable_logs (IntPtr FILE);

        [DllImport(LIBNAME)]
		static extern void linphone_core_disable_logs ();

        [DllImport(LIBNAME)]
        static extern IntPtr linphone_core_new(IntPtr vtable, string config_path, string factory_config_path, IntPtr userdata);

        [DllImport(LIBNAME)]
		static extern void linphone_core_destroy (IntPtr lc);

        [DllImport(LIBNAME)]
        static extern IntPtr linphone_core_create_proxy_config (IntPtr lc);

        [DllImport(LIBNAME)]
		static extern IntPtr linphone_auth_info_new (string username, string userid, string passwd, string ha1, string realm, string domain);

        [DllImport(LIBNAME)]
		static extern void linphone_core_add_auth_info (IntPtr lc, IntPtr info);

        [DllImport(LIBNAME)]
		static extern int linphone_proxy_config_set_identity (IntPtr obj, string identity);

        [DllImport(LIBNAME)]
		static extern int linphone_proxy_config_set_server_addr (IntPtr obj, string server_addr);

        [DllImport(LIBNAME)]
		static extern void linphone_proxy_config_enable_register (IntPtr obj, bool val);

        [DllImport(LIBNAME)]
		static extern void linphone_address_destroy (IntPtr u);

        [DllImport(LIBNAME)]
		static extern int linphone_core_add_proxy_config (IntPtr lc, IntPtr cfg);

        [DllImport(LIBNAME)]
		static extern void linphone_core_set_default_proxy (IntPtr lc, IntPtr config);

        [DllImport(LIBNAME)]
		static extern void linphone_core_iterate (IntPtr lc);

        [DllImport(LIBNAME)]
		static extern IntPtr linphone_core_create_default_call_parameters (IntPtr lc);

        [DllImport(LIBNAME)]
		static extern void linphone_call_params_enable_video (IntPtr lc, bool enabled);

        [DllImport(LIBNAME)]
		static extern void linphone_call_params_enable_early_media_sending (IntPtr lc, bool enabled);

        [DllImport(LIBNAME)]
		static extern IntPtr linphone_core_invite_with_params (IntPtr lc, string url, IntPtr callparams);

        [DllImport(LIBNAME)]
		static extern void linphone_call_params_destroy (IntPtr callparams);

        [DllImport(LIBNAME)]
		static extern int linphone_core_terminate_call (IntPtr lc, IntPtr call);

        [DllImport(LIBNAME)]
		static extern int linphone_core_terminate_all_calls (IntPtr lc);

        [DllImport(LIBNAME)]
		static extern int linphone_core_get_default_proxy (IntPtr lc, ref IntPtr config);

        [DllImport(LIBNAME)]
		static extern bool linphone_proxy_config_is_registered (IntPtr config);

        [DllImport(LIBNAME)]
		static extern void linphone_proxy_config_edit (IntPtr config);

        [DllImport(LIBNAME)]
		static extern int linphone_proxy_config_done (IntPtr config);

        [DllImport(LIBNAME)]
        static extern IntPtr linphone_call_get_remote_address_as_string(IntPtr call);

        [DllImport(LIBNAME)]
		static extern int linphone_core_accept_call_with_params (IntPtr lc, IntPtr call, IntPtr callparams);

        [DllImport(LIBNAME)]
		static extern void linphone_call_start_recording (IntPtr call);

        [DllImport(LIBNAME)]
		static extern void linphone_call_stop_recording (IntPtr call);

        [DllImport(LIBNAME)]
		static extern void linphone_call_params_set_record_file (IntPtr callparams, string filename);

        [DllImport(LIBNAME)]
        static extern IntPtr linphone_call_params_get_record_file(IntPtr callparams);

        [DllImport(LIBNAME)]
		static extern int linphone_core_set_sip_transports (IntPtr lc, IntPtr tr_config);

        [DllImport(LIBNAME)]
		static extern void linphone_core_set_user_agent (IntPtr lc, string ua_name, string version);

		[DllImport(LIBNAME)]
		static extern void linphone_core_set_play_file(IntPtr lc, string file);

		[DllImport(LIBNAME)]
		static extern void linphone_core_set_record_file(IntPtr lc, string file);

		[DllImport(LIBNAME)]
		static extern void linphone_core_use_files(IntPtr lc, bool yesno);

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
		}

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void LinphoneCoreRegistrationStateChangedCb (IntPtr lc, IntPtr cfg, LinphoneRegistrationState cstate, string message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void LinphoneCoreCallStateChangedCb (IntPtr lc, IntPtr call, LinphoneCallState cstate, string message);

        LinphoneCoreRegistrationStateChangedCb registration_state_changed;
		LinphoneCoreCallStateChangedCb call_state_changed;
		IntPtr linphoneCore, callsDefaultParams, proxy_cfg, auth_info, t_configPtr, vtablePtr;
		Thread coreLoop;
		bool running = true;
		string identity, server_addr;
		LinphoneCoreVTable vtable;
        LCSipTransports t_config;

		List<LinphoneCall> calls = new List<LinphoneCall> ();

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

		public void CreatePhone (string username, string password, string server, int port, string agent, string version)
		{
			#if (TRACE)
			linphone_core_enable_logs (IntPtr.Zero);
			#else
			linphone_core_disable_logs ();
			#endif

            running = true;
            registration_state_changed = new LinphoneCoreRegistrationStateChangedCb(OnRegistrationChanged);
            call_state_changed = new LinphoneCoreCallStateChangedCb(OnCallStateChanged);
            vtable = new LinphoneCoreVTable()
            {
                global_state_changed = IntPtr.Zero,
                registration_state_changed = Marshal.GetFunctionPointerForDelegate(registration_state_changed),
                call_state_changed = Marshal.GetFunctionPointerForDelegate(call_state_changed),
                notify_presence_received = IntPtr.Zero,
                new_subscription_requested = IntPtr.Zero,
                auth_info_requested = IntPtr.Zero,
                call_log_updated = IntPtr.Zero,
                message_received = IntPtr.Zero,
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
            };
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

            callsDefaultParams = linphone_core_create_default_call_parameters(linphoneCore);
            linphone_call_params_enable_video(callsDefaultParams, false);
            linphone_call_params_enable_early_media_sending(callsDefaultParams, true);

			identity = "sip:" + username + "@" + server;
			server_addr = "sip:" + server + ":" + port.ToString();

			auth_info = linphone_auth_info_new (username, null, password, null, null, null);
			linphone_core_add_auth_info (linphoneCore, auth_info);

            proxy_cfg = linphone_core_create_proxy_config(linphoneCore);
			linphone_proxy_config_set_identity (proxy_cfg, identity);
			linphone_proxy_config_set_server_addr (proxy_cfg, server_addr);
			linphone_proxy_config_enable_register (proxy_cfg, true);
			linphone_core_add_proxy_config (linphoneCore, proxy_cfg);
            linphone_core_set_default_proxy (linphoneCore, proxy_cfg);
		}

		public void DestroyPhone ()
		{
            if (RegistrationStateChangedEvent != null)
                RegistrationStateChangedEvent(LinphoneRegistrationState.LinphoneRegistrationProgress); // disconnecting

			linphone_core_terminate_all_calls (linphoneCore);

			SetTimeout (delegate {
				linphone_call_params_destroy (callsDefaultParams);

				if (linphone_proxy_config_is_registered (proxy_cfg)) {
					linphone_proxy_config_edit (proxy_cfg);
					linphone_proxy_config_enable_register (proxy_cfg, false);
					linphone_proxy_config_done (proxy_cfg);
				}

				SetTimeout (delegate {
					running = false;
				}, 10000);

			}, 5000);
		}

		void LinphoneMainLoop ()
		{
			while (running)
			{
                linphone_core_iterate (linphoneCore); // roll
				System.Threading.Thread.Sleep (50);
			}

			linphone_core_destroy (linphoneCore);

            if (vtablePtr != IntPtr.Zero)
                Marshal.FreeHGlobal(vtablePtr);
            if (t_configPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(t_configPtr);
			registration_state_changed = null;
			call_state_changed = null;
			linphoneCore = callsDefaultParams = proxy_cfg = auth_info = t_configPtr = IntPtr.Zero;
			coreLoop = null;
			identity = null;
            server_addr = null;

            if (RegistrationStateChangedEvent != null)
                RegistrationStateChangedEvent(LinphoneRegistrationState.LinphoneRegistrationCleared);
		}

		public void TerminateCall (Call call)
		{
			if (call == null)
				throw new ArgumentNullException ("call");

			if (linphoneCore == IntPtr.Zero || !running) {
				if (ErrorEvent != null)
					ErrorEvent (call, "Cannot make or receive calls when Linphone Core is not working.");
				return;
			}

			LinphoneCall linphonecall = (LinphoneCall) call;
			linphone_core_terminate_call (linphoneCore, linphonecall.LinphoneCallPtr);
		}

		public void MakeCall (string uri)
		{
			if (linphoneCore == IntPtr.Zero || !running) {
				if (ErrorEvent != null)
					ErrorEvent (null, "Cannot make or receive calls when Linphone Core is not working.");
				return;
			}

			IntPtr call = linphone_core_invite_with_params (linphoneCore, uri, callsDefaultParams);

			if (call == IntPtr.Zero) {
				if (ErrorEvent != null)
					ErrorEvent (null, "Cannot call.");
				return;
			}
		}

		public void ReceiveCallAndRecord (Call call, string filename)
		{
			if (call == null)
				throw new ArgumentNullException ("call");

			if (linphoneCore == IntPtr.Zero || !running) {
				if (ErrorEvent != null)
					ErrorEvent (call, "Cannot make or receive calls when Linphone Core is not working.");
				return;
			}

			LinphoneCall linphonecall = (LinphoneCall) call;
			linphone_call_params_set_record_file (callsDefaultParams, filename);
			linphone_core_accept_call_with_params (linphoneCore, linphonecall.LinphoneCallPtr, callsDefaultParams);
			linphone_call_start_recording (linphonecall.LinphoneCallPtr);
		}

		public void ReceiveCall (Call call)
		{
			if (call == null)
				throw new ArgumentNullException ("call");

			if (linphoneCore == IntPtr.Zero || !running) {
				if (ErrorEvent != null)
					ErrorEvent (call, "Cannot receive call when Linphone Core is not working.");
				return;
			}

			LinphoneCall linphonecall = (LinphoneCall) call;
			linphone_call_params_set_record_file (callsDefaultParams, null);
			linphone_core_accept_call_with_params (linphoneCore, linphonecall.LinphoneCallPtr, callsDefaultParams);
		}

		public delegate void RegistrationStateChangedDelegate (LinphoneRegistrationState state);
		public event RegistrationStateChangedDelegate RegistrationStateChangedEvent;

		public delegate void CallStateChangedDelegate (Call call);
		public event CallStateChangedDelegate CallStateChangedEvent;

		public delegate void ErrorDelegate (Call call, string message);
		public event ErrorDelegate ErrorEvent;

		void OnRegistrationChanged (IntPtr lc, IntPtr cfg, LinphoneRegistrationState cstate, string message) 
		{
			if (linphoneCore == IntPtr.Zero || !running) return;
            #if (TRACE)
            Console.WriteLine("OnRegistrationChanged: {0}", cstate);
            #endif
            if (RegistrationStateChangedEvent != null)
				RegistrationStateChangedEvent (cstate);
		}

		void OnCallStateChanged (IntPtr lc, IntPtr call, LinphoneCallState cstate, string message)
		{
			if (linphoneCore == IntPtr.Zero || !running) return;
            #if (TRACE)
            Console.WriteLine("OnCallStateChanged: {0}", cstate);
            #endif

			Call.CallState newstate = Call.CallState.None;
			Call.CallType newtype = Call.CallType.None;
			string from = "";
			string to = "";
            IntPtr addressStringPtr;

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
					if (linphone_call_params_get_record_file (callsDefaultParams) != IntPtr.Zero)
						linphone_call_stop_recording (call);
					break;

				default:
					break;
			}

			LinphoneCall existCall = FindCall (call);

			if (existCall == null) {
				existCall = new LinphoneCall ();
				existCall.SetCallState (newstate);
				existCall.SetCallType (newtype);
				existCall.SetFrom (from);
				existCall.SetTo (to);
				existCall.LinphoneCallPtr = call;

				calls.Add (existCall);

				if ((CallStateChangedEvent != null))
					CallStateChangedEvent (existCall);
			} else {
				if (existCall.GetState () != newstate) {
					existCall.SetCallState (newstate);
					CallStateChangedEvent (existCall);
				}
			}
		}

	}
}

