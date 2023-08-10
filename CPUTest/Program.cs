using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emulator;
using Emulator.Machines;

namespace CPUTest
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("(c) 2023 Joscha Egloff");
            Console.WriteLine("Emulator Testing Program");


            // Create a new 6502 CPU
            var cpu = new CPU6502();

            cpu.Memory!.Write(0xA9);
            cpu.Memory!.Write(0x12);

            cpu.State.PC -= 2;

            cpu.Startup();

            cpu.Shutdown();

            Console.WriteLine($"Cycles: {cpu.State.Cycles}");

        }
    }
}
