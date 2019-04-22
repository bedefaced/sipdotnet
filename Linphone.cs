using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace sipdotnet
{
    /// <summary>
    /// Describes proxy registration states
    /// http://www.linphone.org/docs/liblinphone/group__proxies.html
    /// </summary>
    enum LinphoneRegistrationState
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

    class Linphone
    {

#if (WINDOWS)
        public const string LIBNAME = "linphone.dll";
#else
        public const string LIBNAME = "liblinphone";
#endif

        /// <summary>
        /// Disable a sip transport
        /// </summary>
        public const int LC_SIP_TRANSPORT_DISABLED = 0;

        /// <summary>
        /// Randomly chose a sip port for this transport
        /// </summary>
        public const int LC_SIP_TRANSPORT_RANDOM = -1;

        /// <summary>
        /// Don't create any server socket for this transport, ie don't bind on any port
        /// </summary>
        public const int LC_SIP_TRANSPORT_DONTBIND = -2;

        /// <summary>
        /// Wildcard value used to ignore rate in search
        /// </summary>
        public const int LINPHONE_FIND_PAYLOAD_IGNORE_RATE = -1;

        /// <summary>
        /// Wildcard value used to ignore channel in search
        /// </summary>
        public const int LINPHONE_FIND_PAYLOAD_IGNORE_CHANNELS = -1;

        /// <summary>
        /// Linphone core SIP transport ports
        /// http://www.linphone.org/docs/liblinphone/struct__LinphoneSipTransports.html
        /// </summary>
		public struct LCSipTransports
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

        public struct bctbx_list
        {
            public IntPtr next;
            public IntPtr prev;
            public IntPtr data;
        }

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
        public struct LinphoneNatPolicy
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
        public struct LinphoneCoreVTable
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
        public static extern IntPtr linphone_core_new ([In] IntPtr vtable, [In] string config_path, [In] string factory_config_path, [In, Out] IntPtr userdata);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_unref ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_iterate ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_set_log_level ([In, Out] OrtpLogLevel loglevel);

        [Obsolete]
        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_set_log_handler ([In, Out] IntPtr logfunc);

        #endregion

        #region Proxies

        // http://www.linphone.org/docs/liblinphone/group__proxies.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_create_proxy_config ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_proxy_config_set_identity ([In, Out] IntPtr obj, [In] string identity);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_proxy_config_set_server_addr ([In, Out] IntPtr obj, [In] string server_addr);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_proxy_config_enable_register ([In, Out] IntPtr obj, [In] bool val);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_core_add_proxy_config ([In, Out] IntPtr lc, [In, Out] IntPtr cfg);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_set_default_proxy_config ([In, Out] IntPtr lc, [In, Out] IntPtr config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        [System.Obsolete]
        public static extern int linphone_core_get_default_proxy ([In, Out] IntPtr lc, [In, Out] ref IntPtr config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool linphone_proxy_config_is_registered ([In] IntPtr config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_proxy_config_edit ([In, Out] IntPtr config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_proxy_config_done ([In, Out] IntPtr config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_proxy_config_set_nat_policy ([In, Out] IntPtr cfg, [In, Out] IntPtr policy);

        #endregion

        #region Network

        // http://www.linphone.org/docs/liblinphone/group__network__parameters.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_core_set_sip_transports ([In, Out] IntPtr lc, [In, Out] IntPtr tr_config);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_create_nat_policy ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_nat_policy_unref ([In, Out] IntPtr natpolicy);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_nat_policy_ref ([In, Out] IntPtr natpolicy);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_nat_policy_clear ([In, Out] IntPtr policy);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_nat_policy_enable_stun ([In, Out] IntPtr policy, [In] bool enable);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_nat_policy_enable_turn ([In, Out] IntPtr policy, [In] bool enable);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_nat_policy_enable_ice ([In, Out] IntPtr policy, [In] bool enable);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_nat_policy_enable_upnp ([In, Out] IntPtr policy, [In] bool enable);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_nat_policy_set_stun_server ([In, Out] IntPtr policy, [In] string stun_server);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_nat_policy_set_stun_server_username ([In, Out] IntPtr policy, [In] string username);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_nat_policy_resolve_stun_server ([In, Out] IntPtr policy);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool linphone_core_keep_alive_enabled ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_enable_keep_alive ([In, Out] IntPtr lc, [In] bool enable);

        #endregion

        #region SIP

        // http://www.linphone.org/docs/liblinphone/group__linphone__address.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        [System.Obsolete]
        public static extern void linphone_address_destroy ([In, Out] IntPtr u);

        #endregion

        #region Miscenalleous

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_set_user_agent ([In, Out] IntPtr lc, [In] string ua_name, [In] string version);

        #endregion

        #region Calls

        // http://www.linphone.org/docs/liblinphone/group__call__control.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_create_call_params ([In, Out] IntPtr lc, [In, Out] IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_call_params_enable_video ([In, Out] IntPtr lc, [In] bool enabled);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_call_params_enable_early_media_sending ([In, Out] IntPtr lc, [In] bool enabled);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_invite_with_params ([In, Out] IntPtr lc, [In] string url, [In] IntPtr callparams);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_call_get_params ([In, Out] IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_call_params_ref ([In, Out] IntPtr callparams);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_call_params_unref ([In, Out] IntPtr callparams);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_core_terminate_call ([In, Out] IntPtr lc, [In, Out] IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_call_ref ([In, Out] IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_call_unref ([In, Out] IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_core_terminate_all_calls ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_call_get_remote_address_as_string ([In] IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_address_as_string ([In] IntPtr address);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_address_as_string_uri_only ([In] IntPtr address);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_call_get_to_address ([In] IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_core_accept_call_with_params (IntPtr lc, IntPtr call, [In] IntPtr callparams);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_call_params_set_record_file ([In, Out] IntPtr callparams, [In] string filename);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_call_params_get_record_file ([In] IntPtr callparams);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_call_params_enable_audio ([In, Out] IntPtr callparams, [In] bool enabled);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_call_send_dtmfs ([In, Out] IntPtr call, [In] string dtmfs);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool linphone_core_echo_cancellation_enabled ([In] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_enable_echo_cancellation ([In, Out] IntPtr call, [In] bool enabled);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_core_pause_call ([In, Out] IntPtr lc, [In, Out] IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_core_resume_call ([In, Out] IntPtr lc, [In, Out] IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_call_redirect ([In, Out] IntPtr lc, [In, Out] IntPtr call, [In] string redirect_uri);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_call_transfer ([In, Out] IntPtr lc, [In, Out] IntPtr call, [In] string redirect_uri);

        #endregion

        #region Authentication

        // http://www.linphone.org/docs/liblinphone/group__authentication.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_add_auth_info ([In, Out] IntPtr lc, [In] IntPtr info);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_auth_info_new ([In] string username, [In] string userid, [In] string passwd, [In] string ha1, [In] string realm, [In] string domain);

        #endregion

        #region Calls miscenalleous

        // http://www.linphone.org/docs/liblinphone/group__call__misc.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_call_start_recording ([In, Out] IntPtr call);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_call_stop_recording ([In, Out] IntPtr call);

        #endregion

        #region Media

        // http://www.linphone.org/docs/liblinphone/group__media__parameters.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_set_play_file ([In, Out] IntPtr lc, [In] string file);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_set_record_file ([In, Out] IntPtr lc, [In] string file);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_set_ring ([In, Out] IntPtr lc, [In] string file);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_set_remote_ringback_tone ([In, Out] IntPtr lc, [In] string file);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_set_ringback ([In, Out] IntPtr lc, [In] string file);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_reload_sound_devices ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool linphone_core_sound_device_can_capture ([In, Out] IntPtr lc, [In] string device);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool linphone_core_sound_device_can_playback ([In, Out] IntPtr lc, [In] string device);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_get_ringer_device ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_get_playback_device ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_get_capture_device ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_core_set_ringer_device ([In, Out] IntPtr lc, [In] string devid);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_core_set_playback_device ([In, Out] IntPtr lc, [In] string devid);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_core_set_capture_device ([In, Out] IntPtr lc, [In] string devid);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_core_enable_mic ([In, Out] IntPtr lc, [In] bool enable);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool linphone_core_mic_enabled ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_get_sound_devices ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool linphone_payload_type_enabled ([In] IntPtr lpt);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_payload_type_enable ([In, Out] IntPtr lpt, [In] bool enabled);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_get_audio_payload_types ([In, Out] IntPtr lc);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_payload_type_get_mime_type ([In] IntPtr lpt);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int linphone_payload_type_get_clock_rate ([In] IntPtr lpt);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_get_payload_type ([In, Out] IntPtr lc, [In] string type, [In] int rate, [In] int channels);


        #endregion

        #region Chat
        // http://www.linphone.org/docs/liblinphone/group__chatroom.html

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_core_get_chat_room_from_uri ([In, Out] IntPtr lc, [In] string to);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void linphone_chat_room_send_chat_message ([In, Out] IntPtr chatroom, [In, Out] IntPtr chatmessage);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_chat_room_create_message ([In, Out] IntPtr chatroom, [In] string chatmessage);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_chat_room_get_peer_address ([In, Out] IntPtr chatroom);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr linphone_chat_message_get_text ([In] IntPtr chatmessage);

        #endregion

    }
}
