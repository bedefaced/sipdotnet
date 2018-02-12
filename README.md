sipdotnet
=========

Small .NET wrapper for [liblinphone](http://www.linphone.org/eng/documentation/dev/liblinphone-free-sip-voip-sdk.html) library. You can use it for simple interface for SIP telephony.

Using
-----

You can access to wrapped liblinphone actions and events by `Phone` class. Only that you need is the SIP account (`Account` class), [sipdotnet.dll](https://github.com/bedefaced/sipdotnet/blob/dev/lib/sipdotnet.zip) and linphone libraries (see *Requirements* section).

For using library from .NET code you need to copy linphone dlls ([liblinphone and dependencies](https://github.com/bedefaced/sipdotnet#requirements)) to directory with your EXE file (or use, for example, [Add item](https://msdn.microsoft.com/en-us/library/9f4t9t92(v=vs.100).aspx) dialog in Visual Studio (and select *Copy to Output*: `Copy if newer` or `Copy always` in `Properties` window of added dlls)) and then add `sipdotnet.dll` (or the whole project) as dependency to your solution.

Current available functionality:

 - SIP-proxy connection control:
     - `Connect`
     - `Disconnect`
     - `Useragent` and `Version` definition
	 - (experimental) `NatPolicy` - support for different firewall / NAT policy
 - Call / Register / Error events:
     - `PhoneConnectedEvent` - when phone connected to SIP-proxy
     - `PhoneDisconnectedEvent` - when phone disconnected from SIP-proxy
     - `IncomingCallEvent` - when incoming call appears
     - `CallActiveEvent` - when conversation started (resumed)
     - `CallCompletedEvent` - when call completed
     - `ErrorEvent` - when any error (sequence is broken, for example)
	 - `LogEvent` - when new Linphone raw log record appears
 - Make / receive / terminate / record calls:
     - `MakeCall` - call making
     - `MakeCallAndRecord` - for call making with conversation recording (to WAV file)
     - `ReceiveCall` - for call receiving
     - `ReceiveCallAndRecord` - for call receiving with conversation recording (to WAV file)
     - `TerminateCall` - for call termination
	 - (experimental) `RedirectCall`, `TransferCall` - for call redirecting and transferring (during active conversation)
	 - (experimental) `StartRecording`, `PauseRecording` - for manual call recording control during call
 - Utilities:
	 - `SendDTMFs` (sending DTMF-tones)
	 - `SetIncomingRingSound` and `SetRingbackSound` (sound that is heard when it's ringing to remote party)
	 - (experimental) `PlaybackDevices`, `CaptureDevices`, `MicrophoneEnabled`, `RingerDevice`, `PlaybackDevice`, `CaptureDevice` - viewing and controlling audio devices used by phone

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
phone.LogEvent += delegate (string message)
{
	Console.WriteLine("[DEBUG] " + message);
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
* x86 - project type (linphone dlls constraints)

Liblinphone on Windows
----------------------

Due to backwardness of [SDK binaries](http://www.linphone.org/technical-corner/liblinphone/downloads) version it's _recommended_ to use dlls from [Linphone desktop build](http://www.linphone.org/technical-corner/linphone/downloads). You can use my zipped collection in [lib](https://github.com/bedefaced/sipdotnet/blob/master/lib) directory or collect necessary dlls yourself using such tools as [Dependency Walker](http://www.dependencywalker.com/) against 'linphone.dll'.

Liblinphone on Linux
--------------------
1) Install [Mono](http://www.mono-project.com/download/#download-lin)
2) Build manually from [sources](https://github.com/BelledonneCommunications/linphone), or use [my bash script](https://gist.github.com/bedefaced/3dc4e58c700dada43054f49a3053dcad) for Ubuntu 16.04.

See also
--------------------
* [Liblinphone docs](http://www.linphone.org/technical-corner/liblinphone/documentation)
* [Liblinphone official wrapper](https://wiki.linphone.org/xwiki/wiki/public/view/Lib/Getting%20started/Xamarin/)

License
-------
[LGPLv3](http://en.wikipedia.org/wiki/GNU_Lesser_General_Public_License) (see `LICENSE` file)
