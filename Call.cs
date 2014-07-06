using System;

namespace sipdotnet
{
	public class Call
	{
		public enum CallType 
		{
			None,
			Incoming,
			Outcoming
		};

		protected CallType calltype = CallType.None;

		public CallType GetCallType ()
		{
			return this.calltype;
		}

		public enum CallState
		{
			None,
			Loading,
			Active,
			Completed,
            Error
		};

		protected CallState callstate = CallState.None;

		public CallState GetState ()
		{
			return this.callstate;
		}

		protected string from;

		public string GetFrom ()
		{
			return this.from; 
		}

		protected string to;

		public string GetTo ()
		{
			return this.to; 
		}

	}
}

