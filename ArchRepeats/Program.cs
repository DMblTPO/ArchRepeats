using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ArchRepeats
{
    class Program
    {
        private const int OkResult = 0;
        private const int BadResult = -1;

        static int Main(string[] args)
        {
            var dir = @".\";
            if (args.Length > 0)
            {
                dir = args[0];
            }

            if (!Directory.Exists(dir))
            {
                Console.WriteLine($"Specified dir '{dir}' does not exist.");
                return BadResult;
            }

            var dirArch = $@"{dir}\arc\";
            if (args.Length > 1)
            {
                dirArch = args[1];
            }

            if (!Directory.Exists(dirArch))
            {
                try
                {
                    Directory.CreateDirectory(dirArch);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return BadResult;
                }
            }

            var hashDir = new ConcurrentDictionary<string, ConcurrentStack<string>>();
            var files = Directory.GetFiles(dir);

            Parallel.ForEach(files,
                (item, res) =>
                {
                    using (var md5 = MD5.Create())
                    using (var stream = File.OpenRead(item))
                    {
                        var hash = md5.ComputeHash(stream);
                        var fileHash = string.Concat(hash.Select(x => x.ToString("X2")));

                        if (hashDir.ContainsKey(fileHash))
                        {
                            hashDir[fileHash].Push(item);
                            return;
                        }

                        var stack = new ConcurrentStack<string>();
                        stack.Push(item);
                        hashDir.TryAdd(fileHash, stack);
                    }
                });

            Console.WriteLine("Hashes:");
            foreach (var fHash in hashDir)
            {
                Console.WriteLine($"  {fHash.Key}");
                foreach (var file in fHash.Value)
                {
                    Console.WriteLine($"\t- {file}");
                }
            }

            var filesToArch = new ConcurrentStack<string>(hashDir.Values.SelectMany(x => x.Skip(1)));

            Console.WriteLine("File moving:");
            Parallel.ForEach(filesToArch,
                sourceFileName =>
                {
                    var slashPos = sourceFileName.LastIndexOf(@"\", StringComparison.Ordinal);
                    var fileName = sourceFileName.Substring(slashPos + 1);
                    var destFileName = dirArch + fileName;
                    try
                    {
                        File.Move(sourceFileName, destFileName);
                        Console.WriteLine($"from: {sourceFileName}\n\t" +
                                          $"to: {destFileName}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Can not move file '{sourceFileName}', because of '{e.Message}'");
                    }
                });

            Console.ReadKey();

            return OkResult;
        }
    }
}
