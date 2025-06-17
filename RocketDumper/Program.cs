using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RocketDumper.Engine;

namespace RocketDumper
{
    public class RocketDumper
    {
        public static string NamesPath = "GNames.txt";
        public static string ObjectsPath = "GObjects.txt";

        public static IntPtr BaseAddress;
        public static IntPtr GNamesAddress;
        public static IntPtr GObjectsAddress;
        public static IntPtr GNamesOffset;
        public static IntPtr GObjectsOffset;

        public static TArray GNames;
        public static TArray GObjects;

        public static MemoryHelper RL = new();

        private static void Main()
        {
            // 🚀 Open Discord invite link in browser
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://discord.gg/XEpNz5XFtx",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not open Discord link: " + ex.Message);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Title = "KingMod's Rocket League Dumper";
            Console.CursorVisible = false;

            File.Delete(NamesPath);
            File.Delete(ObjectsPath);

            BaseAddress = RL.BaseAddress;
            GNamesAddress = RL.FindPattern("?? ?? ?? ?? ?? ?? 00 00 ?? ?? 01 00 35 25 02 00");
            GObjectsAddress = GNamesAddress + 0x48;
            GNamesOffset = (IntPtr)(GNamesAddress.ToInt64() - BaseAddress.ToInt64());
            GObjectsOffset = (IntPtr)(GObjectsAddress.ToInt64() - BaseAddress.ToInt64());

            // Display addresses in both hex and decimal
            Console.WriteLine($"Base-Address:\t\t0x{BaseAddress.ToInt64():X} ({BaseAddress.ToInt64()})\n");
            Console.WriteLine($"GNames-Address:\t\t0x{GNamesAddress.ToInt64():X} ({GNamesAddress.ToInt64()})");
            Console.WriteLine($"GNames-Offset:\t\t0x{GNamesOffset:X} ({GNamesOffset.ToInt64()})\n");
            Console.WriteLine($"GObjects-Address:\t0x{GObjectsAddress.ToInt64():X} ({GObjectsAddress.ToInt64()})");
            Console.WriteLine($"GObjects-Offset:\t0x{GObjectsOffset:X} ({GObjectsOffset.ToInt64()})\n");

            GNames = RL.ReadMemory<TArray>(GNamesAddress);
            GObjects = RL.ReadMemory<TArray>(GObjectsAddress);

            Console.WriteLine($"Dumping Names...\t[{GNames.ArrayCount}]");
            DumpNames();

            Console.WriteLine($"\n\nDumping Objects... \t[{GObjects.ArrayCount}]");
            DumpObjects();

            Console.WriteLine("\n\nDone!");
        }

        private static void DumpNames()
        {
            using StreamWriter sw = File.AppendText(NamesPath);
            // Display addresses in both hex and decimal
            sw.WriteLine($"Base-Address:\t\t0x{BaseAddress.ToInt64():X} ({BaseAddress.ToInt64()})\n");
            sw.WriteLine($"GNames-Address:\t\t0x{GNamesAddress.ToInt64():X} ({GNamesAddress.ToInt64()})");
            sw.WriteLine($"GNames-Offset:\t\t0x{GNamesOffset:X} ({GNamesOffset.ToInt64()})\n");
            sw.WriteLine($"GObjects-Address:\t0x{GObjectsAddress.ToInt64():X} ({GObjectsAddress.ToInt64()})");
            sw.WriteLine($"GObjects-Offset:\t0x{GObjectsOffset:X} ({GObjectsOffset.ToInt64()})\n");

            for (int index = 0; index < GNames.ArrayCount; index++)
            {
                ProgressBar(index, GNames.ArrayCount);

                var name = GetFName(index);

                if (!string.IsNullOrWhiteSpace(name))
                {
                    sw.WriteLine($"[{index.ToString().PadLeft(6, '0')}] {name}");
                }
            }
        }

        private static void DumpObjects()
        {
            using StreamWriter sw = File.AppendText(ObjectsPath);

            // Display addresses in both hex and decimal
            sw.WriteLine($"Base-Address:\t\t0x{BaseAddress.ToInt64():X} ({BaseAddress.ToInt64()})\n");
            sw.WriteLine($"GNames-Address:\t\t0x{GNamesAddress.ToInt64():X} ({GNamesAddress.ToInt64()})");
            sw.WriteLine($"GNames-Offset:\t\t0x{GNamesOffset:X} ({GNamesOffset.ToInt64()})\n");
            sw.WriteLine($"GObjects-Address:\t0x{GObjectsAddress.ToInt64():X} ({GObjectsAddress.ToInt64()})");
            sw.WriteLine($"GObjects-Offset:\t0x{GObjectsOffset:X} ({GObjectsOffset.ToInt64()})\n");

            for (int i = 0; i < GObjects.ArrayCount; i++)
            {
                ProgressBar(i, GObjects.ArrayCount);

                IntPtr uObjectPtr = RL.ReadMemory<IntPtr>(GObjects.ArrayData + (i * 8));

                if (uObjectPtr == IntPtr.Zero)
                    continue;

                UObject uObject = RL.ReadMemory<UObject>(uObjectPtr);
                string objectName = GetObjectName(uObject);

                if (!string.IsNullOrWhiteSpace(objectName))
                {
                    sw.WriteLine($"[0x{uObjectPtr.ToInt64():X} ({uObjectPtr.ToInt64()})] {objectName}");
                }
            }
        }

        public static string GetObjectName(UObject uObject)
        {
            string baseName = GetFName(uObject.FNameEntryId);
            string innerName = "";
            string outerName = "";

            if (uObject.Class != IntPtr.Zero)
            {
                UObject inner = RL.ReadMemory<UObject>(uObject.Class);
                innerName = GetFName(inner.FNameEntryId);
            }

            if (uObject.Outer != IntPtr.Zero)
            {
                UObject outer = RL.ReadMemory<UObject>(uObject.Outer);
                outerName = GetFName(outer.FNameEntryId);

                if (outer.Outer != IntPtr.Zero)
                {
                    UObject outerOuter = RL.ReadMemory<UObject>(outer.Outer);
                    var name = GetFName(outerOuter.FNameEntryId);

                    if (!string.IsNullOrWhiteSpace(name) && name != outerName)
                    {
                        outerName = $"{name}.{outerName}";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(outerName))
            {
                outerName += ".";
            }

            return $"{innerName} {outerName}{baseName}";
        }

        public static string GetFName(int index)
        {
            IntPtr FNamePtr = RL.ReadMemory<IntPtr>(GNames.ArrayData + (index * 8));

            if (FNamePtr == IntPtr.Zero)
                return string.Empty;

            FNameEntry entry = RL.ReadMemory<FNameEntry>(FNamePtr);

            if (string.IsNullOrWhiteSpace(entry.Name) || entry.Index != index)
                return string.Empty;

            return entry.Name;
        }

        private static void ProgressBar(int currentStep, int totalSteps)
        {
            float percentage = (float)currentStep / totalSteps;

            int filledWidth = 1 + (int)(percentage * 30);

            string progressBar = "[" + new string('=', filledWidth) + new string(' ', 30 - filledWidth) + "]";

            Console.Write("\r" + progressBar + $" {percentage:P0}");
        }
    }
}
