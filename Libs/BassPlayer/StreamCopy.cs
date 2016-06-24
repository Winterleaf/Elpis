#region Copyright (C) 2005-2010 Team MediaPortal

// Copyright (C) 2005-2010 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

namespace BassPlayer
{
    internal class StreamCopy : Un4seen.Bass.Misc.BaseDSP
    {
        public StreamCopy() {}

        public StreamCopy(int channel, int priority) : base(channel, priority, System.IntPtr.Zero) {}

        public int Stream { get; private set; }

        public Un4seen.Bass.BASSFlag StreamFlags { get; set; }

        private Un4seen.Bass.BASSBuffer _streamBuffer;

        public override void OnChannelChanged()
        {
            OnStopped();
            if (IsAssigned)
            {
                OnStarted();
            }
        }

        public override void OnStarted()
        {
            int channelBitwidth = ChannelBitwidth;
            switch (channelBitwidth)
            {
                case 0x20:
                    StreamFlags &= ~Un4seen.Bass.BASSFlag.BASS_SAMPLE_8BITS;
                    StreamFlags |= Un4seen.Bass.BASSFlag.BASS_SAMPLE_FLOAT;
                    channelBitwidth = 4;
                    break;

                case 8:
                    StreamFlags &= ~Un4seen.Bass.BASSFlag.BASS_SAMPLE_FLOAT;
                    StreamFlags |= Un4seen.Bass.BASSFlag.BASS_SAMPLE_8BITS;
                    channelBitwidth = 1;
                    break;

                default:
                    StreamFlags &= ~Un4seen.Bass.BASSFlag.BASS_SAMPLE_FLOAT;
                    StreamFlags &= ~Un4seen.Bass.BASSFlag.BASS_SAMPLE_8BITS;
                    channelBitwidth = 2;
                    break;
            }
            _streamBuffer = new Un4seen.Bass.BASSBuffer(2f, ChannelSampleRate, ChannelNumChans, channelBitwidth);
            Stream = Un4seen.Bass.Bass.BASS_StreamCreate(ChannelSampleRate, ChannelNumChans, StreamFlags, null,
                System.IntPtr.Zero);
            Un4seen.Bass.Bass.BASS_ChannelSetLink(ChannelHandle, Stream);
            if (Un4seen.Bass.Bass.BASS_ChannelIsActive(ChannelHandle) == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING)
            {
                Un4seen.Bass.Bass.BASS_ChannelPlay(Stream, false);
            }
        }

        public override void OnStopped()
        {
            Un4seen.Bass.Bass.BASS_ChannelRemoveLink(ChannelHandle, Stream);
            Un4seen.Bass.Bass.BASS_StreamFree(Stream);
            Stream = 0;
            ClearBuffer();
        }

        public void ClearBuffer()
        {
            _streamBuffer?.Clear();
        }

        public override void DSPCallback(int handle, int channel, System.IntPtr buffer, int length, System.IntPtr user)
        {
            try
            {
                _streamBuffer.Write(buffer, length);
            }
            catch (System.Exception ex)
            {
                Log.Error("Caught Exception in DSPCallBack. {0}", ex.Message);
            }
        }

        public override string ToString()
        {
            return "StreamCopy";
        }

        // Properties
    }
}