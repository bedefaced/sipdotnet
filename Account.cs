using System;
using System.Diagnostics;

namespace sipdotnet
{
	public class Account
	{
		string username, password, server;
		int port = 5060, id;

		public string Username {
			get {
				return username;
			}
			set {
				username = value;
			}
		}

		public string Password {
			get {
				return password;
			}
			set {
				password = value;
			}
		}

		public string Server {
			get {
				return server;
			}
			set {
				server = value;
			}
		}

		public int Id {
			get {
				return id;
			}
			set {
				id = value;
			}
		}

		public int Port {
			get {
				return port;
			}
			set {
				port = value;
			}
		}

		public string Identity
		{
			get { return "sip:" + this.username + "@" + this.server; }
		}

        public Account(string username, string password, string server, int port)
		{
#if (DEBUG)
            Debug.Assert(!String.IsNullOrEmpty(username), "User cannot be empty.");
            Debug.Assert(!String.IsNullOrEmpty(server), "Server cannot be empty.");
#endif

            this.username = username;
            this.password = password;
            this.server = server;
            this.port = port;
		}

        public Account(string username, string password, string server) : this(username, password, server, 5060) { }
	}
}

