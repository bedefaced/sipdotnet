using System;
using System.Collections.Generic;
#if (DEBUG)
using System.Diagnostics;
#endif

namespace sipdotnet
{
	public class Phone
	{
        public enum ConnectState 
		{
			Disconnected, // idle
			Progress, // registering on server
			Connected // successfull registered
		};
		ConnectState connectState;

		public ConnectState CurrentConnectState {
			get {
				return connectState;
			}
		}

		public enum LineState
		{
			Free, // no active calls
			Busy // any call
		};
		LineState lineState;

		public LineState CurrentLineState {
			get {
				return lineState;
			}
		}

		public enum Error
		{
            /// <summary>
            /// Registration error
            /// </summary>
			RegisterFailed,

            /// <summary>
            /// Trying to make/receive call while another call is active
            /// </summary>
			LineIsBusyError,

            /// <summary>
            /// Trying to connect while connected / connecting or disconnect when not connected
            /// </summary>
			OrderError,

            /// <summary>
            /// Call failed
            /// </summary>
            CallError,
			UnknownError
		};

        /// <summary>
        /// Successful registered
        /// </summary>
		public delegate void OnPhoneConnected ();

        /// <summary>
        /// Successful unregistered
        /// </summary>
		public delegate void OnPhoneDisconnected ();

        /// <summary>
        /// Incoming call occurs
        /// </summary>
        /// <param name="call"></param>
		public delegate void OnIncomingCall (Call call);

        /// <summary>
        /// Outgoing call occurs
        /// </summary>
        /// <param name="call"></param>
        public delegate void OnOutgoingCall (Call call);

        /// <summary>
        /// Link is established
        /// </summary>
        /// <param name="call"></param>
		public delegate void OnCallActive (Call call);

        /// <summary>
        /// Call completed
        /// </summary>
        /// <param name="call"></param>
		public delegate void OnCallCompleted (Call call);

        /// <summary>
        /// Message received
        /// </summary>
        /// <param name="call"></param>
        public delegate void OnMessageReceived (string from, string message);

        /// <summary>
        /// Error notification
        /// </summary>
        /// <param name="call"></param>
        /// <param name="error"></param>
		public delegate void OnError (Call call, Error error);

        /// <summary>
        /// Raw log notification
        /// </summary>
        /// <param name="message"></param>
        public delegate void OnLog(string message);

        public event OnPhoneConnected PhoneConnectedEvent;
		public event OnPhoneDisconnected PhoneDisconnectedEvent;
		public event OnIncomingCall IncomingCallEvent;
        public event OnOutgoingCall OutgoingCallEvent;
        public event OnCallActive CallActiveEvent;
		public event OnCallCompleted CallCompletedEvent;
        public event OnMessageReceived MessageReceivedEvent;
        public event OnError ErrorEvent;

        private static event OnLog logEventHandler;
        public static event OnLog LogEvent
        {
            add
            {
                logEventHandler += value;
                if (!LinphoneWrapper.LogsEnabled) {
                    LinphoneWrapper.LogsEnabled = true;
                    LinphoneWrapper.LogEvent += (message) =>
                    {
                        logEventHandler?.Invoke(message);
                    };
                }
            }

            remove
            {
                logEventHandler -= value;
                if (logEventHandler == null)
                {
                    LinphoneWrapper.LogsEnabled = false;
                }
            }
        }

        Account account;

		public Account Account {
			get {
				return account;
			}
		}

		string useragent = "liblinphone";

		public string Useragent {
			get {
				return useragent;
			}
			set {
				useragent = value;
			}
		}

		string version = "6.0.0";

		public string Version {
			get {
				return version;
			}
			set {
				version = value;
			}
		}

        int expires = 3600;

        public int Expires
        {
            get
            {
                return expires;
            }
            set
            {
                expires = value;
            }
        }

        string configFile;

        LinphoneWrapper linphone;
        List<AudioCodec> codeclist;

        void Initialize()
        {
            linphone = new LinphoneWrapper();
            linphone.RegistrationStateChangedEvent += (LinphoneRegistrationState state) => {
                switch (state)
                {
                    case LinphoneRegistrationState.LinphoneRegistrationProgress:
                        connectState = ConnectState.Progress;
                        break;

                    case LinphoneRegistrationState.LinphoneRegistrationFailed:
                        linphone.DestroyPhone();
                        ErrorEvent?.Invoke(null, Error.RegisterFailed);
                        break;

                    case LinphoneRegistrationState.LinphoneRegistrationCleared:
                        connectState = ConnectState.Disconnected;
                        PhoneDisconnectedEvent?.Invoke();
                        break;

                    case LinphoneRegistrationState.LinphoneRegistrationOk:
                        connectState = ConnectState.Connected;
                        PhoneConnectedEvent?.Invoke();
                        break;

                    case LinphoneRegistrationState.LinphoneRegistrationNone:
                    default:
                        break;
                }
            };

            linphone.ErrorEvent += (call, message) => {
                Console.WriteLine("Error: {0}", message);
                ErrorEvent?.Invoke(call, Error.UnknownError);
            };

            linphone.CallStateChangedEvent += (Call call) => {
                Call.CallState state = call.State;

                switch (state)
                {
                    case Call.CallState.Active:
                        lineState = LineState.Busy;
                        CallActiveEvent?.Invoke(call);
                        break;

                    case Call.CallState.Loading:
                        lineState = LineState.Busy;
                        if (call.Type == Call.CallType.Incoming)
                            IncomingCallEvent?.Invoke(call);
                        if (call.Type == Call.CallType.Outcoming)
                            OutgoingCallEvent?.Invoke(call);
                        break;

                    case Call.CallState.Error:
                        this.lineState = LineState.Free;
                        ErrorEvent?.Invoke(null, Error.CallError);
                        break;

                    case Call.CallState.Completed:
                    default:
                        this.lineState = LineState.Free;
                        CallCompletedEvent?.Invoke(call);
                        break;
                }
            };

            linphone.MessageReceivedEvent += (string from, string message) =>
            {
                MessageReceivedEvent?.Invoke(from, message);
            };
        }

        public Phone (Account account)
		{
#if (DEBUG)
            Debug.Assert (null != account, "Phone requires an Account to make calls.");
#endif
            this.account = account;
            Initialize();
        }

        public Phone (string configFile)
        {
#if (DEBUG)
            Debug.Assert(null != configFile || !System.IO.File.Exists(configFile), "Phone requires an existing config file.");
#endif
            this.configFile = configFile;
            Initialize();
        }

        public void Connect(NatPolicy natPolicy)
        {
            if (connectState == ConnectState.Disconnected)
            {
                connectState = ConnectState.Progress;
                if (configFile != null)
                {
                    linphone.CreatePhone(null, null, null, 0, null, null,false, false, false, false, null, 0, configFile);
                } else
                {
                    linphone.CreatePhone(account.Username, Account.Password, Account.Server, Account.Port, Useragent, Version,
                        natPolicy.UseSTUN, natPolicy.UseTURN, natPolicy.UseICE, natPolicy.UseUPNP, natPolicy.Server, Expires);
                }
            }
            else
                ErrorEvent?.Invoke(null, Error.OrderError);
        }

        public void Connect ()
		{
            Connect(NatPolicy.GetDefaultNatPolicy());
        }

		public void Disconnect ()
		{
			if (connectState == ConnectState.Connected)
				linphone.DestroyPhone ();
			else
                ErrorEvent?.Invoke(null, Error.OrderError);
        }

		public void MakeCall (string sipUriOrPhone)
		{
			if (string.IsNullOrEmpty(sipUriOrPhone))
				throw new ArgumentNullException ("sipUriOrPhone");

            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");

            if (lineState == LineState.Free)
				linphone.MakeCall (sipUriOrPhone);
			else {
                ErrorEvent?.Invoke(null, Error.LineIsBusyError);
            }
		}

        public void MakeCallManualRecord (string sipUriOrPhone, string filename)
        {
            if (string.IsNullOrEmpty(sipUriOrPhone))
                throw new ArgumentNullException("sipUriOrPhone");

            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");

            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");

            if (lineState == LineState.Free)
                linphone.MakeCallAndRecord(sipUriOrPhone, filename, false);
            else
            {
                ErrorEvent?.Invoke(null, Error.LineIsBusyError);
            }
        }

        public void MakeCallAndRecord (string sipUriOrPhone, string filename)
		{
			if (string.IsNullOrEmpty(sipUriOrPhone))
				throw new ArgumentNullException ("sipUriOrPhone");

			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException ("filename");

            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");

            if (lineState == LineState.Free)
				linphone.MakeCallAndRecord (sipUriOrPhone, filename);
			else {
                ErrorEvent?.Invoke(null, Error.LineIsBusyError);
            }
		}

        public void ReceiveCallManualRecord(Call call, string filename)
        {
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
                throw new ArgumentNullException("call");
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");

            linphone.ReceiveCallAndRecord(call, filename, false);
        }

        public void ReceiveCallAndRecord (Call call, string filename)
		{
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
				throw new ArgumentNullException ("call");
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException ("filename");

			linphone.ReceiveCallAndRecord (call, filename);
		}

        public void SendMessage (string to, string message)
        {
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (string.IsNullOrEmpty(to))
                throw new ArgumentNullException("to");
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("message");

            linphone.SendMessage(to, message);
        }

        public void StartRecording (Call call)
        {
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
                throw new ArgumentNullException("call");
            linphone.StartRecording (call);
        }

        public void PauseRecording (Call call)
        {
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
                throw new ArgumentNullException("call");
            linphone.PauseRecording (call);
        }

		public void ReceiveCall (Call call)
		{
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
				throw new ArgumentNullException ("call");

			linphone.ReceiveCall (call);
		}

		public void TerminateCall (Call call)
		{
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
				throw new ArgumentNullException ("call");

			linphone.TerminateCall (call);
		}

        public void PauseCall (Call call)
        {
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
                throw new ArgumentNullException("call");

            linphone.PauseCall(call);
        }

        public void ResumeCall (Call call)
        {
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
                throw new ArgumentNullException("call");

            linphone.ResumeCall(call);
        }

        public void RedirectCall (Call call, string redirectURI)
        {
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
                throw new ArgumentNullException("call");

            linphone.RedirectCall(call, redirectURI);
        }

        public void TransferCall (Call call, string redirectURI)
        {
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
                throw new ArgumentNullException("call");

            linphone.TransferCall(call, redirectURI);
        }

        public void SendDTMFs (Call call, string dtmfs)
        {
            if (connectState != ConnectState.Connected)
                throw new InvalidOperationException("not connected");
            if (call == null)
                throw new ArgumentNullException("call");
            if (string.IsNullOrEmpty(dtmfs))
                throw new ArgumentNullException("dtmfs");

            linphone.SendDTMFs (call, dtmfs);
        }

        public void SetIncomingRingSound (string filename)
        {
            if (linphone == null)
                throw new InvalidOperationException("not connected");

            linphone.SetIncomingRingSound (filename);
        }

        public void SetRingbackSound (string filename)
        {
            if (linphone == null)
                throw new InvalidOperationException("not connected");

            linphone.SetRingbackSound (filename);
        }

        public List<string> PlaybackDevices()
        {
            if (linphone == null)
                throw new InvalidOperationException("phone not connected");

            return linphone.GetPlaybackDevices();
        }

        public List<string> CaptureDevices ()
        {
            if (linphone == null)
                throw new InvalidOperationException("phone not connected");

            return linphone.GetCaptureDevices();
        }

        public bool MicrophoneEnabled
        {
            get
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                return linphone.MicrophoneEnabled;
            }

            set
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                linphone.MicrophoneEnabled = value;
            }
        }

        public bool KeepAliveEnabled
        {
            get
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                return linphone.KeepAliveEnabled;
            }

            set
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                linphone.KeepAliveEnabled = value;
            }
        }

        /// <summary>
        /// Value is saved and used for subsequent calls. This actually controls software echo cancellation.
        /// If hardware echo cancellation is available, it will be always used and activated for calls, regardless of the value passed to this function. 
        /// </summary>
        public bool EchoCancellationEnabled
        {
            get
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                return linphone.EchoCancellationEnabled;
            }

            set
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                linphone.EchoCancellationEnabled = value;
            }
        }

        public string RingerDevice
        {
            get
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                return linphone.RingerDevice;
            }

            set
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                linphone.RingerDevice = value;
            }
        }

        public string PlaybackDevice
        {
            get
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                return linphone.PlaybackDevice;
            }

            set
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                linphone.PlaybackDevice = value;
            }
        }

        public string CaptureDevice
        {
            get
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                return linphone.CaptureDevice;
            }

            set
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                linphone.CaptureDevice = value;
            }
        }

        public List<AudioCodec> AvailableAudioCodecs
        {
            get
            {
                if (linphone == null)
                    throw new InvalidOperationException("phone not connected");

                if (codeclist == null)
                {
                    codeclist = linphone.GetAudioCodecs();
                }

                return codeclist;
            }
            
        }
    }
}

