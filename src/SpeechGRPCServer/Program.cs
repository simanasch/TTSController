using System;
using System.IO;
using System.Collections.Generic;
using Speech;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Helloworld;
using Ttscontroller;
using System.Runtime.InteropServices;
using System.Threading;

namespace SpeechGrpcServer
{

    class GreeterImpl : Greeter.GreeterBase
    {
        // Server side handler of the SayHello RPC
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
        }

        // Server side handler for the SayHelloAgain RPC
        public override Task<HelloReply> SayHelloAgain(HelloRequest request, ServerCallContext context)
        {
            Program.OneShotPlayMode("葵", request.Name);
            return Task.FromResult(new HelloReply { Message = "Hello again " + request.Name });
        }
    }

    class TTSControllerImpl : TTSService.TTSServiceBase
    {
        public override Task<SpeechEngineList> getSpeechEngineDetail(SpeechEngineRequest request, ServerCallContext context)
        {
            return Task.FromResult(GetLibraryList());
        }

        public override Task<ttsResult> record(ttsRequest request, ServerCallContext context)
        {
            return RecordTask(request.LibraryName, request.EngineName, request.Body, request.OutputPath);
        }

        public override Task<ttsResult> talk(ttsRequest request, ServerCallContext context)
        {
            return TalkTask(request.LibraryName, request.EngineName, request.Body, request.OutputPath);
        }

        private static SpeechEngineList GetLibraryList()
        {
            var results = new SpeechEngineList();
            var engines = SpeechController.GetAllSpeechEngine();
            foreach (SpeechEngineInfo engineInfo in engines)
            {
                results.DetailItem.Add(new SpeechEngineList.Types.SpeechEngineDetail
                {
                    EngineName = engineInfo.EngineName,
                    LibraryName = engineInfo.LibraryName,
                    EnginePath = String.IsNullOrWhiteSpace(engineInfo.EnginePath) ? "" : engineInfo.EnginePath,
                    Is64BitProcess = engineInfo.Is64BitProcess
                });
                Console.WriteLine(engineInfo);
            }
            return results;
        }

        private static Task<ttsResult> TalkTask(String libraryName, String  engineName, String body, String outputPath)
        {
            Console.WriteLine("talk called,Library Name:" + libraryName + "\nengine:" + engineName + "\nbody:" + body);
            // engine.finishedイベントが呼ばれてから結果を返すようにするためTaskCompletionSourceを使う
            var tcs = new TaskCompletionSource<ttsResult>();

            ISpeechController engine = getInstance(libraryName, engineName);
            if (engine == null)
            {
                Console.WriteLine($"{libraryName} を起動できませんでした。");
                return Task.FromResult(new ttsResult
                {
                    IsSuccess = false,
                    OutputPath = ""
                });
            }

            engine.Activate();
            engine.Finished += (s, a) =>
            {
                engine.Dispose();
                Console.WriteLine("talk completed");
                tcs.TrySetResult(new ttsResult
                {
                    IsSuccess = true,
                    OutputPath = outputPath
                });
            };
            engine.Play(body);
            return tcs.Task;
        }

        private static Task<ttsResult> RecordTask(String libraryName, String engineName, String body, String outputPath)
        {
            Console.WriteLine("Record called,Library Name:" + libraryName + "\nengine:" + engineName + "\nbody:" + body);
            // engine.finishedイベントが呼ばれてから結果を返すようにするためTaskCompletionSourceを使う
            var tcs = new TaskCompletionSource<ttsResult>();

            SoundRecorder recorder = new SoundRecorder(outputPath);
            recorder.PostWait = 300;

            ISpeechController engine = getInstance(libraryName, engineName);
            if (engine == null)
            {
                Console.WriteLine($"{libraryName} を起動できませんでした。");
                return Task.FromResult(new ttsResult
                {
                    IsSuccess = false
                });
            }
            engine.Activate();
            engine.Finished += (s, a) =>
            {
                Task t = recorder.Stop();
                t.Wait();
                engine.Dispose();
                Console.WriteLine("record completed");
                tcs.TrySetResult(new ttsResult
                {
                    IsSuccess = true,
                    LibraryName = libraryName,
                    EngineName = engineName,
                    Body = body,
                    OutputPath = outputPath
                });
            };
            // recorderの起動後に音声を再生する
            recorder.Start();
            engine.Play(body);
            return tcs.Task;
        }

        private static ISpeechController getInstance(String libraryName, String engineName)
        {
            if (String.IsNullOrWhiteSpace(engineName))
            {
                return  SpeechController.GetInstance(libraryName);
            }
            else
            {
                return  SpeechController.GetInstance(libraryName, engineName);
            }

        }
    }
    class Program
    {
        const int Port = 5001;
        static Server server = null;

        static void Main(string[] args)
        {
            server = new Server
            {
                Services = {
                    Greeter.BindService(new GreeterImpl()),
                    TTSService.BindService(new TTSControllerImpl())
                },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("localhost:" + Port + "で接続待機中");
            while (true)
            {
                Thread.Sleep(500);
            }

        }



        public static Boolean OneShotPlayMode(string libraryName, string text)
        {

            var engine = SpeechController.GetInstance(libraryName);
            if (engine == null)
            {
                Console.WriteLine($"{libraryName} を起動できませんでした。");
                return false;
            }
            engine.Activate();
            engine.Finished += (s, a) =>
            {
                engine.Dispose();

            };
            engine.Play(text);
            return true;

        }
    }
}
