using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sipdotnet
{
    public class NatPolicy
    {
        bool use_stun, use_turn, use_ice, use_upnp;
        string server;

        public NatPolicy(bool use_stun, bool use_turn, bool use_ice, bool use_upnp, string server)
        {
            this.UseSTUN = use_stun;
            this.UseTURN = use_turn;
            this.UseICE = use_ice;
            this.UseUPNP = use_upnp;
            this.Server = server;
        }

        public static NatPolicy GetDefaultNatPolicy()
        {
            return new NatPolicy(false, false, false, false, string.Empty);
        }

        public bool UseSTUN { get => use_stun; set => use_stun = value; }
        public bool UseTURN { get => use_turn; set => use_turn = value; }
        public bool UseICE { get => use_ice; set => use_ice = value; }
        public bool UseUPNP { get => use_upnp; set => use_upnp = value; }
        public string Server { get => server; set => server = value; }
    }
}
