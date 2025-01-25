using Chip8.CPU;
using Chip8.Exceptions;
using Chip8.Peripherals;

namespace Chip8;
//TODO: Needs performance optimizations
public class Chip8Processor
{
    private readonly byte[] _memory = new byte[0x1000];
    private readonly byte[] _v = new byte[16];
    private readonly ushort[] _stack = new ushort[16];
    private bool[,] _screen =  new bool[SCREEN_WIDTH, SCREEN_HEIGHT];
    private bool[,] _pendingClearScreen = new bool[SCREEN_WIDTH, SCREEN_HEIGHT];
    private bool _needsRedraw = true;
    
    private ushort _pc = 0;
    private ushort _sp = 0;
    private ushort _i = 0;
    private byte _delay = 0;
    
    private readonly HashSet<byte> _pressedKeys = [];

    private const int SCREEN_WIDTH = 64;
    private const int SCREEN_HEIGHT = 32;
    private const int ROM_START_LOCATION = 0x200;

    private readonly Dictionary<byte, Action<OpCode>> _instructions = [];
    private readonly Dictionary<byte, Action<OpCode>> _miscInstructions = [];
    private readonly Random _rand = new ();

    private readonly ISoundPlayer _soundPlayer;
    private readonly IRenderer _renderer;

    public Chip8Processor(IRenderer renderer ,ISoundPlayer soundPlayer)
    {
        _instructions[0x0] = this.ZeroOps;
        _instructions[0x1] = this.JumptoAddress;
        _instructions[0x2]  = this.CallSubroutine;
        _instructions[0x3] = this.SkipVxEqNN;
        _instructions[0x4] = this.SkipVxNeqNN;
        _instructions[0x5] = this.SkipVxEqVy;
        _instructions[0x6] = this.SetVxtoNN;
        _instructions[0x7] = this.AddNNtoVx;
        _instructions[0x8] = this.XYOps;
        _instructions[0x9] = this.SkipIfVxNeqVy;

        _instructions[0xA] = this.SetI;
        _instructions[0xB] = this.JumpWithV0;
        _instructions[0xC] = this.Rand;
        _instructions[0xD] = this.Draw;
        _instructions[0xE] = this.SkipOnKey;
        _instructions[0xF] = this.Misc;

        _miscInstructions[0x07] = this.GetDelay;
        _miscInstructions[0x0A] = this.WaitKey;
        _miscInstructions[0x15] = this.SetDelay;
        _miscInstructions[0x18] = this.SetSoundTimer;
        _miscInstructions[0x1E] = this.AddVxToI;
        _miscInstructions[0x29] = this.SetIToCharSprite;
        _miscInstructions[0x33] = this.SetBCD;
        _miscInstructions[0x55] = this.RegDump;
        _miscInstructions[0x65] = this.RegLoad;

        _soundPlayer = soundPlayer;
        _renderer = renderer;
    }

    public async Task LoadRom(Stream rom)
    {
        Reset();
        using var memory = new MemoryStream(_memory, ROM_START_LOCATION, (int)rom.Length, true);
        await rom.CopyToAsync(memory);
    }
    

    public void Tick()
    {
        var data = (ushort) (_memory[_pc++] << 8 | _memory[_pc++]);
        var opCode = new OpCode(data);

        if(!_instructions.TryGetValue(opCode.Set, out var instruction))
        {
            throw new InstructionNotValidException($"Instruction is not part of arch or is not implemented");
        }

        instruction(opCode);
    }

    public void Tick60Hz()
    {
        if(_delay > 0)
        {
            _delay--;
        }

        if(_needsRedraw)
        {
            _renderer.Draw(_screen);
            _needsRedraw = false;
        }
    }

    public void Reset()
    {
        Array.Clear(_memory, 0, _memory.Length);
        for (var i = 0; i != Fonts.Characters.Length; ++i)
            _memory[i] = Fonts.Characters[i];

        Array.Clear(_v, 0, _v.Length);
        Array.Clear(_stack, 0, _stack.Length);
        Array.Clear(_screen, 0, _screen.Length);
        _pc = ROM_START_LOCATION;
        _i = 0;
        _sp = 0;
    }

    public void SetKeyDown(byte key)
    {
        _pressedKeys.Add(key);
    }
    
    public void SetKeyUp(byte key)
    {
        _pressedKeys.Remove(key);
    }

    #region Instructions

    private void JumptoAddress(OpCode opCode)
    {
        _pc = opCode.NNN;
    }
    
    private void CallSubroutine(OpCode opCode)
    {
        Push(_pc);
        _pc = opCode.NNN;
    }
    
    private void SkipVxEqNN(OpCode opCode)
    {
        if(_v[opCode.X] == opCode.NN)
        {
            _pc += 2;
        }
    }

    private void SkipVxNeqNN(OpCode opCode)
    {
        if(_v[opCode.X] != opCode.NN)
        {
            _pc += 2;
        }
    }
    private void SkipVxEqVy(OpCode opCode)
    {
        if(_v[opCode.X] == _v[opCode.Y])
        {
            _pc += 2;
        }
    }

    private void SetVxtoNN(OpCode opCode)
    {
        _v[opCode.X] = opCode.NN;
    }

    private void AddNNtoVx(OpCode opCode)
    {
        var res =_v[opCode.X] + opCode.NN;
        bool carry = res > 255;
        if(carry)
        {
            res -= 255;
        }

        _v[opCode.X] = (byte)(res & 0x00FF);
    }

    private void XYOps(OpCode opCode)
    {
        switch(opCode.N)
        {
            case 0x0:
                _v[opCode.X] = _v[opCode.Y];
                break;
            
            case 0x1:
                _v[opCode.X] |= _v[opCode.Y];
                break;
            
            case 0x2:
                _v[opCode.X] &= _v[opCode.Y];
                break;
            
            case 0x3:
                _v[opCode.X] ^= _v[opCode.Y];
                break;
            
            case 0x4:
                var res = _v[opCode.X] + _v[opCode.Y];
                var carry = res > 255;
                _v[opCode.X] = (byte)res;
                _v[0xF] = (byte)(carry ? 1 : 0);
                break;
            
            case 0x5:
                _v[0xF] = (byte)(_v[opCode.X] >= _v[opCode.Y] ? 1 : 0);
                _v[opCode.X] -= _v[opCode.Y];
                break;

            case 0x6:
                _v[0xF] = (byte)((_v[opCode.X] & 0x1) == 1 ? 1 : 0);
                _v[opCode.X] >>= 1;
                break;
            
            case 0x7:
                _v[opCode.X] = (byte)(_v[opCode.Y] - _v[opCode.X]);
                _v[0xF] = (byte)(_v[opCode.Y] >= _v[opCode.X] ? 1 : 0);
                break;

            case 0xE:
                _v[0xF] = (byte)((_v[opCode.X] & 0xA0) == 0xA0 ? 1 : 0);
                _v[opCode.X] <<= 1;
                break;

            default:
                throw new InstructionNotValidException($"The instruction {opCode.N} is not part of the CPU");

        }
    }

    private void SetI(OpCode opCode)
    {
        _i = opCode.NNN;
    }

    private void JumpWithV0(OpCode opCode)
    {
        _pc = (ushort)(_v[0] + opCode.NNN);
    }

    private void Rand(OpCode opCode)
    {
        _v[opCode.X] = (byte)(_rand.Next(0,255) & opCode.NNN);
    }

    private void Draw(OpCode opCode)
    {
        var startX = _v[opCode.X];
        var startY = _v[opCode.Y];

        for(var x = 0; x < SCREEN_WIDTH; x++)
        {
            for(var y = 0; y < SCREEN_HEIGHT; y++)
            {
                if(_pendingClearScreen[x, y])
                {
                    if(_screen[x, y])
                    {
                        _needsRedraw = true;
                    }

                    _pendingClearScreen[x, y] = false;
                    _screen[x, y] = false;
                }
            }
        }

        _v[0xF] = 0;

        for(var i = 0; i < opCode.N; i++)
        {
            var rowData = _memory[_i + i];

            for(var bit = 0; bit < 8; bit++)
            {
                var x = (startX + bit) % SCREEN_WIDTH;
                var y = (startY + i) % SCREEN_HEIGHT;

                var spriteBit = ((rowData >> (7 - bit)) & 1);
                var oldBit = _screen[x, y] ? 1 : 0;

                if(oldBit != spriteBit)
                    _needsRedraw = true;
                
                var newBit = oldBit ^ spriteBit;

                if(newBit != 0)
                    _screen[x, y] = true;
                else
                    _pendingClearScreen[x, y] = true;

                if(oldBit != 0 && newBit == 0)
                    _v[0xF] = 1;
            }
        }
    }

    private void ZeroOps(OpCode opCode)
    {
        switch(opCode.NN)
        {
            case 0xE0:
                Array.Clear(_screen, 0, _screen.Length);
                break;
            
            case 0xEE:
                _pc = Pop();
                break;

            default:
                throw new InstructionNotValidException();
        }
    }

    private void SkipOnKey(OpCode opCode)
    {
        switch(opCode.NN)
        {
            case 0X9E:
                if(_pressedKeys.Contains(_v[opCode.X]))
                    _pc += 2;
                break;

            case 0XA1:
                if(!_pressedKeys.Contains(_v[opCode.X]))
                    _pc += 2;
                break;

            default:
                throw new InstructionNotValidException($" OpCode 0XE{opCode.NN:X} is not implemented");
        }

    }

    private void SkipIfVxNeqVy(OpCode opCode)
    {
        if(_v[opCode.X] != _v[opCode.Y])
            _pc += 2;
    }

    private void Push(ushort ProgramCounter)
    {
        _stack[_sp++] = ProgramCounter;

    }

    private ushort Pop()
    {
        return _stack[--_sp];
    }

    #endregion Instructions

    #region Misc instructions
    
    private void Misc(OpCode opCode)
    {
        if(!_miscInstructions.TryGetValue(opCode.NN, out var instructions))
            throw new InstructionNotValidException($"Instruction is not part of arch or is not implemented");

        instructions(opCode);
    }

    private void GetDelay(OpCode opCode)
    {
        _v[opCode.X] = _delay;
    }

    private void WaitKey(OpCode opCode)
    {
        if(!_pressedKeys.Any())
            _pc -= 2;
        else
            _v[opCode.X] = _pressedKeys.First();
    }

    private void SetDelay(OpCode opCode)
    {
        _delay = _v[opCode.X];
    }

    private void SetSoundTimer(OpCode opCode)
    {
        _soundPlayer.Beep(_v[opCode.X]);
    }

    private void AddVxToI(OpCode opCode)
    {
        _i += _v[opCode.X];
    }

    private void SetIToCharSprite(OpCode opCode)
    {
        _i = (ushort)(_v[opCode.X] * 5);
    }

    private void SetBCD(OpCode opCode)
    {
        _memory[_i] = (byte) (_v[opCode.X]/100);
        _memory[_i] = (byte) ((_v[opCode.X]/10) % 10);
        _memory[_i] = (byte) (_v[opCode.X] % 10);
    }

    private void RegDump(OpCode opCode)
    {
        for(byte i = 0; i <= opCode.X; i++ )
            _memory[_i + 1] = _v[i];
    }

    private void RegLoad(OpCode opCode)
    {
        for(byte i = 0; i <= opCode.X; i++)
            _v[i] = _v[_i + i];
    }

    #endregion Misc instructions

}
