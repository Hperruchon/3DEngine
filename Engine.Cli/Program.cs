using System.Text;

namespace Engine.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        // Finding E17 of the codebase review of 2026-09-30 (R-0028, TASK-0321): the program writes UTF-8, so
        // a name that is not ASCII reaches a script and a console unchanged. Until TASK-0321 the program wrote
        // in the code page of the console, and a reader of UTF-8 got other characters for "Größe-Ω". A process
        // with no console can refuse the change; the program then keeps the default encoding and continues.
        // The program gives the console its earlier encoding back at the end, because on Windows the code page of the
        // console stays after the process.
        var previous = Console.OutputEncoding;
        try
        {
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }
        catch (IOException)
        {
        }

        try
        {
            return Cli.Run(args, Console.Out, Console.Error);
        }
        finally
        {
            try
            {
                Console.OutputEncoding = previous;
            }
            catch (IOException)
            {
            }
        }
    }
}
