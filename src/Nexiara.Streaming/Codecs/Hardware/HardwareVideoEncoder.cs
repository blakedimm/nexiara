using System;
using System.Runtime.InteropServices;
using Nexiara.Abstractions;
using Nexiara.Streaming.Codecs.ColorSpace;

namespace Nexiara.Streaming.Codecs.Hardware
{
    public class HardwareVideoEncoder : IVideoEncoder
    {
        public event Action<byte[]>? OnEncodedPacket;
        public bool IsInitialized { get; private set; }

        private IMFTransform? _mft;
        private int _width;
        private int _height;
        private int _fps;
        private long _frameIndex;
        private byte[]? _nv12Buffer;

        // --- Win32 Media Foundation P/Invoke API ---
        [DllImport("mfplat.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int MFStartup(uint version, uint dwFlags = 0);

        [DllImport("mfplat.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int MFShutdown();

        [DllImport("mfplat.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int MFCreateMediaType(out IMFMediaType ppMFType);

        [DllImport("mfplat.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int MFCreateMemoryBuffer(int cbMaxLength, out IMFMediaBuffer ppBuffer);

        [DllImport("mfplat.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int MFCreateSample(out IMFSample ppSample);

        [DllImport("ole32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int CoCreateInstance(
            ref Guid rclsid,
            IntPtr pUnkOuter,
            uint dwClsContext,
            ref Guid riid,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        // --- GUIDs Windows Media Foundation (Без readonly, чтобы компилятор разрешал ref) ---
        private static Guid CLSID_CMSH264EncoderMFT = new("6ca50344-051a-4ded-9779-a433ce9caa22");
        private static Guid IID_IMFTransform = new("bf947266-02be-491a-a76f-20079140c9bc");

        private static Guid MF_MT_MAJOR_TYPE = new("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
        private static Guid MF_MT_SUBTYPE = new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        private static Guid MF_MT_AVG_BITRATE = new("20332624-fb0d-4d9e-bd0d-cbf6786c102e");
        private static Guid MF_MT_FRAME_RATE = new("c459a2e8-3d2c-4e44-b132-fee5156c7bb0");
        private static Guid MF_MT_FRAME_SIZE = new("1652c33d-d6b2-4012-b834-72030849a37d");
        private static Guid MF_MT_INTERLACE_MODE = new("e2724bb8-e676-4806-b4b2-a8d6efb44ccd");

        private static Guid MFVideoFormat_H264 = new("34363248-0000-0010-8000-00aa00389b71");
        private static Guid MFVideoFormat_NV12 = new("3231564e-0000-0010-8000-00aa00389b71");
        private static Guid MFMediaType_Video = new("73646976-0000-0010-8000-00aa00389b71");

        // --- COM Интерфейсы ---
        [ComImport, Guid("4400497f-a5d2-407a-9224-b184f5e31707"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMFMediaType
        {
            void GetItem(); void GetItemType(); void CompareItem(); void GetUINT32(); void GetUINT64(); void GetDouble(); void GetGUID(); void GetStringLength(); void GetString(); void GetAllocatedString(); void GetBlobSize(); void GetBlob(); void GetUnknown();
            [PreserveSig] int SetItem(ref Guid guidKey, IntPtr Value);
            [PreserveSig] int DeleteItem();
            [PreserveSig] int DeleteAllItems();
            [PreserveSig] int SetUINT32(ref Guid guidKey, uint unValue);
            [PreserveSig] int SetUINT64(ref Guid guidKey, ulong unValue);
            [PreserveSig] int SetDouble();
            [PreserveSig] int SetGUID(ref Guid guidKey, ref Guid guidValue);
        }

        [ComImport, Guid("04562002-42a2-4bcd-a36e-849302b4971d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMFMediaBuffer
        {
            [PreserveSig] int Lock(out IntPtr ppbBuffer, out int pcbMaxLength, out int pcbCurrentLength);
            [PreserveSig] int Unlock();
            [PreserveSig] int GetCurrentLength(out int pcbCurrentLength);
            [PreserveSig] int SetCurrentLength(int cbCurrentLength);
            [PreserveSig] int GetMaxLength(out int pcbMaxLength);
        }

        [ComImport, Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMFSample
        {
            void GetItem(); void GetItemType(); void CompareItem(); void GetUINT32(); void GetUINT64(); void GetDouble(); void GetGUID(); void GetStringLength(); void GetString(); void GetAllocatedString(); void GetBlobSize(); void GetBlob(); void GetUnknown(); void SetItem(); void DeleteItem(); void DeleteAllItems(); void SetUINT32(); void SetUINT64(); void SetDouble(); void SetGUID();
            [PreserveSig] int GetSampleFlags(out uint pdwSampleFlags);
            [PreserveSig] int SetSampleFlags(uint dwSampleFlags);
            [PreserveSig] int GetSampleTime(out long phnsSampleTime);
            [PreserveSig] int SetSampleTime(long hnsSampleTime);
            [PreserveSig] int GetSampleDuration(out long phnsSampleDuration);
            [PreserveSig] int SetSampleDuration(long hnsSampleDuration);
            [PreserveSig] int GetBufferCount(out int pdwBufferCount);
            [PreserveSig] int GetBufferByIndex(int dwIndex, out IMFMediaBuffer ppBuffer);
            [PreserveSig] int ConvertToContiguousBuffer(out IMFMediaBuffer ppBuffer);
            [PreserveSig] int AddBuffer(IMFMediaBuffer pBuffer);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MFT_OUTPUT_DATA_BUFFER
        {
            public uint dwStreamID;
            public IntPtr pSample;
            public uint dwStatus;
            public IntPtr pEvents;
        }

        [ComImport, Guid("bf947266-02be-491a-a76f-20079140c9bc"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMFTransform
        {
            [PreserveSig] int GetStreamLimits(out uint pdwInputMinimum, out uint pdwInputMaximum, out uint pdwOutputMinimum, out uint pdwOutputMaximum);
            [PreserveSig] int GetStreamCount(out uint pcInputStreams, out uint pcOutputStreams);
            [PreserveSig] int GetStreamIDs(uint dwInputIDSize, out uint pdwInputIDs, uint dwOutputIDSize, out uint pdwOutputIDs);
            [PreserveSig] int GetInputStreamInfo(uint dwInputStreamID, IntPtr pStreamInfo);
            [PreserveSig] int GetOutputStreamInfo(uint dwOutputStreamID, IntPtr pStreamInfo);
            [PreserveSig] int GetAttributes(out IntPtr pAttributes);
            [PreserveSig] int GetInputStreamAttributes(uint dwInputStreamID, out IntPtr pAttributes);
            [PreserveSig] int GetOutputStreamAttributes(uint dwOutputStreamID, out IntPtr pAttributes);
            [PreserveSig] int DeleteInputStream(uint dwStreamID);
            [PreserveSig] int AddInputStreams(uint cStreams, ref uint adwStreamIDs);
            [PreserveSig] int GetInputAvailableType(uint dwInputStreamID, uint dwTypeIndex, out IMFMediaType ppType);
            [PreserveSig] int GetOutputAvailableType(uint dwOutputStreamID, uint dwTypeIndex, out IMFMediaType ppType);
            [PreserveSig] int SetInputType(uint dwInputStreamID, IMFMediaType pType, uint dwFlags);
            [PreserveSig] int SetOutputType(uint dwOutputStreamID, IMFMediaType pType, uint dwFlags);
            [PreserveSig] int GetInputCurrentType(uint dwInputStreamID, out IMFMediaType ppType);
            [PreserveSig] int GetOutputCurrentType(uint dwOutputStreamID, out IMFMediaType ppType);
            [PreserveSig] int GetInputStatus(uint dwInputStreamID, out uint pdwFlags);
            [PreserveSig] int GetOutputStatus(out uint pdwFlags);
            [PreserveSig] int SetOutputBounds(long hnsLowerBound, long hnsUpperBound);
            [PreserveSig] int ProcessEvent(uint dwInputStreamID, IntPtr pEvent);
            [PreserveSig] int ProcessMessage(uint eMessage, IntPtr ulParam);
            [PreserveSig] int ProcessInput(uint dwInputStreamID, IMFSample pSample, uint dwFlags);
            [PreserveSig] int ProcessOutput(uint dwFlags, uint cOutputBufferCount, [In, Out] MFT_OUTPUT_DATA_BUFFER[] pOutputSamples, out uint pdwStatus);
        }

        public void Initialize(int width, int height, int targetFps = 60, int bitrateKbps = 6000)
        {
            _width = width;
            _height = height;
            _fps = targetFps;
            _nv12Buffer = new byte[(int)(width * height * 1.5)];

            MFStartup(0x00020010); // MF_VERSION

            int hr = CoCreateInstance(ref CLSID_CMSH264EncoderMFT, IntPtr.Zero, 1, ref IID_IMFTransform, out object obj);
            if (hr != 0 || obj == null) throw new Exception($"Не удалось создать MFT H.264 кодировщик (HR: {hr:X})");

            _mft = (IMFTransform)obj;

            // Выходной тип (H.264)
            MFCreateMediaType(out var outType);
            outType.SetGUID(ref MF_MT_MAJOR_TYPE, ref MFMediaType_Video);
            outType.SetGUID(ref MF_MT_SUBTYPE, ref MFVideoFormat_H264);
            outType.SetUINT32(ref MF_MT_AVG_BITRATE, (uint)(bitrateKbps * 1000));
            outType.SetUINT64(ref MF_MT_FRAME_RATE, PackLong((uint)_fps, 1u));
            outType.SetUINT64(ref MF_MT_FRAME_SIZE, PackLong((uint)_width, (uint)_height));
            outType.SetUINT32(ref MF_MT_INTERLACE_MODE, 2u);
            _mft.SetOutputType(0, outType, 0);

            // Входной тип (NV12)
            MFCreateMediaType(out var inType);
            inType.SetGUID(ref MF_MT_MAJOR_TYPE, ref MFMediaType_Video);
            inType.SetGUID(ref MF_MT_SUBTYPE, ref MFVideoFormat_NV12);
            inType.SetUINT64(ref MF_MT_FRAME_RATE, PackLong((uint)_fps, 1u));
            inType.SetUINT64(ref MF_MT_FRAME_SIZE, PackLong((uint)_width, (uint)_height));
            inType.SetUINT32(ref MF_MT_INTERLACE_MODE, 2u);
            _mft.SetInputType(0, inType, 0);

            // Сообщения старта
            _mft.ProcessMessage(0x00000000, IntPtr.Zero); // MFT_MESSAGE_COMMAND_FLUSH
            _mft.ProcessMessage(0x10000000, IntPtr.Zero); // MFT_MESSAGE_NOTIFY_BEGIN_STREAMING
            _mft.ProcessMessage(0x10000002, IntPtr.Zero); // MFT_MESSAGE_NOTIFY_START_OF_STREAM

            IsInitialized = true;
        }

        public void EncodeFrame(ReadOnlySpan<byte> pixelData, int width, int height, int stride)
        {
            if (!IsInitialized || _mft == null || _nv12Buffer == null) return;

            try
            {
                Nv12Converter.ConvertArgbToNv12(pixelData, _nv12Buffer, width, height, stride);

                MFCreateMemoryBuffer(_nv12Buffer.Length, out var inputBuffer);
                inputBuffer.Lock(out IntPtr ptr, out _, out _);
                Marshal.Copy(_nv12Buffer, 0, ptr, _nv12Buffer.Length);
                inputBuffer.Unlock();
                inputBuffer.SetCurrentLength(_nv12Buffer.Length);

                MFCreateSample(out var inputSample);
                inputSample.AddBuffer(inputBuffer);
                inputSample.SetSampleTime(_frameIndex * 10000000L / _fps);
                inputSample.SetSampleDuration(10000000L / _fps);
                _frameIndex++;

                _mft.ProcessInput(0, inputSample, 0);

                Marshal.ReleaseComObject(inputBuffer);
                Marshal.ReleaseComObject(inputSample);

                ReadEncoderOutput();
            }
            catch { }
        }

        private void ReadEncoderOutput()
        {
            while (true)
            {
                MFCreateSample(out var sample);
                MFCreateMemoryBuffer(_width * _height, out var outBuffer);
                sample.AddBuffer(outBuffer);

                MFT_OUTPUT_DATA_BUFFER[] outputBuffers = new MFT_OUTPUT_DATA_BUFFER[1];
                outputBuffers[0].pSample = Marshal.GetIUnknownForObject(sample);

                int hr = _mft!.ProcessOutput(0, 1, outputBuffers, out uint status);

                if (outputBuffers[0].pSample != IntPtr.Zero)
                    Marshal.Release(outputBuffers[0].pSample);

                if (hr != 0)
                {
                    Marshal.ReleaseComObject(outBuffer);
                    Marshal.ReleaseComObject(sample);
                    break;
                }

                sample.ConvertToContiguousBuffer(out var finalBuffer);
                finalBuffer.Lock(out IntPtr dataPtr, out _, out int currentLength);

                if (currentLength > 0)
                {
                    byte[] packet = new byte[currentLength];
                    Marshal.Copy(dataPtr, packet, 0, currentLength);
                    OnEncodedPacket?.Invoke(packet);
                }

                finalBuffer.Unlock();
                Marshal.ReleaseComObject(finalBuffer);
                Marshal.ReleaseComObject(outBuffer);
                Marshal.ReleaseComObject(sample);
            }
        }

        private ulong PackLong(uint left, uint right)
        {
            return ((ulong)left << 32) | right;
        }

        public void Dispose()
        {
            IsInitialized = false;
            if (_mft != null)
            {
                _mft.ProcessMessage(0x10000003, IntPtr.Zero); // MFT_MESSAGE_NOTIFY_END_OF_STREAM
                _mft.ProcessMessage(0x10000001, IntPtr.Zero); // MFT_MESSAGE_NOTIFY_END_STREAMING
                Marshal.ReleaseComObject(_mft);
                _mft = null;
            }
            MFShutdown();
        }
    }
}