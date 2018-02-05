using System;
using System.Diagnostics;

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
        /// Phone is ringing
        /// </summary>
        /// <param name="call"></param>
		public delegate void OnIncomingCall (Call call);

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
		public event OnCallActive CallActiveEvent;
		public event OnCallCompleted CallCompletedEvent;
		public event OnError ErrorEvent;

        private event OnLog logEventHandler;
        public event OnLog LogEvent
        {
            add
            {
                logEventHandler += value;
                if (!linphone.LogsEnabled) {
                    linphone.LogsEnabled = true;
                    linphone.LogEvent += (message) =>
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
                    linphone.LogsEnabled = false;
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

		Linphone linphone;

		public Phone (Account account)
		{
			Debug.Assert (null != account, "Phone requires an Account to make calls.");
			this.account = account;
			linphone = new Linphone ();
			linphone.RegistrationStateChangedEvent += (Linphone.LinphoneRegistrationState state) => {
				switch (state) {
					case Linphone.LinphoneRegistrationState.LinphoneRegistrationProgress:
						connectState = ConnectState.Progress;
						break;

					case Linphone.LinphoneRegistrationState.LinphoneRegistrationFailed:
                        linphone.DestroyPhone();
                        ErrorEvent?.Invoke(null, Error.RegisterFailed);
                        break;

					case Linphone.LinphoneRegistrationState.LinphoneRegistrationCleared:
						connectState = ConnectState.Disconnected;
                        PhoneDisconnectedEvent?.Invoke();
                        break;

					case Linphone.LinphoneRegistrationState.LinphoneRegistrationOk:
						connectState = ConnectState.Connected;
                        PhoneConnectedEvent?.Invoke();
                        break;

					case Linphone.LinphoneRegistrationState.LinphoneRegistrationNone:
					default:
						break;
				}
			};

			linphone.ErrorEvent += (call, message) => {
				Console.WriteLine ("Error: {0}", message);
                ErrorEvent?.Invoke(call, Error.UnknownError);
            };

			linphone.CallStateChangedEvent += (Call call) => {
				Call.CallState state = call.GetState();

				switch (state) {
				case Call.CallState.Active:
					lineState = LineState.Busy;
                        CallActiveEvent?.Invoke(call);
                        break;

				case Call.CallState.Loading:
					lineState = LineState.Busy;
					if (call.GetCallType () == Call.CallType.Incoming)
                            IncomingCallEvent?.Invoke(call);
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
		}

		public void Connect ()
		{
            if (connectState == ConnectState.Disconnected)
            {
                connectState = ConnectState.Progress;
				linphone.CreatePhone(account.Username, Account.Password, Account.Server, Account.Port, Useragent, Version);
            }
            else
                ErrorEvent?.Invoke(null, Error.OrderError);
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

    }
}

