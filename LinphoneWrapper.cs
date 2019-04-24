using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using static sipdotnet.Linphone;
using System.Reflection;
using System.Collections.Concurrent;
#if (DEBUG)
using System.Diagnostics;
#endif

namespace sipdotnet
{
    class LinphoneWrapper
    {
        class ProducerConsumerQueue<T> : BlockingCollection<T>
        {
            /// <summary>
            /// Initializes a new instance of the ProducerConsumerQueue, Use Add and TryAdd for Enqueue and TryEnqueue and Take and TryTake for Dequeue and TryDequeue functionality
            /// </summary>
            public ProducerConsumerQueue ()
                : base(new ConcurrentQueue<T>())
            {
            }

            /// <summary>
            /// Initializes a new instance of the ProducerConsumerQueue, Use Add and TryAdd for Enqueue and TryEnqueue and Take and TryTake for Dequeue and TryDequeue functionality
            /// </summary>
            /// <param name="maxSize"></param>
            public ProducerConsumerQueue (int maxSize)
                : base(new ConcurrentQueue<T>(), maxSize)
            {
            }

        }

        class NativeCallTask
        {
            public bool IsCompleted { get; set; }
            public Object ResultObject { get; set; }
            public Func<Object> FunctionCode { get; set; }

            public NativeCallTask(Func<Object> function)
            {
                IsCompleted = false;
                FunctionCode = function;
            }
        }

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

        class LinphoneAudioCodec : AudioCodec
        {
            LinphoneWrapper phoneWrapper;

            public LinphoneAudioCodec (LinphoneWrapper phoneWrapper, string name, int clockrate, bool enabled) : base(name, clockrate, enabled)
            {
                this.phoneWrapper = phoneWrapper;
            }

            public override bool Enabled
            {
                get
                {
                    return this.enabled;
                }
                
                set
                {
                    if (phoneWrapper.linphoneCore == null)
                        throw new InvalidOperationException("phone not connected");

                    phoneWrapper.ToggleAudioCodec(this, value);

                    this.enabled = value;
                }
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

        private const int MillisecondsLoopTime = 20; // recommended value from linphone/include/linphone/core.h
        static LogEventCb logevent_cb;
        LinphoneCoreRegistrationStateChangedCb registration_state_changed;
        LinphoneCoreCallStateChangedCb call_state_changed;
        LinphoneCoreCbsMessageReceivedCb message_received;
        IntPtr linphoneCore, proxy_cfg, auth_info, t_configPtr, factory, cbs, natPolicy, config;
        Thread coreLoop;
        bool running = true;
        string identity, server_addr;

        List<LinphoneCall> calls = new List<LinphoneCall>();

        public delegate void RegistrationStateChangedDelegate (LinphoneRegistrationState state);
        public event RegistrationStateChangedDelegate RegistrationStateChangedEvent;

        public delegate void CallStateChangedDelegate (Call call);
        public event CallStateChangedDelegate CallStateChangedEvent;

        public delegate void ErrorDelegate (Call call, string message);
        public event ErrorDelegate ErrorEvent;

        public delegate void MessageReceivedDelegate (string from, string message);
        public event MessageReceivedDelegate MessageReceivedEvent;

        AutoResetEvent dequeueAutoReset = new AutoResetEvent(false);
        ProducerConsumerQueue<NativeCallTask> LinphoneNativeCallsQueue = new ProducerConsumerQueue<NativeCallTask>();

        static LinphoneWrapper lastCreatedLinphoneWrapper;

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
                    lastCreatedLinphoneWrapper.NativeFunctionCall(() =>
                    {
                        linphone_core_set_log_level(OrtpLogLevel.DEBUG);
                        return null;
                    }, false); // mb no active linphone

                    if (logevent_cb == null)
                    {
                        logevent_cb = new LogEventCb(LinphoneLogEvent);
                    }

                    lastCreatedLinphoneWrapper.NativeFunctionCall(() =>
                    {
                        linphone_core_set_log_handler(Marshal.GetFunctionPointerForDelegate(logevent_cb));
                        return null;
                    }, false); // mb no active linphone
                }
                logEventHandler += value;
            }

            remove
            {
                logEventHandler -= value;
                if (logEventHandler == null)
                {
                    lastCreatedLinphoneWrapper.NativeFunctionCall(() =>
                    {
                        linphone_core_set_log_level(OrtpLogLevel.END);
                        return null;
                    }, false); // mb no active linphone
                }
            }
        }

        static void LinphoneLogEvent (string domain, OrtpLogLevel lev, string fmt, IntPtr args)
        {
            string message = DllLoadUtils.ProcessVAlist(fmt, args);

            ThreadPool.QueueUserWorkItem(unused =>
            {
                logEventHandler?.Invoke(message);
            });
        }

        LinphoneCall FindCall (IntPtr call)
        {
            return calls.Find(delegate (LinphoneCall obj) {
                return (obj.LinphoneCallPtr == call);
            });
        }

        private IntPtr createDefaultCallParams ()
        {
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

                return (bool) NativeFunctionCall(() =>
                {
                    return linphone_core_mic_enabled(linphoneCore);
                }, true);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                NativeFunctionCall(() =>
                {
                    linphone_core_enable_mic(linphoneCore, value);
                    return null;
                }, true);
            }
        }

        public bool KeepAliveEnabled
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                return (bool) NativeFunctionCall(() =>
                {
                    return linphone_core_keep_alive_enabled(linphoneCore);
                }, true);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                NativeFunctionCall(() =>
                {
                    linphone_core_enable_keep_alive(linphoneCore, value);
                    return null;
                }, true);
            }
        }

        public bool EchoCancellationEnabled
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                return (bool) NativeFunctionCall(() =>
                {
                    return linphone_core_echo_cancellation_enabled(linphoneCore);
                }, true);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                NativeFunctionCall(() =>
                {
                    linphone_core_enable_echo_cancellation(linphoneCore, value);
                    return null;
                }, true);
            }
        }

        public string RingerDevice
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                return (string) NativeFunctionCall(() =>
                {
                    IntPtr devChar = linphone_core_get_ringer_device(linphoneCore);
                    return Marshal.PtrToStringAnsi(devChar);
                }, true);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                NativeFunctionCall(() =>
                {
                    linphone_core_set_ringer_device(linphoneCore, value);
                    return null;
                }, true);
            }
        }

        public string PlaybackDevice
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                return (string) NativeFunctionCall(() =>
                {
                    IntPtr devChar = linphone_core_get_playback_device(linphoneCore);
                    return Marshal.PtrToStringAnsi(devChar);
                }, true);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                NativeFunctionCall(() =>
                {
                    linphone_core_set_playback_device(linphoneCore, value);
                    return null;
                }, true);
            }
        }

        public string CaptureDevice
        {
            get
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                return (string) NativeFunctionCall(() =>
                {
                    IntPtr devChar = linphone_core_get_capture_device(linphoneCore);
                    return Marshal.PtrToStringAnsi(devChar);
                }, true);
            }

            set
            {
                if (linphoneCore == IntPtr.Zero || !running)
                    throw new InvalidOperationException("linphoneCore not started");

                NativeFunctionCall(() =>
                {
                    linphone_core_set_capture_device(linphoneCore, value);
                    return null;
                }, true);
            }
        }

        public List<string> GetPlaybackDevices ()
        {
            if (linphoneCore == IntPtr.Zero || !running)
                throw new InvalidOperationException("linphoneCore not started");

            return (List<string>) NativeFunctionCall(() =>
            {
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

            }, true);
        }

        public List<string> GetCaptureDevices ()
        {
            if (linphoneCore == IntPtr.Zero || !running)
                throw new InvalidOperationException("linphoneCore not started");

            return (List<string>) NativeFunctionCall(() =>
            {
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
            }, true);
        }

        public void ToggleAudioCodec(AudioCodec codec, bool enabled)
        {
            if (linphoneCore == IntPtr.Zero || !running)
                throw new InvalidOperationException("linphoneCore not started");

            NativeFunctionCall(() =>
            {
                IntPtr lpt = linphone_core_get_payload_type(linphoneCore, codec.Name, codec.ClockRate, 
                    LINPHONE_FIND_PAYLOAD_IGNORE_CHANNELS);

                linphone_payload_type_enable(lpt, enabled);

                return null;

            }, true);
        }

        public List<AudioCodec> GetAudioCodecs ()
        {
            if (linphoneCore == IntPtr.Zero || !running)
                throw new InvalidOperationException("linphoneCore not started");

            return (List<AudioCodec>) NativeFunctionCall(() =>
            {
                List<AudioCodec> codecList = new List<AudioCodec>();

                IntPtr listPtr = linphone_core_get_audio_payload_types(linphoneCore);

                while (listPtr != IntPtr.Zero)
                {
                    bctbx_list list = (bctbx_list) Marshal.PtrToStructure(listPtr, typeof(bctbx_list));
                    IntPtr payload = list.data;
                    string mime = Marshal.PtrToStringAnsi(linphone_payload_type_get_mime_type(payload));
                    int clockrate = linphone_payload_type_get_clock_rate(payload);
                    bool enabled = linphone_payload_type_enabled(payload);
                    codecList.Add(new LinphoneAudioCodec(this, mime, clockrate, enabled));
                    linphone_payload_type_enable(payload, true);
                    listPtr = list.next;
                }

                return codecList;
            }, true);
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
            lastCreatedLinphoneWrapper = this;
            linphone_core_set_log_level(OrtpLogLevel.END);
        }
        
        public void CreatePhone (string username, string password, string server, int port, string agent, string version,
            bool use_stun, bool use_turn, bool use_ice, bool use_upnp, string stun_server, int expires = 3600, 
            string configFile = null)
		{
            factory = linphone_factory_get();
            
            cbs = linphone_factory_create_core_cbs(factory);
            registration_state_changed = new LinphoneCoreRegistrationStateChangedCb(OnRegistrationChanged);
            call_state_changed = new LinphoneCoreCallStateChangedCb(OnCallStateChanged);
            message_received = new LinphoneCoreCbsMessageReceivedCb(OnMessageReceived);
            linphone_core_cbs_set_registration_state_changed(cbs, Marshal.GetFunctionPointerForDelegate(registration_state_changed));
            linphone_core_cbs_set_call_state_changed(cbs, Marshal.GetFunctionPointerForDelegate(call_state_changed));
            linphone_core_cbs_set_message_received(cbs, Marshal.GetFunctionPointerForDelegate(message_received));

            config = linphone_config_new(configFile); // if configFile is present will be created from file
            linphoneCore = linphone_factory_create_core_with_config(factory, cbs, config);

            if (configFile == null)
            {
                t_configPtr = linphone_transports_new();
                linphone_transports_set_udp_port(t_configPtr, LC_SIP_TRANSPORT_RANDOM);
                linphone_transports_set_tcp_port(t_configPtr, LC_SIP_TRANSPORT_RANDOM);
                linphone_transports_set_tls_port(t_configPtr, LC_SIP_TRANSPORT_RANDOM);
                linphone_transports_set_dtls_port(t_configPtr, LC_SIP_TRANSPORT_RANDOM);
                linphone_core_set_sip_transports(linphoneCore, t_configPtr);

                linphone_core_set_user_agent(linphoneCore, agent, version);

                identity = "sip:" + username + "@" + server;
                server_addr = "sip:" + server + ":" + port.ToString();

                auth_info = linphone_auth_info_new(username, null, password, null, null, null);
                linphone_core_add_auth_info(linphoneCore, auth_info);

                natPolicy = linphone_core_create_nat_policy(linphoneCore);
                natPolicy = linphone_nat_policy_ref(natPolicy);
                linphone_nat_policy_enable_stun(natPolicy, use_stun);
                linphone_nat_policy_enable_turn(natPolicy, use_turn);
                linphone_nat_policy_enable_ice(natPolicy, use_ice);
                linphone_nat_policy_enable_upnp(natPolicy, use_upnp);
                if (!string.IsNullOrEmpty(stun_server))
                {
                    linphone_nat_policy_set_stun_server(natPolicy, stun_server);
                    linphone_nat_policy_resolve_stun_server(natPolicy);
                }

                proxy_cfg = linphone_core_create_proxy_config(linphoneCore);
                linphone_proxy_config_set_identity(proxy_cfg, identity);
                linphone_proxy_config_set_server_addr(proxy_cfg, server_addr);
                linphone_proxy_config_set_expires(proxy_cfg, expires);
                linphone_proxy_config_enable_register(proxy_cfg, true);

                linphone_proxy_config_set_nat_policy(proxy_cfg, natPolicy);

                linphone_core_add_proxy_config(linphoneCore, proxy_cfg);
                linphone_core_set_default_proxy_config(linphoneCore, proxy_cfg);
            } 

            running = true;
            coreLoop = new Thread(LinphoneMainLoop);
            coreLoop.IsBackground = false;
            coreLoop.Start();
        }
       
        public void DestroyPhone ()
		{
            ThreadPool.QueueUserWorkItem(unused =>
            {
                RegistrationStateChangedEvent?.Invoke(LinphoneRegistrationState.LinphoneRegistrationProgress); // disconnecting
            });

            NativeFunctionCall(() =>
            {
                linphone_core_terminate_all_calls(linphoneCore);
                return null;
            }, true);

            NativeFunctionCall(() =>
            {
                if (proxy_cfg != IntPtr.Zero && linphone_proxy_config_is_registered(proxy_cfg))
                {
                    linphone_proxy_config_edit(proxy_cfg);
                    linphone_proxy_config_enable_register(proxy_cfg, false);
                    linphone_proxy_config_done(proxy_cfg);
                }

                return null;
            }, true);

            running = false;

        }

        Object NativeFunctionCall (Func<Object> function, bool waitForResult)
        {
            NativeCallTask task = new NativeCallTask(function);
            LinphoneNativeCallsQueue.Add(task);
            while (waitForResult && !task.IsCompleted)
            {
#if (DEBUG)
                Debug.WriteLine("Waiting for task completion: " + task.FunctionCode.Method.ToString());
#endif
                dequeueAutoReset.WaitOne();
            }
#if (DEBUG)
            Debug.WriteLine(task.FunctionCode.Method.ToString() + " completed!");
#endif
            return task.ResultObject;
        }

        void LinphoneMainLoop()
        {
            NativeCallTask task;
            
            while (running)
            {
                linphone_core_iterate(linphoneCore); // roll

                if (LinphoneNativeCallsQueue.TryTake(out task, MillisecondsLoopTime))
                {
#if (DEBUG)
                    Debug.WriteLine("Task taken: " + task.FunctionCode.Method.ToString());
#endif
                    task.ResultObject = task.FunctionCode();
                    task.IsCompleted = true;
                    dequeueAutoReset.Set();
                }
            }

            while (LinphoneNativeCallsQueue.Count > 0)
            {
                LinphoneNativeCallsQueue.Take();
            }

            if (natPolicy != IntPtr.Zero) linphone_nat_policy_unref(natPolicy);
            linphone_core_unref(linphoneCore);

            if (t_configPtr != IntPtr.Zero) Marshal.FreeHGlobal(t_configPtr);
            registration_state_changed = null;
            call_state_changed = null;
            message_received = null;
            linphoneCore = proxy_cfg = auth_info = t_configPtr = factory = cbs = config = IntPtr.Zero;
            coreLoop = null;
            identity = null;
            server_addr = null;

            ThreadPool.QueueUserWorkItem(unused =>
            {
                RegistrationStateChangedEvent?.Invoke(LinphoneRegistrationState.LinphoneRegistrationCleared);
            });

        }
        
        public void SendDTMFs (Call call, string dtmfs)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                });
                return;
            }

            NativeFunctionCall(() =>
            {
                LinphoneCall linphonecall = (LinphoneCall) call;
                linphone_call_send_dtmfs(linphonecall.LinphoneCallPtr, dtmfs);
                return null;
            }, true);
        }

        public void SetRingbackSound (string file)
        {
            if (linphoneCore == IntPtr.Zero || !running)
            {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(null, "Cannot modify configuration when Linphone Core is not working.");
                });
                return;
            }

            NativeFunctionCall(() =>
            {
                linphone_core_set_ringback(linphoneCore, file);
                return null;
            }, true);
        }

        public void SetIncomingRingSound (string file)
        {
            if (linphoneCore == IntPtr.Zero || !running)
            {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(null, "Cannot modify configuration when Linphone Core is not working.");
                });
                return;
            }

            NativeFunctionCall(() =>
            {
                linphone_core_set_ring(linphoneCore, file);
                return null;
            }, true);
        }
        
        public void TerminateCall (Call call)
		{
			if (call == null)
				throw new ArgumentNullException ("call");

			if (linphoneCore == IntPtr.Zero || !running) {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                });
                return;
			}

            NativeFunctionCall(() =>
            {
                LinphoneCall linphonecall = (LinphoneCall) call;
                linphone_core_terminate_call(linphoneCore, linphonecall.LinphoneCallPtr);
                return null;
            }, true);
        }

		public void MakeCall (string uri)
		{
            MakeCallAndRecord(uri, null, false);
        }

		public void MakeCallAndRecord (string uri, string filename, bool startRecordInstantly = true)
		{
			if (linphoneCore == IntPtr.Zero || !running) {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(null, "Cannot make or receive calls when Linphone Core is not working.");
                });
                return;
			}

            NativeFunctionCall(() =>
            {
                IntPtr callParams = createDefaultCallParams();

                if (!string.IsNullOrEmpty(filename))
                    linphone_call_params_set_record_file(callParams, filename);

                IntPtr call = linphone_core_invite_with_params(linphoneCore, uri, callParams);
                if (call == IntPtr.Zero)
                {
                    ThreadPool.QueueUserWorkItem(unused =>
                    {
                        ErrorEvent?.Invoke(null, "Cannot call.");
                    });
                    return null;
                }

                linphone_call_ref(call);
                if (startRecordInstantly)
                {
                    linphone_call_start_recording(call);
                }

                return null;

            }, true);
		}

        public void StartRecording (Call call)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                });
                return;
            }

            NativeFunctionCall(() =>
            {
                LinphoneCall linphonecall = (LinphoneCall) call;
                if (!string.IsNullOrEmpty(linphonecall.Recordfile))
                    linphone_call_start_recording(linphonecall.LinphoneCallPtr);
                return null;
            }, true);
        }

        public void PauseRecording (Call call)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                });
                return;
            }

            NativeFunctionCall(() =>
            {
                LinphoneCall linphonecall = (LinphoneCall) call;
                if (!string.IsNullOrEmpty(linphonecall.Recordfile))
                    linphone_call_stop_recording(linphonecall.LinphoneCallPtr);
                return null;
            }, true);
        }

        public void PauseCall (Call call)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                });
                return;
            }

            NativeFunctionCall(() =>
            {
                LinphoneCall linphonecall = (LinphoneCall) call;
                linphone_core_pause_call(linphoneCore, linphonecall.LinphoneCallPtr);
                return null;
            }, true);
        }

        public void ResumeCall (Call call)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                });
                return;
            }

            NativeFunctionCall(() =>
            {
                LinphoneCall linphonecall = (LinphoneCall) call;
                linphone_core_resume_call(linphoneCore, linphonecall.LinphoneCallPtr);
                return null;
            }, true);
        }

        public void RedirectCall (Call call, string redirect_uri)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                });
                return;
            }

            NativeFunctionCall(() =>
            {
                LinphoneCall linphonecall = (LinphoneCall) call;
                linphone_call_redirect(linphoneCore, linphonecall.LinphoneCallPtr, redirect_uri);
                return null;
            }, true);
        }

        public void TransferCall (Call call, string redirect_uri)
        {
            if (call == null)
                throw new ArgumentNullException("call");

            if (linphoneCore == IntPtr.Zero || !running)
            {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                });
                return;
            }

            NativeFunctionCall(() =>
            {
                LinphoneCall linphonecall = (LinphoneCall) call;
                linphone_call_transfer(linphoneCore, linphonecall.LinphoneCallPtr, redirect_uri);
                return null;
            }, true);
        }

        public void ReceiveCallAndRecord (Call call, string filename, bool startRecordInstantly = true)
		{
			if (call == null)
				throw new ArgumentNullException ("call");

			if (linphoneCore == IntPtr.Zero || !running) {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(call, "Cannot make or receive calls when Linphone Core is not working.");
                });
                return;
			}

            NativeFunctionCall(() =>
            {
                LinphoneCall linphonecall = (LinphoneCall) call;
                linphone_call_ref(linphonecall.LinphoneCallPtr);

                IntPtr callParams = createDefaultCallParams();

                if (!string.IsNullOrEmpty(filename))
                    linphone_call_params_set_record_file(callParams, filename);

                linphone_core_accept_call_with_params(linphoneCore, linphonecall.LinphoneCallPtr, callParams);
                if (startRecordInstantly)
                {
                    linphone_call_start_recording(linphonecall.LinphoneCallPtr);
                }

                return null;

            }, true);
		}

		public void ReceiveCall (Call call)
		{
            ReceiveCallAndRecord(call, null, false);
        }

        public void SendMessage (string to, string message)
        {
            if (linphoneCore == IntPtr.Zero || !running)
            {
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    ErrorEvent?.Invoke(null, "Cannot send messages when Linphone Core is not working.");
                });
                return;
            }

            NativeFunctionCall(() =>
            {
                IntPtr chat_room = linphone_core_get_chat_room_from_uri(linphoneCore, to);
                IntPtr chat_message = linphone_chat_room_create_message(chat_room, message);
                linphone_chat_room_send_chat_message(chat_room, chat_message);
                return null;
            }, true);
        }

		void OnRegistrationChanged (IntPtr lc, IntPtr cfg, LinphoneRegistrationState cstate, string message) 
		{
			if (linphoneCore == IntPtr.Zero || !running) return;
            
            ThreadPool.QueueUserWorkItem(unused =>
            {
                logEventHandler?.Invoke("OnRegistrationChanged: " + cstate);
                RegistrationStateChangedEvent?.Invoke(cstate);
            });
        }

		void OnCallStateChanged (IntPtr lc, IntPtr call, LinphoneCallState cstate, string message)
		{
			if (linphoneCore == IntPtr.Zero || !running) return;
            ThreadPool.QueueUserWorkItem(unused =>
            {
                logEventHandler?.Invoke("OnCallStateChanged: " + cstate);
            });

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

                ThreadPool.QueueUserWorkItem(unused =>
                {
                    CallStateChangedEvent?.Invoke(existCall);
                });
            } else {
				if (existCall.State != newstate) {
					existCall.SetCallState (newstate);
                    ThreadPool.QueueUserWorkItem(unused =>
                    {
                        CallStateChangedEvent?.Invoke(existCall);
                    });
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
                    ThreadPool.QueueUserWorkItem(unused =>
                    {
                        MessageReceivedEvent?.Invoke(Marshal.PtrToStringAnsi(addressStringPtr),
                        Marshal.PtrToStringAnsi(chatmessage));
                    });
                }
            }
        }

    }
}

