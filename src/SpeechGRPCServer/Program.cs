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

        public override Task<ttsResult> talk(ttsRequest request, ServerCallContext context)
        {
            return TalkTask(request.EngineName, request.Body, request.OutputPath);
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
                //Console.WriteLine(engineInfo);
            }
            return results;
        }

        private static Task<ttsResult> TalkTask(String LibraryName, String body, String outputPath)
        {
            // engine.finishedイベントが呼ばれてから結果を返すようにするためTaskCompletionSourceを使う
            var tcs = new TaskCompletionSource<ttsResult>();

            SoundRecorder recorder = new SoundRecorder(outputPath);
            recorder.PostWait = 300;

            var engine = SpeechController.GetInstance(LibraryName);
            if (engine == null)
            {
                Console.WriteLine($"{LibraryName} を起動できませんでした。");
                return Task.FromResult(new ttsResult
                {
                    IsSuccess = false,
                    OutputPath = ""
                });
            }

            engine.Activate();
            engine.Finished += (s, a) =>
            {
                Task t = recorder.Stop();
                t.Wait();
                engine.Dispose();
                tcs.TrySetResult(new ttsResult
                {
                    IsSuccess = true,
                    OutputPath = outputPath
                });
            };
            // recorderの起動後に音声を再生する
            recorder.Start();
            engine.Play(body);
            return tcs.Task;
        }
    }
    class Program
    {
        const int Port = 30051;
        static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = {
                    Greeter.BindService(new GreeterImpl()),
                    TTSService.BindService(new TTSControllerImpl())
                },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("localhost:" + Port + "で接続待機中");
            Console.WriteLine("なにかキーを押すとサーバーを閉じます");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }



        public static Boolean OneShotPlayMode(string libraryName, string text)
        {

            //var engines = SpeechController.GetAllSpeechEngine();
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
