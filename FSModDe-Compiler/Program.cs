using System;
using System.IO;
using Yabber;
using SoulsFormats;
using SoulsFormats.AC4;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FSModDe_Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                ShowUsageAndTerminate();
                return;
            }
            string path = args[1];
            if (!Directory.Exists(path))
            {
                Console.WriteLine($"{path} is not a valid mod directory.");
                ShowUsageAndTerminate();
            }

            string operation = args[0].ToLower();
            switch (operation)
            {
                case "-c":
                    CompileMod(path);
                    break;
                case "-d":
                    DecompileMod(path);
                    break;
                default:
                    Console.WriteLine($"{operation} is not a valid operation type.\n");
                    ShowUsageAndTerminate();
                    break;
            }
        }

        private static void CompileMod(string path)
        {
            var directories = new List<string>(Directory.GetDirectories(path, "*", SearchOption.AllDirectories));
            directories.Reverse();
            Parallel.ForEach(directories, directoryPath => { if (!RepackDir(directoryPath, new Progress<float>())) Directory.Delete(directoryPath, true); });
         }

        private static void DecompileMod(string path)
        {
            var modFolderFilesPathes = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            //foreach (var filePath in modFolderFilesPathes)
            //{
            //    //string filePathExtension = Path.GetExtension(filePath).TrimStart('.');
            //    //switch (filePathExtension)
            //    //{
            //    //    case ".dcx":
            //    //        var a = BND4.Read(filePath);
            //    //        a.
            //    //        break;
            //    //    default:
            //    //        break;
            //    //}
                
            //}
            Parallel.ForEach(modFolderFilesPathes, filePath => { if(!UnpackFile(filePath, new Progress<float>())) File.Delete(filePath); });
        }

        private static bool RepackDir(string sourceDir, IProgress<float> progress)
        {
            string sourceName = new DirectoryInfo(sourceDir).Name;
            string targetDir = new DirectoryInfo(sourceDir).Parent.FullName;
            if (File.Exists($"{sourceDir}\\_yabber-bnd3.xml"))
            {
                Console.WriteLine($"Repacking BND3: {sourceName}...");
                YBND3.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_yabber-bnd4.xml"))
            {
                Console.WriteLine($"Repacking BND4: {sourceName}...");
                YBND4.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_yabber-bxf3.xml"))
            {
                Console.WriteLine($"Repacking BXF3: {sourceName}...");
                YBXF3.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_yabber-bxf4.xml"))
            {
                Console.WriteLine($"Repacking BXF4: {sourceName}...");
                YBXF4.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_yabber-tpf.xml"))
            {
                Console.WriteLine($"Repacking TPF: {sourceName}...");
                YTPF.Repack(sourceDir, targetDir);
            }
            else
            {
                Console.WriteLine($"Yabber XML not found in: {sourceName}");
                return true;
            }
            return false;
        }

        private static bool UnpackFile(string sourceFile, IProgress<float> progress)
        {
            string sourceDir = Path.GetDirectoryName(sourceFile);
            string filename = Path.GetFileName(sourceFile);
            string targetDir = $"{sourceDir}\\{filename.Replace('.', '-')}";
            if (File.Exists(targetDir))
                targetDir += "-ybr";

            if (DCX.Is(sourceFile))
            {
                Console.WriteLine($"Decompressing DCX: {filename}...");
                byte[] bytes = DCX.Decompress(sourceFile, out DCX.Type compression);
                if (BND3.Is(bytes))
                {
                    Console.WriteLine($"Unpacking BND3: {filename}...");
                    using (var bnd = new BND3Reader(bytes))
                    {
                        bnd.Compression = compression;
                        bnd.Unpack(filename, targetDir, progress);
                    }
                }
                else if (BND4.Is(bytes))
                {
                    Console.WriteLine($"Unpacking BND4: {filename}...");
                    using (var bnd = new BND4Reader(bytes))
                    {
                        bnd.Compression = compression;
                        bnd.Unpack(filename, targetDir, progress);
                    }
                }
                else if (FFXDLSE.Is(bytes))
                {
                    Console.WriteLine($"Unpacking FFX: {filename}...");
                    var ffx = FFXDLSE.Read(bytes);
                    ffx.Compression = compression;
                    ffx.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".fmg.dcx"))
                {
                    Console.WriteLine($"Unpacking FMG: {filename}...");
                    FMG fmg = FMG.Read(bytes);
                    fmg.Compression = compression;
                    fmg.Unpack(sourceFile);
                }
                else if (GPARAM.Is(bytes))
                {
                    Console.WriteLine($"Unpacking GPARAM: {filename}...");
                    GPARAM gparam = GPARAM.Read(bytes);
                    gparam.Compression = compression;
                    gparam.Unpack(sourceFile);
                }
                else if (TPF.Is(bytes))
                {
                    Console.WriteLine($"Unpacking TPF: {filename}...");
                    TPF tpf = TPF.Read(bytes);
                    tpf.Compression = compression;
                    tpf.Unpack(filename, targetDir, progress);
                }
                else
                {
                    Console.WriteLine($"File format not recognized: {filename}");
                    return true;
                }
            }
            else
            {
                if (BND3.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking BND3: {filename}...");
                    using (var bnd = new BND3Reader(sourceFile))
                    {
                        bnd.Unpack(filename, targetDir, progress);
                    }
                }
                else if (BND4.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking BND4: {filename}...");
                    using (var bnd = new BND4Reader(sourceFile))
                    {
                        bnd.Unpack(filename, targetDir, progress);
                    }
                }
                else if (BXF3.IsBHD(sourceFile))
                {
                    string bdtExtension = Path.GetExtension(filename).Replace("bhd", "bdt");
                    string bdtFilename = $"{Path.GetFileNameWithoutExtension(filename)}{bdtExtension}";
                    string bdtPath = $"{sourceDir}\\{bdtFilename}";
                    if (File.Exists(bdtPath))
                    {
                        Console.WriteLine($"Unpacking BXF3: {filename}...");
                        using (var bxf = new BXF3Reader(sourceFile, bdtPath))
                        {
                            bxf.Unpack(filename, bdtFilename, targetDir, progress);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"BDT not found for BHD: {filename}");
                        return true;
                    }
                }
                else if (BXF4.IsBHD(sourceFile))
                {
                    string bdtExtension = Path.GetExtension(filename).Replace("bhd", "bdt");
                    string bdtFilename = $"{Path.GetFileNameWithoutExtension(filename)}{bdtExtension}";
                    string bdtPath = $"{sourceDir}\\{bdtFilename}";
                    if (File.Exists(bdtPath))
                    {
                        Console.WriteLine($"Unpacking BXF4: {filename}...");
                        using (var bxf = new BXF4Reader(sourceFile, bdtPath))
                        {
                            bxf.Unpack(filename, bdtFilename, targetDir, progress);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"BDT not found for BHD: {filename}");
                        return true;
                    }
                }
                else if (FFXDLSE.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking FFX: {filename}...");
                    var ffx = FFXDLSE.Read(sourceFile);
                    ffx.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".ffx.xml") || sourceFile.EndsWith(".ffx.dcx.xml"))
                {
                    Console.WriteLine($"Repacking FFX: {filename}...");
                    YFFX.Repack(sourceFile);
                }
                else if (sourceFile.EndsWith(".fmg"))
                {
                    Console.WriteLine($"Unpacking FMG: {filename}...");
                    FMG fmg = FMG.Read(sourceFile);
                    fmg.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".fmg.xml") || sourceFile.EndsWith(".fmg.dcx.xml"))
                {
                    Console.WriteLine($"Repacking FMG: {filename}...");
                    YFMG.Repack(sourceFile);
                }
                else if (GPARAM.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking GPARAM: {filename}...");
                    GPARAM gparam = GPARAM.Read(sourceFile);
                    gparam.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".gparam.xml") || sourceFile.EndsWith(".gparam.dcx.xml")
                    || sourceFile.EndsWith(".fltparam.xml") || sourceFile.EndsWith(".fltparam.dcx.xml"))
                {
                    Console.WriteLine($"Repacking GPARAM: {filename}...");
                    YGPARAM.Repack(sourceFile);
                }
                else if (sourceFile.EndsWith(".luagnl"))
                {
                    Console.WriteLine($"Unpacking LUAGNL: {filename}...");
                    LUAGNL gnl = LUAGNL.Read(sourceFile);
                    gnl.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".luagnl.xml"))
                {
                    Console.WriteLine($"Repacking LUAGNL: {filename}...");
                    YLUAGNL.Repack(sourceFile);
                }
                else if (LUAINFO.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking LUAINFO: {filename}...");
                    LUAINFO info = LUAINFO.Read(sourceFile);
                    info.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".luainfo.xml"))
                {
                    Console.WriteLine($"Repacking LUAINFO: {filename}...");
                    YLUAINFO.Repack(sourceFile);
                }
                else if (TPF.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking TPF: {filename}...");
                    TPF tpf = TPF.Read(sourceFile);
                    tpf.Unpack(filename, targetDir, progress);
                }
                else if (Zero3.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking 000: {filename}...");
                    Zero3 z3 = Zero3.Read(sourceFile);
                    z3.Unpack(targetDir);
                }
                else
                {
                    Console.WriteLine($"File format not recognized: {filename}");
                    return true;
                }
            }
            return false;
        }

        private static void ShowUsageAndTerminate()
        {
            Console.WriteLine("Usage: FSModDe_Compiler.exe <operationType> <mod folder path>");
            Console.WriteLine("allowed operation types: compile = -c, decompile = -d");
            System.Environment.Exit(22);
        }
    }
}