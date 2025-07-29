# Open Media Transport (OMT) Protocol 1.0

## Introduction

This guide should serve as an overview of the protocol used by Open Media Transport.

Implementers should first look at using libomtnet or libomt as these are complete implementations covering the vast majority of platforms.

## Basics

Open Media Transport consists of three major components:

1. TCP Protocol for sending/receiving video, audio and metadata.
2. Special Metadata commands to control various aspects of the connection.
2. DNS-SD for discovery (RFC 6763).

## Data Types

All data types are stored in Little-Endian byte order.
This is in contrast to the often used Big-Endian network order.

## TCP Protocol

All data is encapsulated into a frame consisting of the following parts:

### HEADER (12 bytes)

BYTE Version // Must be 1

BYTE FrameType // Metadata = 1, Video = 2, Audio = 4

INT64 Timestamp // Timestamp where 1 second = 10,000,000

UINT16 MetadataLength // Length of XML UTF-8 per-frame metadata including null character.

INT32 DataLength //ExtendedHeader + Data length + MetadataLength, excluding this header

### VIDEO EXTENDED HEADER (32 bytes) (Mandatory for video frame type)

INT32 Codec //Video codec FourCC

INT32 Width //Video width in pixels

INT32 Height //Video height in pixels

INT32 FrameRateN //Frame rate numerator/denominator in frames per second, for example 60/1 is 60 frames per second.

INT32 FrameRateD 

FLOAT32 AspectRatio //Display aspect ratio expressed as a ratio of width/height. For example 1.777777777777778 for 16/9

INT32 Flags

INT32 ColorSpace //Color space flag. 601 for BT601, 709 for BT709, 0 for undefined (typically BT601 for SD, BT709 for HD)

### AUDIO EXTENDED HEADER (24 bytes) (Mandatory for audio frame type)

INT32 Codec //Audio codec FourCC, currently only 'FPA1' is supported which is 32bit floating point planar audio

INT32 SampleRate //Audio sample rate

INT32 SamplesPerChannel //Number of samples per channel stored in this frame

INT32 Channels //Number of channels of audio

UINT32 ActiveChannels //Bit field denoting the number of actual channels stored in this frames data out of the total Channels specified. This is so silent channels can be skipped, saving bandwidth.

INT32 Reserved1 //Reserved for future use

### DATA

The frame data followed by the per-frame metadata

### Latency considerations

To optimize latency and prevent network stalls, implementers should ensure the receiver never blocks when accepting data.
To achieve this one approach involves keeping a small frame queue with at least one frame reserved for the asynchronous network callback.
That reserved frame can the be reused repeatedly if the separate decode thread is taking too long to process frames.

## Metadata

Metadata is stored as UTF-8 encoded, null terminated XML data.

DataLength should always include the null character.

Special metadata commands are used to control various aspects of the connection.
These are fixed strings that must be specified exactly.
This is an optimization so that the end point can employ simple string matching rather than full XML parsing.

### Subscribe Commands

A sender should not send any data until a subscribe command is received.

\<OMTSubscribe Video="true" /\>

Sent by a receiver to request the sender start sending video frames.

\<OMTSubscribe Audio="true" /\>

Sent by a receiver to request the sender start sending audio frames.

\<OMTSubscribe Metadata="true" /\>

### Preview Commands

\<OMTSettings Preview="true" /\>
\<OMTSettings Preview="false" /\>

Enable/disable sending preview video data instead of the full resolution frame.

### Tally Commands

\<OMTTally Preview="true" Program="false" /\>
\<OMTTally Preview="false" Program="true" /\>
\<OMTTally Preview="true" Program="true" /\>
\<OMTTally Preview="false" Program="false" /\>

Sent by a receiver to indicate tally status.

The sender should then combine this tally status with those set by other receivers and then broadcast this new combined tally to all receivers.

This tally should also be sent to any new connections as well.

### Suggested Quality

\<OMTSettings Quality="Default" /\>

Sent by receivers to indicate the preferred compression quality:

Default,
Low,
Medium,
High

Senders may respond to this by gathering the preferred quality of all receivers, determining the highest quality requested and then adjusting the encoder to match.

### Sender Information

\<OMTInfo ProductName="MyProduct" Manufacturer="MyCompany" Version="1.0" /\>

Senders can optionally send information about the encoder to receivers when connected.

## DNS-SD

Open Media Transport uses the service type _omt._tcp
The port should be the tcp port that sender is listening on
The full service name should take the form HOSTNAME (Source Name)._omt._tcp.local



