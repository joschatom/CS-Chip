using System.Collections;
using System.Collections.Immutable;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Emulator
{
    public sealed class CPUState
    {
        public long Cycles = 0;
        public CPUFlags Flags = 0;
        public ushort PC;
        public ushort SP;
    }

    [Flags]
    public enum CPUFlags
    {
        None = 0,
        Carry,
        Zero,
        Interrupt,
        Decimal,
        Break,
        Overflow,
        Negative
    }


    public abstract class CPU
    {
        public SystemMemory? @Memory { set; get; }
        public CPUState State { get; set; } = new CPUState();
        public CPURegisters Registers { get; set; } = new(); 
        public abstract void Step();
        public abstract void Startup();
        public abstract void Shutdown();

        public CPU(long systemMemory)
        {
            this.@Memory = new SystemMemory(new byte[systemMemory], State);
        }
    }

    public sealed class CPURegisters : Dictionary<string, object?>
    {
        public T GetValue<T>(string registerName)
        {
            if (TryGetValue(registerName, out object? value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                else
                {
                    throw new InvalidCastException($"Register {registerName} is of a different type");
                }
            }
            else
            {
                throw new ArgumentException($"Register {registerName} not found");
            }
        }

        public void SetValue<T>(string registerName, T value)
        {
            this[registerName] = value;
        }
    }


    public class InternalEmulatorException : Exception {
        public InternalEmulatorException(string message) : base(message) { }
    }

    public class SystemMemory
    {
        public Memory<byte> Memory { get;}
        private CPUState? m_cpuState;
        private CPUState CPUState => m_cpuState ?? throw new InternalEmulatorException("cpu state not set");

        public SystemMemory(Memory<byte> memory, CPUState cpuState)
        {
            Memory = memory;
            m_cpuState = cpuState;
        }

        public void Write(ushort address, byte value)
        {
            CPUState.Cycles += 1;
            Memory.Span[address] = value;
        }

        public byte Read(ushort address)
        {
            CPUState.Cycles += 1;
            return Memory.Span[address];
        }

        public void WriteWord(ushort address, ushort value)
        {
            Memory.Span[address] = (byte)(value & 0xFF);
            Memory.Span[address + 1] = (byte)((value >> 8) & 0xFF);

            CPUState.Cycles += 2;
        }

        public ushort ReadWord(ushort address)
        {
            CPUState.Cycles += 2;
            return (ushort)(Memory.Span[address] | (Memory.Span[address + 1] << 8));
        }

        public byte Read()
        {
            CPUState.Cycles += 1;
            return Memory.Span[CPUState.PC++];
        }

        public ushort ReadWord()
        {
            CPUState.Cycles += 2;
            var value = (ushort)(Memory.Span[CPUState.PC] | (Memory.Span[CPUState.PC + 1] << 8));
            CPUState.PC += 2;
            return value;
        }

        public void WriteWord(ushort value)
        {
            Memory.Span[CPUState.PC] = (byte)(value & 0xFF);
            Memory.Span[CPUState.PC + 1] = (byte)((value >> 8) & 0xFF);

            CPUState.Cycles += 2;
        }

        public void Write(byte value)
        {
            Memory.Span[CPUState.PC++] = value;
            CPUState.Cycles += 1;
        }
    }


    public delegate void Operation(CPU cpu, byte opcode);

    public class InstructionSet: Dictionary<byte, Operation>
    {
        public Operation Process(byte opcode)
        {
            if (this.TryGetValue(opcode, out var action))
            {
                return action;
            }
            else throw new Exception($"Opcode {opcode} not found");
        }
    }

    public class MutiThreadedCPU : CPU
    {
        Thread? Thread { get; set; }
        public bool Running
        {
            get
            {
                if (Thread == null) return false;
                return Thread.IsAlive;
            }
        }

        public MutiThreadedCPU(long systemMemory) : base(systemMemory)
        {
            Thread = new Thread(Step);
            Thread.Name = "CPU";
            Thread.IsBackground = true;
        }

        public override void Shutdown()
        {
            throw new NotImplementedException();
        }

        public override void Startup()
        {
            Thread?.Start();
        }

        public override void Step()
        {
            throw new NotImplementedException();
        }
    }

    public class SingleThreadedCPU : CPU
    {
        private bool _is_running = false;
        private bool _locked = true;
        public bool Running
        {
            get
            {
                return _is_running;
            }
            protected set
            {
                if (_locked) throw new Exception("CPU Runtime is locked!");
                _is_running = value;
            }
        }

        public SingleThreadedCPU(long systemMemory) : base(systemMemory)
        {
            _is_running = false;
            _locked = true;
        }

        public override void Shutdown()
        {
            UnlockRuntime();
            Running = false;
            LockRuntime();
        }

        protected void LockRuntime()
        {
            _locked = true;

        }

        protected void UnlockRuntime()
        {
            _locked = false;
        }

        public override void Startup()
        {
            UnlockRuntime();
            Running = true;
            LockRuntime();

            // Start the emulation loop directly
            EmulationLoop();
        }

        public override void Step()
        {
            throw new NotImplementedException();
        }

        private void EmulationLoop()
        {
            while (Running)
            {
                // Execute one iteration of the emulation
                Step();

                // You might introduce a delay here if needed
                // to control the emulation speed
            }
        }
    }

}

namespace Emulator.Machines
{
    public class CPU6502 : SingleThreadedCPU
    {
        int count = 0;
        internal static readonly InstructionSet instructionSet = new InstructionSet
        {
            // null
            {0x00, (_, _) => { } },
            // nop
            {0x90, (_, _) => { } },
            // lda int
            {0xA9, (cpu, _) =>
            {
                byte data = cpu.Memory!.Read();
                cpu.Registers.SetValue("A", data);
                if ((data & 0x80) == 0x80)
                    cpu.State.Flags |= CPUFlags.Negative;
                if (data == 0)
                    cpu.State.Flags |= CPUFlags.Zero;
            }
            },
            // ldx int
            {0xA2, (cpu, _) =>
            {
                byte data = cpu.Memory!.Read();
                cpu.Registers.SetValue("X", data);
                if ((data & 0x80) == 0x80)
                    cpu.State.Flags |= CPUFlags.Negative;
                if (data == 0)
                    cpu.State.Flags |= CPUFlags.Zero;
            }
            },
            // ldy int
            {0xA0, (cpu, _) =>
            {
                byte data = cpu.Memory!.Read();
                cpu.Registers.SetValue("Y", data);
                if ((data & 0x80) == 0x80)
                    cpu.State.Flags |= CPUFlags.Negative;
                if (data == 0)
                    cpu.State.Flags |= CPUFlags.Zero;
            }
            },
        };

        public CPU6502() : base(4096)
        {
            Console.WriteLine("6502 CPU is being constructed...!");
        }

        public override void Step()
        {

            byte opcode = Memory!.Read();

            Operation operation = CPU6502.instructionSet.Process(opcode);

            operation(this, opcode);

        }
    }
}
