using System.Numerics;
using System.Runtime.InteropServices;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Tests;

// The interface between mesh.vert and RenderPipeline (finding V13 of the codebase review
// of 2026-10-13, TASK-0074). Until this test, a swap of the two matrices in
// WritePushConstants, or a new member in the block of the shader, passed each test. A swap
// of the two arguments of the call in Push still passes these tests; the image test fails
// on it (finding V24 of the codebase review of 2026-10-17, TASK-0079). The compiler strips the
// names, so the test reads the structure of the SPIR-V: the block of the push constant
// holds two column-major 4 x 4 matrices at offsets 0 and 64, member 0 multiplies the
// position first, and member 1 multiplies that result. RenderPipeline writes the
// model-view matrix at offset 0 and the projection at offset 64.
public class PushConstantInterfaceTests
{
    private const ushort OpDecorate = 71, OpMemberDecorate = 72, OpTypeMatrix = 24, OpTypeStruct = 30,
        OpTypePointer = 32, OpConstant = 43, OpVariable = 59, OpLoad = 61, OpAccessChain = 65, OpMatrixTimesVector = 145;

    private const uint StorageClassPushConstant = 9, DecorationColMajor = 5, DecorationMatrixStride = 7, DecorationOffset = 35;

    [Fact]
    public void The_Block_Of_The_Shader_Is_Two_Matrices_That_Fill_The_Push_Constant_Range()
    {
        var module = Read(ShaderLibrary.Load("mesh.vert"));
        var block = module.PushConstantBlock();

        Assert.Equal(2, block.Members.Length);
        Assert.All(block.Members, member => Assert.Equal((4u, 4u), module.Matrices[member]));
        Assert.Equal([0u, 64u], block.Offsets);
        Assert.All(block.ColumnMajor, Assert.True);
        Assert.All(block.Strides, stride => Assert.Equal(16u, stride));
        Assert.Equal(RenderPipeline.PushConstantSize, block.Offsets[^1] + 64u);
    }

    [Fact]
    public void Member_0_Multiplies_The_Position_And_Member_1_Multiplies_Its_Result()
    {
        var module = Read(ShaderLibrary.Load("mesh.vert"));
        var products = module.Products();

        var first = Assert.Single(products, product => product.Member == 0);
        var second = Assert.Single(products, product => product.Member == 1);
        Assert.Equal(first.Result, second.Vector);
    }

    [Fact]
    public void The_Pipeline_Writes_The_Model_View_At_Offset_0_And_The_Projection_At_Offset_64()
    {
        var modelView = Matrix4x4.CreateTranslation(1, 2, 3);
        var projection = Matrix4x4.CreateScale(4, 5, 6);
        var block = new byte[RenderPipeline.PushConstantSize];

        RenderPipeline.WritePushConstants(block, modelView, projection);

        Assert.Equal(modelView, MemoryMarshal.Read<Matrix4x4>(block.AsSpan(0, 64)));
        Assert.Equal(projection, MemoryMarshal.Read<Matrix4x4>(block.AsSpan(64, 64)));
    }

    private static Module Read(byte[] spirv)
    {
        var words = MemoryMarshal.Cast<byte, uint>(spirv).ToArray();
        var module = new Module();
        for (var index = 5; index < words.Length;)
        {
            var count = (int)(words[index] >> 16);
            var opcode = (ushort)(words[index] & 0xFFFF);
            var operands = words.AsSpan(index + 1, count - 1).ToArray();
            module.Add(opcode, operands);
            index += count;
        }

        return module;
    }

    private sealed record Block(uint[] Members, uint[] Offsets, bool[] ColumnMajor, uint[] Strides);

    private sealed record Product(int Member, uint Vector, uint Result);

    private sealed class Module
    {
        public Dictionary<uint, (uint Columns, uint Rows)> Matrices { get; } = new();

        private readonly Dictionary<uint, uint> _vectorSizes = new();
        private readonly Dictionary<uint, uint[]> _structs = new();
        private readonly Dictionary<uint, (uint Storage, uint Type)> _pointers = new();
        private readonly Dictionary<uint, uint> _constants = new();
        private readonly List<(uint Type, uint Id, uint Storage)> _variables = new();
        private readonly List<(uint Struct, uint Member, uint Decoration, uint Value)> _memberDecorations = new();
        private readonly Dictionary<uint, (uint Base, uint Index)> _chains = new();
        private readonly Dictionary<uint, uint> _loads = new();
        private readonly List<(uint Result, uint Matrix, uint Vector)> _products = new();

        public void Add(ushort opcode, uint[] operands)
        {
            switch (opcode)
            {
                case 23: // OpTypeVector: result, component type, count
                    _vectorSizes[operands[0]] = operands[2];
                    break;
                case OpTypeMatrix:
                    Matrices[operands[0]] = (operands[2], _vectorSizes.GetValueOrDefault(operands[1]));
                    break;
                case OpTypeStruct:
                    _structs[operands[0]] = operands[1..];
                    break;
                case OpTypePointer:
                    _pointers[operands[0]] = (operands[1], operands[2]);
                    break;
                case OpConstant:
                    _constants[operands[1]] = operands[2];
                    break;
                case OpVariable:
                    _variables.Add((operands[0], operands[1], operands[2]));
                    break;
                case OpMemberDecorate:
                    _memberDecorations.Add((operands[0], operands[1], operands[2], operands.Length > 3 ? operands[3] : 0));
                    break;
                case OpAccessChain when operands.Length == 4:
                    _chains[operands[1]] = (operands[2], operands[3]);
                    break;
                case OpLoad:
                    _loads[operands[1]] = operands[2];
                    break;
                case OpMatrixTimesVector:
                    _products.Add((operands[1], operands[2], operands[3]));
                    break;
                case OpDecorate:
                default:
                    break;
            }
        }

        private (uint Variable, uint Struct) PushConstantVariable()
        {
            var variable = Assert.Single(_variables, v => v.Storage == StorageClassPushConstant);
            return (variable.Id, _pointers[variable.Type].Type);
        }

        public Block PushConstantBlock()
        {
            var (_, structId) = PushConstantVariable();
            var members = _structs[structId];
            uint Decoration(int member, uint decoration) => _memberDecorations
                .Where(d => d.Struct == structId && d.Member == member && d.Decoration == decoration)
                .Select(d => d.Value).FirstOrDefault(uint.MaxValue);

            return new Block(
                members,
                [.. Enumerable.Range(0, members.Length).Select(m => Decoration(m, DecorationOffset))],
                [.. Enumerable.Range(0, members.Length).Select(m => Decoration(m, DecorationColMajor) != uint.MaxValue)],
                [.. Enumerable.Range(0, members.Length).Select(m => Decoration(m, DecorationMatrixStride))]);
        }

        // Each product of a matrix of the block and a vector, with the index of the
        // member that gave the matrix.
        public List<Product> Products()
        {
            var (variable, _) = PushConstantVariable();
            var products = new List<Product>();
            foreach (var (result, matrix, vector) in _products)
            {
                if (_loads.TryGetValue(matrix, out var pointer)
                    && _chains.TryGetValue(pointer, out var chain)
                    && chain.Base == variable)
                    products.Add(new Product((int)_constants[chain.Index], vector, result));
            }

            return products;
        }
    }
}
