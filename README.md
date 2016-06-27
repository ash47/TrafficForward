# TrafficForward
An application that listens on a given port and forwards traffic to the given remote host and ip.

###Usage###
 - TrafficForward.exe &lt;listenPort&gt; &lt;forwardHost&gt; &lt;forwardPort&gt; [&lt;listenIP&gt;]
  - listenPort is the local port to listen on [required]
  - forwardHost is the remote host to forward TCP traffic to [required]
  - forwardPort is the remote port to forward TCP traffic to [required]
  - listenIP is the IP to listen on, in the case you don't want to bind to all IPs, default is 0.0.0.0 [optional]

###Executable Download###
 - An executable download can be found in the [Releases Section](https://github.com/ash47/TrafficForward/releases) of this repo, or it can be built from the source.
