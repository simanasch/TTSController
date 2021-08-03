using System;
using System.IO;
using System.IO.Pipes;
using Speech;
using System.Linq;
using System.Text;

namespace SpeechPipeServer
{
    class Program
    {
        private const string PipeName = "testpipe";
        //static bool finished = false;
        static NamedPipeServerStream pipeServer;
        static void Main(string[] args)
        {
            pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1 );
            Console.WriteLine("サーバーを立ち上げた");
            // Wait for a client to connect
            pipeServer.WaitForConnection();
            try
            {
                //byte[] message = Encoding.ASCII.GetBytes("START\n");
                //pipeServer.BeginWrite(message, 0, message.Length, WriteCallback, null);
                Console.WriteLine("接続された");
                using (StreamReader sr = new StreamReader(pipeServer))
                {
                    string line;
                    // Read and display lines from the file until the end of
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                        OneShotPlayMode("葵", line);
                    }
                }
                Console.WriteLine("接続終わった");

                // Read モードの場合にやりたい処理
                //using (StreamWriter sw = new StreamWriter(pipeServer))
                //{
                //    sw.AutoFlush = true;
                //    //Console.Write("Enter text: ");
                //    //sw.WriteLine(Console.ReadLine());
                //    string bit = Environment.Is64BitProcess ? "64 bit" : "32 bit";
                //    Console.WriteLine($"※ このアプリケーションは {bit}プロセスのため、{bit}のライブラリのみが列挙されます。");
                //    Console.WriteLine("-----");
                //    var names = GetLibraryName();
                //    foreach (var s in names)
                //    {
                //        Console.WriteLine(s);
                //        sw.WriteLine(s);
                //    }
                //}
            }
            catch (IOException e)
            {
                Console.WriteLine("ERROR: {0}", e.Message);
            }
            pipeServer.Close();
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
    }
}
