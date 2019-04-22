using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sipdotnet
{
    public abstract class Codec
    {
        protected string name;
        protected int clockrate;
        protected bool enabled;

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public int ClockRate
        {
            get
            {
                return this.clockrate;
            }
        }

        public virtual bool Enabled { get; set; }

        public Codec (string name, int clockrate, bool enabled)
        {
            this.name = name;
            this.clockrate = clockrate;
            this.enabled = enabled;
        }
        
        public override string ToString ()
        {
            return String.Format("{0} ({1} kbit/s)", this.name, this.clockrate / 1000);
        }
    }

    public class AudioCodec : Codec
    {
        public AudioCodec (string name, int clockrate, bool enabled) : base(name, clockrate, enabled)
        {
        }
    }
}
