using NUnit.Framework;
using Emulator;
using Emulator.Machines;

namespace CPUTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test(Description = "LDA Instruction Test for 6502 CPU.")]
        public void LDA6502Test()
        {
            var cpu = new CPU6502();

            cpu.Memory!.Write(cpu.State.PC, 0xA9);
            cpu.Memory!.Write((ushort)(cpu.State.PC + 1), 0x12);

            cpu.Step();

            Assert.AreEqual(cpu.Registers.GetValue<byte>("A"), 0x12);
        }

        [Test(Description = "LDX Instruction Test for 6502 CPU.")]
        public void LDX6502Test()
        {
            var cpu = new CPU6502();

            cpu.Memory!.Write(cpu.State.PC, 0xA2);
            cpu.Memory!.Write((ushort)(cpu.State.PC + 1), 0x18);

            cpu.Step();

            Assert.AreEqual(cpu.Registers.GetValue<byte>("X"), 0x18);
        }
        [Test(Description = "LDY Instruction Test for 6502 CPU.")]
        public void LDY6502Test()
        {
            var cpu = new CPU6502();

            cpu.Memory!.Write(cpu.State.PC, 0xA0);
            cpu.Memory!.Write((ushort)(cpu.State.PC + 1), 0x23);

            cpu.Step();

            Assert.AreEqual(cpu.Registers.GetValue<byte>("Y"), 0x23);
        }
    }
}