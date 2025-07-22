using System;
using System.Collections.Generic;
using System.Text;

namespace libomtnet
{
    internal class OMTConstants
    {
        public static int NETWORK_SEND_BUFFER = 65536;
        public static int NETWORK_SEND_RECEIVE_BUFFER = 65536;
        public static int NETWORK_RECEIVE_BUFFER = 10485760;

        public static int NETWORK_ASYNC_COUNT = 4;
        public static int NETWORK_ASYNC_BUFFER_AV = 1048576;
        public static int NETWORK_ASYNC_BUFFER_META = 65536;

        public static int VIDEO_FRAME_POOL_COUNT = 4;

        public static int VIDEO_MIN_SIZE = 65536;
        public static int VIDEO_MAX_SIZE = 10485760;

        public static int AUDIO_FRAME_POOL_COUNT = 10;

        public static int AUDIO_MIN_SIZE = 65536;
        public static int AUDIO_MAX_SIZE = 1048576;

        public static int NETWORK_PORT_START = 6400;
        public static int NETWORK_PORT_END = 6600;

        public static int AUDIO_SAMPLE_SIZE = 4;
        public static int METADATA_MAX_COUNT = 60;

        public static string URL_PREFIX = "omt://";
    }
}
