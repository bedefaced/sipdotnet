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

		public CallType Type 
		{
			get
            {
                return this.calltype;
            }
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

		public CallState State
		{
            get
            {
                return this.callstate;
            }
		}

		protected string from;

		public string From
		{
            get
            {
                return this.from;
            }
		}

		protected string to;

		public string To
		{
            get
            {
                return this.to;
            }
		}

        protected string recordfile;

        public string Recordfile
        {
            get
            {
                return this.recordfile;
            }
        }
    }
}

