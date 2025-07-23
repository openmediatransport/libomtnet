# Open Media Transport (OMT) Libary for .NET

libomtnet is a .NET library that implements the Open Media Transport protocol for low latency, high performance Local Area Network
video/audio transmission.

It is built using a basic subset of .NET and as a result supports both .NET Framework 4+ and .NET Standard 2.0+ applications, covering all .NET versions from 4 onwards.

libomt is a native compiled version of the .NET library and is available separately.

## Getting Started

### Installation

Official binary releases for Windows and MacOS can be found in the Releases section of this repository.

There are only two dependencies when using this library in a .NET app:

**libomtnet.dll**
This is a cross platform file that will work on Windows, Mac and Linux

**libvmx.dll (Windows)**
**libvmx.dylib (MacOS)**
**libvmx.so (Linux)**

These are platform specific native shared libraries. The correct library for the CPU type and OS platform needs to be placed in the same directory as the application.

### Creating a Source

1. Create an instance of the OMTSend class specifying a name
2. Fill the struct OMTMediaFrame with the video data in either of the available YUV or RGBx formats
3. Send using OMTSend.Send
4. That's it, the source is now available on the network for receivers to connect to

### Creating a Receiver

1. Create an instance of the OMTReceive class specifying the full name of the source (including machine name)
The full name of all sources on the network can be found by using the OMTDiscovery class
2. In a loop, poll OMTReceive.Receive specifying the types of frames to receive and also a timeout
3. Process said frames as required

