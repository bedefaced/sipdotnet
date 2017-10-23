sipdotnet
=========

Small .NET wrapper for [liblinphone](http://www.linphone.org/eng/documentation/dev/liblinphone-free-sip-voip-sdk.html) library. You can use it for simple interface for SIP telephony.

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
 - Utilities:
	 - `SendDTMFs` (sending DTMF-tones)
	 - `SetIncomingRingSound` and `SetRingbackSound` (sound that is heard when it's ringing to remote party)

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
* last available (4.1.1) liblinphone library binaries installed

Liblinphone on Windows
----------------------

Due to backwardness of [SDK binaries](http://www.linphone.org/technical-corner/liblinphone/downloads) version it's _recommended_ to use dlls from [Linphone desktop build](http://www.linphone.org/technical-corner/linphone/downloads). You can use my zipped collection in [lib](https://github.com/bedefaced/sipdotnet/blob/master/lib) directory or collect necessary dlls yourself using such tools as [Dependency Walker](http://www.dependencywalker.com/) against 'linphone.dll'.

Liblinphone on Linux
--------------------
1) Install [Mono](http://www.mono-project.com/download/#download-lin)
2) Build manually from [sources](https://github.com/BelledonneCommunications/linphone), or use [my bash script](https://gist.github.com/bedefaced/3dc4e58c700dada43054f49a3053dcad) for Ubuntu 16.04.

License
-------
[LGPLv3](http://en.wikipedia.org/wiki/GNU_Lesser_General_Public_License) (see `LICENSE` file)
