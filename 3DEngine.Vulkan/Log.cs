// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See THIRD-PARTY-NOTICES.md, section 4.
// This file comes from the Vortice.Vulkan sample framework. TASK-0027 moved it
// into this project and changed it. ADR-0017 gives the rules for this project.

using System; 
namespace ThreeDEngine.Vulkan;

internal static class Log
{
    public static void Info(string message)
    {
        WriteColored(ConsoleColor.Green, "[INFO]");
        Console.WriteLine(" " + message);
    }

    public static void Warn(string message)
    {
        WriteColored(ConsoleColor.Yellow, "[WARN]");
        Console.WriteLine(" " + message);
    }

    public static void Error(string message)
    {
        WriteColored(ConsoleColor.Red, "[ERROR]");
        Console.WriteLine(" " + message);
    }

    private static void WriteColored(ConsoleColor color, string message)
    {
        ConsoleColor currentColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(message);
        Console.ForegroundColor = currentColor;
    }
}
