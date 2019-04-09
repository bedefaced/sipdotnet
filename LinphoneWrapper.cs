using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using static sipdotnet.Linphone;
using System.Reflection;

namespace sipdotnet
{
    class LinphoneWrapper
    {
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

            public void SetRecordFile (string recordfile)
            {
                this.recordfile = recordfile;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void LinphoneCoreRegistrationStateChangedCb (IntPtr lc, IntPtr cfg, LinphoneRegistrationState cstate, string message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void LinphoneCoreCallStateChangedCb (IntPtr lc, IntPtr call, LinphoneCallState cstate, string message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void LogEventCb (string domain, OrtpLogLevel lev, string fmt, IntPtr args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void LinphoneCoreCbsMessageReceivedCb (IntPtr lc, IntPtr room, IntPtr message);

        static LogEventCb logevent_cb;
        LinphoneCoreRegistrationStateChangedCb registration_state_changed;
        LinphoneCoreCallStateChangedCb call_state_changed;
        LinphoneCoreCbsMessageReceivedCb message_received;
        IntPtr linphoneCore, proxy_cfg, auth_info, t_configPtr, vtablePtr, natPolicy;
        Thread coreLoop;
        bool running = true;
        string identity, server_addr;
        LinphoneCoreVTable vtable;
        LCSipTransports t_config;

        List<LinphoneCall> calls = new List<LinphoneCall>();

        public delegate void RegistrationStateChangedDelegate (LinphoneRegistrationState state);
        public event RegistrationStateChangedDelegate RegistrationStateChangedEvent;

        public delegate void CallStateChangedDelegate (Call call);
        public event CallStateChangedDelegate CallStateChangedEvent;

        public delegate void ErrorDelegate (Call call, string message);
        public event ErrorDelegate ErrorEvent;

        public delegate void MessageReceivedDelegate (string from, string message);
        public event MessageReceivedDelegate MessageReceivedEvent;

        private static bool logsEnabled = false;
        public static bool LogsEnabled { get => logsEnabled; set => logsEnabled = value; }
        public delegate void LogDelegate (string message);
        private static event LogDelegate logEventHandler;
        public static event LogDelegate LogEvent
        {
            add
            {
                if (logEventHandler == null && LogsEnabled)
                {
                    linphone_core_set_log_level(OrtpLogLevel.DEBUG);
                    if (logevent_cb == null)
                    {
                        logevent_cb = new LogEventCb(LinphoneLogEvent);
                    }

                    linphone_core_set_log_handler(Marshal.GetFunctionPointerForDelegate(logevent_cb));
                }
                logEventHandler += value;
            }

            remove
            {
                logEventHandler -= value;
                if (logEventHandler == null)
                {
                    linphone_core_set_log_level(OrtpLogLevel.END);
                }
            }
        }

        static void LinphoneLogEvent (string domain, OrtpLogLevel lev, string fmt, IntPtr args)
        {
            logEventHandler?.Invoke(DllLoadUtils.ProcessVAlist(fmt, args));
        }


        LinphoneCall FindCall (IntPtr call)
        {
            return calls.Find(delegate (LinphoneCall obj) {
                return (obj.LinphoneCallPtr == call);
            });
        }

        void SetTimeout (Action callback, int miliseconds)
        {
            System.Timers.Timer timeout = new System.Timers.Timer();
            timeout.Interval = miliseconds;
            timeout.AutoReset = false;
            timeout.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) => {
                callback();
            };
            timeout.Start();
        }

        IntPtr createDefaultCallParams ()
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

        public bool MicrophoneEnabled
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                return linphone_core_mic_enabled(linphoneCore);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                linphone_core_enable_mic(linphoneCore, value);
            }
        }

        public bool KeepAliveEnabled
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                return linphone_core_keep_alive_enabled(linphoneCore);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                linphone_core_enable_keep_alive(linphoneCore, value);
            }
        }

        public bool EchoCancellationEnabled
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                return linphone_core_echo_cancellation_enabled(linphoneCore);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                linphone_core_enable_echo_cancellation(linphoneCore, value);
            }
        }

        public string RingerDevice
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                IntPtr devChar = linphone_core_get_ringer_device(linphoneCore);

                return Marshal.PtrToStringAnsi(devChar);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                linphone_core_set_ringer_device(linphoneCore, value);
            }
        }

        public string PlaybackDevice
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                IntPtr devChar = linphone_core_get_playback_device(linphoneCore);

                return Marshal.PtrToStringAnsi(devChar);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                linphone_core_set_playback_device(linphoneCore, value);
            }
        }

        public string CaptureDevice
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                IntPtr devChar = linphone_core_get_capture_device(linphoneCore);

                return Marshal.PtrToStringAnsi(devChar);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                linphone_core_set_capture_device(linphoneCore, value);
            }
        }

        public List<string> GetPlaybackDevices ()
        {
            if (linphoneCore == IntPtr.Zero || !running)
                throw new InvalidOperationException("linphoneCore not started");

            List<string> devList = new List<string>();

            linphone_core_reload_sound_devices(linphoneCore);

            IntPtr listPtr = linphone_core_get_sound_devices(linphoneCore);

            if (listPtr != IntPtr.Zero)
            {
                IntPtr ptr = Marshal.ReadIntPtr(listPtr);
                while (ptr != IntPtr.Zero)
                {
                    string device = Marshal.PtrToStringAnsi(ptr);

                    if (linphone_core_sound_device_can_playback(linphoneCore, device))
                        devList.Add(device);

                    listPtr = new IntPtr(listPtr.ToInt64() + IntPtr.Size);
                    ptr = Marshal.ReadIntPtr(listPtr);
                }
            }

            return devList;
        }

        public List<string> GetCaptureDevices ()
        {
            if (linphoneCore == IntPtr.Zero || !running)
                throw new InvalidOperationException("linphoneCore not started");

            List<string> devList = new List<string>();

            linphone_core_reload_sound_devices(linphoneCore);

            IntPtr listPtr = linphone_core_get_sound_devices(linphoneCore);

            if (listPtr != IntPtr.Zero)
            {
                IntPtr ptr = Marshal.ReadIntPtr(listPtr);
                while (ptr != IntPtr.Zero)
                {
                    string device = Marshal.PtrToStringAnsi(ptr);

                    if (linphone_core_sound_device_can_capture(linphoneCore, device))
                        devList.Add(device);

                    listPtr = new IntPtr(listPtr.ToInt64() + IntPtr.Size);
                    ptr = Marshal.ReadIntPtr(listPtr);
                }
            }

            return devList;
        }

#if (DEBUG)
        static LinphoneWrapper ()
        {
            IntPtr dllPtr = DllLoadUtils.DoLoadLibrary(Linphone.LIBNAME);

            foreach (MethodInfo info in typeof(Linphone).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (DllLoadUtils.DoGetProcAddress(dllPtr, info.Name) == IntPtr.Zero)
                {
                    throw new EntryPointNotFoundException(String.Format("Invalid linphone library version: {0} is not found.", info.Name));
                }
            }

            DllLoadUtils.DoFreeLibrary(dllPtr);
        }
#endif

        public LinphoneWrapper ()
        {
            linphone_core_set_log_level(OrtpLogLevel.END);
        }
        
        public void CreatePhone (string username, string password, string server, int port, string agent, string version,
            bool use_stun, bool use_turn, bool use_ice, bool use_upnp, string stun_server)
		{
            running = true;

            registration_state_changed = new LinphoneCoreRegistrationStateChangedCb(OnRegistrationChanged);
            call_state_changed = new LinphoneCoreCallStateChangedCb(OnCallStateChanged);
            message_received = new LinphoneCoreCbsMessageReceivedCb(OnMessageReceived);

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
                message_received = Marshal.GetFunctionPointerForDelegate(message_received),
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
            if (!string.IsNullOrEmpty(linphonecall.Recordfile))
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
            if (!string.IsNullOrEmpty(linphonecall.Recordfile))
                linphone_call_stop_recording (linphonecall.LinphoneCallPtr);
        }

        public void PauseCall (Call call)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                return;
            }

            LinphoneCall linphonecall = (LinphoneCall) call;
            linphone_core_pause_call(linphoneCore, linphonecall.LinphoneCallPtr);
        }

        public void ResumeCall (Call call)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                return;
            }

            LinphoneCall linphonecall = (LinphoneCall) call;
            linphone_core_resume_call(linphoneCore, linphonecall.LinphoneCallPtr);
        }

        public void RedirectCall (Call call, string redirect_uri)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                return;
            }

            LinphoneCall linphonecall = (LinphoneCall) call;
            linphone_call_redirect(linphoneCore, linphonecall.LinphoneCallPtr, redirect_uri);
        }

        public void TransferCall (Call call, string redirect_uri)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                return;
            }

            LinphoneCall linphonecall = (LinphoneCall) call;
            linphone_call_transfer(linphoneCore, linphonecall.LinphoneCallPtr, redirect_uri);
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

        public void SendMessage (string to, string message)
        {
            if (linphoneCore == IntPtr.Zero || !running)
            {
                ErrorEvent?.Invoke(null, "Cannot send messages when Linphone Core is not working.");
                return;
            }

            IntPtr chat_room = linphone_core_get_chat_room_from_uri (linphoneCore, to);
            IntPtr chat_message = linphone_chat_room_create_message (chat_room, message);
            linphone_chat_room_send_chat_message(chat_room, chat_message);
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
                    if (addressStringPtr != IntPtr.Zero)
                        from = Marshal.PtrToStringAnsi(addressStringPtr);

                    to = this.identity;

#if (ADDRESS_BY_CALLDATA)
                    addressStringPtr = linphone_call_get_to_address(call);
                    if (addressStringPtr != IntPtr.Zero)
                    {
                        addressStringPtr = linphone_address_as_string_uri_only(addressStringPtr);
                        to = Marshal.PtrToStringAnsi(addressStringPtr);
                    }
#endif
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
                    if (addressStringPtr != IntPtr.Zero)
                        to = Marshal.PtrToStringAnsi(addressStringPtr);

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
				if (existCall.State != newstate) {
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

        void OnMessageReceived (IntPtr lc, IntPtr room, IntPtr message)
        {
            IntPtr from = linphone_chat_room_get_peer_address(room);
            if (from != IntPtr.Zero)
            {
                IntPtr addressStringPtr = linphone_address_as_string(from);
                IntPtr chatmessage = linphone_chat_message_get_text(message);

                if (addressStringPtr != IntPtr.Zero && chatmessage != IntPtr.Zero)
                {
                    MessageReceivedEvent?.Invoke(Marshal.PtrToStringAnsi(addressStringPtr),
                        Marshal.PtrToStringAnsi(chatmessage));
                }
            }
        }

    }
}

