using System;
using System.IO;
using Speech;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Helloworld;

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
            return Task.FromResult(new HelloReply { Message = "Hello again " + request.Name });
        }
    }
    class Program
    {
        const int Port = 30051;

        //private const string PipeName = "testpipe";
        //static bool finished = false;
        //static NamedPipeServerStream pipeServer;
        static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = {
                    Greeter.BindService(new GreeterImpl())
                },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("Greeter server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
            //pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 2 );
            //Console.WriteLine("サーバーを立ち上げた");
            //// Wait for a client to connect
            //pipeServer.WaitForConnection();
            //try
            //{
            //    Console.WriteLine("接続できた");
            //    // Stream for the request.
            //    StreamReader sr = new StreamReader(pipeServer);
            //    // Stream for the response.
            //    StreamWriter sw = new StreamWriter(pipeServer);
            //    sw.AutoFlush = true;
            //    string line;
            //    // Read and display lines from the file until the end of
            //    // the file is reached.
            //    while ((line = sr.ReadLine()) != null)
            //    {
            //        Console.WriteLine(line);
            //        OneShotPlayMode("葵", line);
            //        sw.WriteLine("以下を再生した: " + line);
            //    }

            //    // Read モードの場合にやりたい処理
            //    //using (StreamWriter sw = new StreamWriter(pipeServer))
            //    //{
            //    //    sw.AutoFlush = true;
            //    //    //Console.Write("Enter text: ");
            //    //    //sw.WriteLine(Console.ReadLine());
            //    //    string bit = Environment.Is64BitProcess ? "64 bit" : "32 bit";
            //    //    Console.WriteLine($"※ このアプリケーションは {bit}プロセスのため、{bit}のライブラリのみが列挙されます。");
            //    //    Console.WriteLine("-----");
            //    //    var names = GetLibraryName();
            //    //    foreach (var s in names)
            //    //    {
            //    //        Console.WriteLine(s);
            //    //        sw.WriteLine(s);
            //    //    }
            //    //}
            //}
            //catch (IOException e)
            //{
            //    Console.WriteLine("ERROR: {0}", e.Message);
            //}
            //pipeServer.Close();
        }


        private static string[] GetLibraryName()
        {
            var engines = SpeechController.GetAllSpeechEngine();
            var names = from c in engines
                        select c.LibraryName;
            return names.ToArray();
        }
        private static void OneShotPlayMode(string libraryName, string text)
        {

            //var engines = SpeechController.GetAllSpeechEngine();
            var engine = SpeechController.GetInstance(libraryName);
            if (engine == null)
            {
                Console.WriteLine($"{libraryName} を起動できませんでした。");
                return;
            }
            engine.Activate();
            engine.Finished += (s, a) =>
            {
                //finished = true;
                engine.Dispose();
            };
            engine.Play(text);

        }

        // Defines the data protocol for reading and writing strings on our stream
        public class StreamString
        {
            private Stream ioStream;
            private UnicodeEncoding streamEncoding;

            public StreamString(Stream ioStream)
            {
                this.ioStream = ioStream;
                streamEncoding = new UnicodeEncoding();
            }

            public string ReadString()
            {
                int len = 0;

                len = ioStream.ReadByte() * 256;
                len += ioStream.ReadByte();
                byte[] inBuffer = new byte[len];
                ioStream.Read(inBuffer, 0, len);

                return streamEncoding.GetString(inBuffer);
            }

            public int WriteString(string outString)
            {
                byte[] outBuffer = streamEncoding.GetBytes(outString);
                int len = outBuffer.Length;
                if (len > UInt16.MaxValue)
                {
                    len = (int)UInt16.MaxValue;
                }
                ioStream.WriteByte((byte)(len / 256));
                ioStream.WriteByte((byte)(len & 255));
                ioStream.Write(outBuffer, 0, len);
                ioStream.Flush();

                return outBuffer.Length + 2;
            }
        }
    }
}
