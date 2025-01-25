namespace Chip8;

public readonly struct OpCode
    {
        public ushort Data { get;}
        public byte Set { get;}
        public ushort NNN { get;}
        public byte NN { get;} 
        public byte N { get;}
        public byte X { get;}
        public byte Y { get;}
        public OpCode(ushort data)
        {
            // variable values from the instructions are mapped to an OpCode

            this.Data = data;
            this.Set = (byte)(data >>12); // For all set instructions we only care about the first hex#
            this.NNN = (ushort)(data & 0x0FFF); // NNN is usually an address pointer and is the last three hex values
            this.NN = (byte)(data & 0x00FF); // Also part of opCode, this value is usually used in skipping
            this.N = (byte)(data & 0x000F); // Only used once for drawing but for the scape of consistentcy is included
            this.X = (byte)((data & 0X0F00) >> 8); // points to a specific register
            this.Y = (byte)((data & 0X00F0) >> 4); // points to a specific register
        }

    }
