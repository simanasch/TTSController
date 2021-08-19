﻿using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.Compression;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Speech
{
    public class SoundRecorder : IDisposable
    {
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int WM_APPCOMMAND = 0x319;

        public bool is16bit { get; set; } = true;
        bool _finished = false;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public IntPtr GetHandle()
        {
            var handle = Process.GetCurrentProcess().MainWindowHandle;
            if(handle == IntPtr.Zero)
            {
                handle = GetConsoleWindow();
            }
            return handle;
        }

        private void Mute()
        {
            var handle = GetHandle();
            SendMessageW(handle, WM_APPCOMMAND, handle, (IntPtr)APPCOMMAND_VOLUME_MUTE);
        }
        private void Unmute()
        {
            var handle = GetHandle();
            SendMessageW(handle, WM_APPCOMMAND, handle, (IntPtr)APPCOMMAND_VOLUME_UP);
            SendMessageW(handle, WM_APPCOMMAND, handle, (IntPtr)APPCOMMAND_VOLUME_DOWN);
        }

        private WaveFileWriter _writer = null;
        private WasapiLoopbackCapture _capture = null;
        private bool disposedValue;

        /// <summary>
        /// 音声合成の完了後に何ミリ秒待ってから録音を終了するか
        /// </summary>
        public UInt32 PostWait { get; set; } = 0;

        /// <summary>
        /// Start()が呼び出されてから何ミリ秒待ってから録音を開始するか
        /// </summary>
        public UInt32 PreWait { get; set; } = 0;

        /// <summary>
        /// 出力先のファイル名を取得または設定します
        /// </summary>
        public string OutputPath { get; set; }
        public SoundRecorder(string filename)
        {
            OutputPath = filename;
        }
        public async Task Start()
        {
            String dirName = Path.GetDirectoryName(OutputPath);
            String FolderName = Path.GetFileName(OutputPath);
            String rawOutputPath = is16bit ? Path.GetDirectoryName(OutputPath) + @"/orig_" + Path.GetFileName(OutputPath) : OutputPath;
            if(!File.Exists(OutputPath))
            {
                Directory.CreateDirectory(dirName);
            }
            _finished = false;
            _capture = new WasapiLoopbackCapture();
            _writer = new WaveFileWriter(rawOutputPath, _capture.WaveFormat);

            _capture.DataAvailable += (s, a) =>
            {
                _writer.Write(a.Buffer, 0, a.BytesRecorded);

            };
            _capture.RecordingStopped += (s, a) =>
            {
                _writer.Flush();
                _writer.Close();
                _writer.Dispose();
                if(is16bit)
                {
                    resampleTo16bit(rawOutputPath, OutputPath);
                }
                _finished = true;
            };
            await Task.Delay((int)PreWait);
            Mute();
            _capture.StartRecording();
        }

        public async Task Stop()
        {
            if(_capture != null)
            {
                await Task.Delay((int)PostWait);
                _capture.StopRecording();
                _capture.Dispose();
                while (!_finished)
                {
                    Thread.Sleep(100);
                }
                Unmute();
            }
        }

        private void resampleTo16bit(String rawOutputPath, String OutputPath)
        {
            using (var reader = new WaveFileReader(rawOutputPath))
            {
                var convertWaveFormat = new WaveFormat(48000, 16, 2);
                var wavProvider = new WaveFloatTo16Provider(reader);
                wavProvider.Volume = 1.3f;
                using (var resampler = new MediaFoundationResampler(wavProvider, convertWaveFormat))
                {
                    WaveFileWriter.CreateWaveFile(OutputPath, resampler);
                }

            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Task t = Stop();
                    t.Wait();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~SoundRecorder()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
