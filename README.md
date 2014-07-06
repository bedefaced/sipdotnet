sipdotnet
=========

.NET wrapper for [liblinphone][1] library. You can use it for simple interface for SIP telephony.

Using
-----

You can access to wrapped liblinphone actions and events by `Phone` class. Only that you need is the SIP account (`Account` class).

Current available functionality:

 - SIP-proxy connection control:
     - `Connect`
     - `Disconnect`
     - `Useragent` and `Version` definition
 - Call / Register / Error events:
     - `PhoneConnectedEvent`
     - `PhoneDisconnectedEvent`
     - `IncomingCallEvent`
     - `CallActiveEvent`
     - `CallCompletedEvent`
     - `ErrorEvent`
 - Make / receive / terminate / record calls:
     - `MakeCall`
     - `MakeCallAndRecord`
     - `ReceiveCall`
     - `ReceiveCallAndRecord`
     - `TerminateCall`

Example
-------
```cs
Account account = new Account ("username", "password", "server");
Phone phone = new Phone (account);
phone.PhoneConnectedEvent += delegate() {
	Console.WriteLine("Phone connected. Calling...");
	phone.MakeCallAndRecord("phonenumber", "/tmp/filename.wav");
};
phone.CallActiveEvent += delegate(Call call) {
	Console.WriteLine("Answered. Call is active!");
};
phone.CallCompletedEvent += delegate(Call call) {
	Console.WriteLine("Completed.");
};
phone.Connect (); // connecting
Console.ReadLine ();
Console.WriteLine("Disconnecting...");
phone.Disconnect (); // terminate all calls and disconnect
```
     
Requirements
------------

* .NET 4.0 framework on Windows or Linux (>= Mono 3.2.8)
* last available (>= 3.7.0) liblinphone library binaries installed

Liblinphone on Windows
----------------------
It can be hardly built from [sources][2] or be gotten from [linphone binaries][3]. On Windows it requires almost all dlls from `bin` directory of linphone. Complete pack of required liblinphone dlls can be downloaded from [here][4].

Liblinphone on Linux
--------------------
It is good if you build liblinphone from [sources][5] to have last version of liblinphone shared libraries. Note that this will require some components. For example, this is my "multi spell" for Ubuntu 14.04:

    apt-get install g++ git libtool automake autoconf libantlr3c-dev antlr3 make intltool speex libxml2-dev gtk+-2.0-dev libspeexdsp-dev && git clone git://git.linphone.org/linphone.git --recursive && git clone git://git.linphone.org/belle-sip.git && cd belle-sip && ./autogen.sh && ./configure && make && make install && cd ../linphone && ./autogen.sh && ./configure SPEEX_CFLAGS="-L/usr/lib/i386-linux-gnu -lspeex" SPEEX_LIBS="-L/usr/lib/i386-linux-gnu -lspeex" --without--ffmpeg --disable-video && make && make install

License
-------
[LGPLv3][6] (see `LICENSE` file)


  [1]: http://www.linphone.org/eng/documentation/dev/liblinphone-free-sip-voip-sdk.html
  [2]: http://www.linphone.org/eng/download/git.html
  [3]: http://www.linphone.org/eng/download/packages/
  [4]: https://mega.co.nz/#!4hIiFCIC!4RdkaxtRDMg-FisoT8L-4Asd7YEMuwFpfV_bZF0SX4c
  [5]: http://www.linphone.org/eng/download/git.html
  [6]: http://en.wikipedia.org/wiki/GNU_Lesser_General_Public_License
