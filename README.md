# TrafficForward
An application that listens on a given port and forwards traffic to the given remote host and ip.

###Usage###
 - TrafficForward.exe <listenPort> <forwardHost> <forwardPort> [<listenIP>]
  - listenPort is the local port to listen on [required]
  - forwardHost is the remote host to forward TCP traffic to [required]
  - forwardPort is the remote port to forward TCP traffic to [required]
  - listenIP is the IP to listen on, in the case you don't want to bind to all IPs, default is 0.0.0.0 [optional]
