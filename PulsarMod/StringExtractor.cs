using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using MelonLoader;
//source of code https://web.archive.org/web/20200727042639/https://stackoverflow.com/questions/14243284/how-can-i-retrieve-string-literals-using-reflection#
class StringExtractors
{
    public static IEnumerable<string> FindLiterals(MethodInfo method)
    {
        var reader = new ILReader(method);
        foreach (var instruction in reader.Instructions)
        {
            if (instruction.Op == OpCodes.Ldstr)
            {
                yield return instruction.Data as string;
            }
        }
    }
}

public interface IILReaderProvider
{
    byte[] GetMethodBody();

    FieldInfo ResolveField(int metadataToken);
    MemberInfo ResolveMember(int metadataToken);

    MethodBase ResolveMethod(int metadataToken);
    byte[] ResolveSignature(int metadataToken);

    string ResolveString(int metadataToken);
    Type ResolveType(int metadataToken);
}

[StructLayout(LayoutKind.Sequential)]
public struct ILInstruction
{
    private readonly OpCode
        operationCode; // 40.  56-64.  The entire structure is very big.  maybe do array lookup for opcode instead.

    private readonly byte[] instructionRawData;

    private readonly object instructionData;

    private readonly int instructionAddress;

    private readonly int index;

    internal ILInstruction(OpCode code, byte[] instructionRawData, int instructionAddress, object instructionData,
        int index)
    {
        this.operationCode = code;
        this.instructionRawData = instructionRawData;
        this.instructionAddress = instructionAddress;
        this.instructionData = instructionData;
        this.index = index;
    }

    public OpCode Op
    {
        get { return this.operationCode; }
    }

    /// <summary>
    /// Gets the raw data.
    /// </summary>
    public byte[] RawData
    {
        get { return this.instructionRawData; }
    }

    /// <summary>
    /// Gets the data.
    /// </summary>
    public object Data
    {
        get { return this.instructionData; }
    }

    /// <summary>
    /// Gets the address of the instruction.
    /// </summary>
    public int Address
    {
        get { return this.instructionAddress; }
    }

    /// <summary>
    /// Gets the index of the instruction.
    /// </summary>
    /// <value>
    /// The index of the instruction.
    /// </value>
    public int InstructionIndex => this.index;

    /// <summary>
    /// Gets the value as integer
    /// </summary>
    /// <value>The data value.</value>
    public int DataValue
    {
        get
        {
            var value = 0;
            if (this.Data == null) return value;
            if (this.Data is byte)
            {
                value = (byte) Data;
            }
            else if (this.Data is short)
            {
                value = (short) this.Data;
            }
            else if (this.Data is int)
            {
                value = (int) this.Data;
            }

            return value;
        }
    }

    /// <summary>
    /// Gets the length of the instructions and operands.
    /// </summary>
    /// <value>The length.</value>
    public int Length => this.Op.Size + (RawData?.Length ?? 0);

    /// <summary>
    /// Returns a <see cref="System.String"/> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String"/> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendFormat("0x{0:x4} {1,-10}", this.Address, this.Op.Name);

        if (this.Data != null)
        {
            builder.Append(this.Data);
        }

        if (this.RawData == null || this.RawData.Length <= 0) return builder.ToString();
        builder.Append(" [0x");
        for (var i = this.RawData.Length - 1; i >= 0; i--)
        {
            builder.Append(this.RawData[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        builder.Append(']');

        return builder.ToString();
    }
}

/// <summary>
/// Reads IL instructions from a byte stream.
/// </summary>
/// <remarks>Allows generated code to be viewed without debugger or enabled debug assemblies.</remarks>
public sealed class ILReader
{
    /// <summary>
    /// The _instruction lookup.
    /// </summary>
    private static readonly Lazy<Dictionary<short, OpCode>> instructionLookup =
        new Lazy<Dictionary<short, OpCode>>(ILReader.GetLookupTable,
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// The IL reader provider.
    /// </summary>
    private IILReaderProvider intermediateLanguageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ILReader"/> class.
    /// </summary>
    /// <param name="method">
    /// The method.
    /// </param>
    public ILReader(MethodInfo method)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        this.intermediateLanguageProvider = ILReader.CreateILReaderProvider(method);
    }

    /// <summary>
    /// Gets the instructions.
    /// </summary>
    /// <value>The instructions.</value>
    public IEnumerable<ILInstruction> Instructions
    {
        get
        {
            var instructionBytes = this.intermediateLanguageProvider.GetMethodBody();
            int instructionIndex = 0, startAddress;
            for (var position = 0; position < instructionBytes.Length;)
            {
                startAddress = position;
                short operationData = instructionBytes[position];
                if (IsInstructionPrefix(operationData))
                {
                    operationData = (short) ((operationData << 8) | instructionBytes[++position]);
                }

                position++;

                if (!instructionLookup.Value.TryGetValue(operationData, out var code))
                {
                    throw new InvalidProgramException($"0x{operationData:X2} is not a valid op code.");
                }

                var dataSize = GetSize(code.OperandType);
                var data = new byte[dataSize];
                Buffer.BlockCopy(instructionBytes, position, data, 0, dataSize);
                var objData = this.GetData(code, data);
                position += dataSize;

                if (code.OperandType == OperandType.InlineSwitch)
                {
                    dataSize = (int) objData;
                    int[] labels = new int[dataSize];
                    for (int index = 0; index < labels.Length; index++)
                    {
                        labels[index] = BitConverter.ToInt32(instructionBytes, position);
                        position += 4;
                    }

                    objData = labels;
                }

                yield return new ILInstruction(code, data, startAddress, objData, instructionIndex);
                instructionIndex++;
            }
        }
    }


    /// <summary>
    /// Creates the IL reader provider.
    /// </summary>
    /// <param name="methodInfo">The MethodInfo object that represents the method to read..</param>
    /// <returns>
    /// The ILReader provider.
    /// </returns>
    private static IILReaderProvider CreateILReaderProvider(MethodInfo methodInfo)
    {
        IILReaderProvider reader = DynamicILReaderProvider.Create(methodInfo);
        return reader ?? new ILReaderProvider(methodInfo);
    }

    /// <summary>
    /// Checks to see if the IL instruction is a prefix indicating the length of the instruction is two bytes long.
    /// </summary>
    /// <param name="value">The IL instruction as a byte.</param>
    /// <remarks>IL instructions can either be 1 or 2 bytes.</remarks>
    /// <returns>True if this IL instruction is a prefix indicating the instruction is two bytes long.</returns>
    private static bool IsInstructionPrefix(short value)
    {
        return ((value & OpCodes.Prefix1.Value) == OpCodes.Prefix1.Value) || ((value & OpCodes.Prefix2.Value) ==
                                                                              OpCodes.Prefix2.Value)
                                                                          || ((value & OpCodes.Prefix3.Value) ==
                                                                              OpCodes.Prefix3.Value) ||
                                                                          ((value & OpCodes.Prefix4.Value) ==
                                                                           OpCodes.Prefix4.Value)
                                                                          || ((value & OpCodes.Prefix5.Value) ==
                                                                              OpCodes.Prefix5.Value) ||
                                                                          ((value & OpCodes.Prefix6.Value) ==
                                                                           OpCodes.Prefix6.Value)
                                                                          || ((value & OpCodes.Prefix7.Value) ==
                                                                              OpCodes.Prefix7.Value) ||
                                                                          ((value & OpCodes.Prefixref.Value) ==
                                                                           OpCodes.Prefixref.Value);
    }

    /// <summary>
    /// The get lookup table.
    /// </summary>
    /// <returns>
    /// A dictionary of IL instructions.
    /// </returns>
    private static Dictionary<short, OpCode> GetLookupTable()
    {
        // Might be better to do an array lookup.  Use a seperate arrary for instructions without a prefix and array for each prefix.
        var fields = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);

        return fields.Select(field => (OpCode) field.GetValue(null)).ToDictionary(code => code.Value);
    }

    /// <summary>
    /// Gets the size of a operand.
    /// </summary>
    /// <param name="operandType">Defines the type of operand.</param>
    /// <returns>The size in bytes of the operand type.</returns>
    private static int GetSize(OperandType operandType)
    {
        switch (operandType)
        {
            case OperandType.InlineNone:
                return 0;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                return 1;
            case OperandType.InlineVar:
                return 2;
            case OperandType.InlineBrTarget:
            case OperandType.InlineField:
            case OperandType.InlineI:
            case OperandType.InlineMethod:
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineSwitch:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.ShortInlineR:
                return 4;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                return 8;
            default:
                return 0;
        }
    }

    private object GetData(OpCode code, byte[] rawData)
    {
        object data = null;
        switch (code.OperandType)
        {
            case OperandType.InlineField:
                data = this.intermediateLanguageProvider.ResolveField(BitConverter.ToInt32(rawData, 0));
                break;
            case OperandType.InlineSwitch:
                data = BitConverter.ToInt32(rawData, 0);

                break;
            case OperandType.InlineBrTarget:
            case OperandType.InlineI:
                data = BitConverter.ToInt32(rawData, 0);
                break;
            case OperandType.InlineI8:
                data = BitConverter.ToInt64(rawData, 0);
                break;
            case OperandType.InlineMethod:
                data = this.intermediateLanguageProvider.ResolveMethod(BitConverter.ToInt32(rawData, 0));
                break;
            case OperandType.InlineR:
                data = BitConverter.ToDouble(rawData, 0);
                break;
            case OperandType.InlineSig:
                data = this.intermediateLanguageProvider.ResolveSignature(BitConverter.ToInt32(rawData, 0));
                break;
            case OperandType.InlineString:
                data = this.intermediateLanguageProvider.ResolveString(BitConverter.ToInt32(rawData, 0));
                break;
            case OperandType.InlineTok:
            case OperandType.InlineType:
                data = this.intermediateLanguageProvider.ResolveType(BitConverter.ToInt32(rawData, 0));
                break;
            case OperandType.InlineVar:
                data = BitConverter.ToInt16(rawData, 0);
                break;
            case OperandType.ShortInlineVar:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineBrTarget:
                data = rawData[0];
                break;
            case OperandType.ShortInlineR:
                data = BitConverter.ToSingle(rawData, 0);
                break;
        }

        return data;
    }
}

internal class DynamicILReaderProvider : IILReaderProvider
{
    public const int TypeRidPrefix = 0x02000000;

    public const int MethodRidPrefix = 0x06000000;

    public const int FieldRidPrefix = 0x04000000;

    public static readonly Type RuntimeDynamicMethodType;

    private static readonly FieldInfo fileLengthField =
        typeof(ILGenerator).GetField("m_length", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo IntermediateLanguageBytesField =
        typeof(ILGenerator).GetField("m_ILStream", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly MethodInfo bakeByteArrayMethod =
        typeof(ILGenerator).GetMethod("BakeByteArray", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly PropertyInfo dynamicScopeIndexor;

    private static readonly FieldInfo dynamicScopeField;

    private static readonly Type genericMethodInfoType;

    private static readonly FieldInfo genericMethodHandleField;

    private static readonly FieldInfo genericMethodContextField;

    private static readonly Type varArgMethodType;

    private static readonly FieldInfo varArgMethodMethod;

    private static readonly Type genericFieldInfoType;

    private static readonly FieldInfo genericFieldInfoHandle;

    private static readonly FieldInfo genericFieldInfoContext;

    private static readonly FieldInfo ownerField;

    private object dynamicScope;

    private ILGenerator generator;

    static DynamicILReaderProvider()
    {
        var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        //dynamicScopeIndexor = Type.GetType("System.Reflection.Emit.DynamicScope").GetProperty("Item", bindingFlags);
        //dynamicScopeField = Type.GetType("System.Reflection.Emit.DynamicILGenerator").GetField("m_scope", bindingFlags);

        //varArgMethodType = Type.GetType("System.Reflection.Emit.VarArgMethod");
        //varArgMethodMethod = varArgMethodType.GetField("m_method", bindingFlags);

        //genericMethodInfoType = Type.GetType("System.Reflection.Emit.GenericMethodInfo");
        //genericMethodHandleField = genericMethodInfoType.GetField("m_methodHandle", bindingFlags);
        //genericMethodContextField = genericMethodInfoType.GetField("m_context", bindingFlags);

        // genericFieldInfoType = Type.GetType("System.Reflection.Emit.GenericFieldInfo", false);
        // if (genericFieldInfoType != null)
        // {
        //     genericFieldInfoHandle = genericFieldInfoType.GetField("m_fieldHandle", bindingFlags);
        //     genericFieldInfoContext = genericFieldInfoType.GetField("m_context", bindingFlags);
        // }
        // else
        // {
        //     genericFieldInfoHandle = genericFieldInfoContext = null;
        //}

        RuntimeDynamicMethodType = typeof(DynamicMethod);
        ownerField = RuntimeDynamicMethodType.GetField("m_owner", bindingFlags);
    }

    private DynamicILReaderProvider(DynamicMethod method)
    {
        this.Method = method;
        this.generator = method.GetILGenerator();
        this.dynamicScope = dynamicScopeField.GetValue(this.generator);
    }

    public DynamicMethod Method { get; private set; }

    internal object this[int token]
    {
        get { return dynamicScopeIndexor.GetValue(this.dynamicScope, new object[] {token}); }
    }

    public static DynamicILReaderProvider Create(MethodInfo method)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        var dynamicMethod = method as DynamicMethod;
        if (dynamicMethod != null)
        {
            return new DynamicILReaderProvider(dynamicMethod);
        }

        var methodType = method.GetType();
        if (RuntimeDynamicMethodType.IsAssignableFrom(methodType))
        {
            return new DynamicILReaderProvider(ownerField.GetValue(method) as DynamicMethod);
        }

        return null;
    }

    public byte[] GetMethodBody()
    {
        byte[] data = null;
        var ilgen = this.Method.GetILGenerator();

        try
        {
            data = (byte[]) bakeByteArrayMethod.Invoke(ilgen, null) ?? new byte[0];
        }
        catch (TargetInvocationException)
        {
            var length = (int) fileLengthField.GetValue(ilgen);
            data = new byte[length];
            Array.Copy((byte[]) IntermediateLanguageBytesField.GetValue(ilgen), data, length);
        }

        return data;
    }

    public FieldInfo ResolveField(int metadataToken)
    {
        var tokenValue = this[metadataToken];
        if (tokenValue is RuntimeFieldHandle)
        {
            return FieldInfo.GetFieldFromHandle((RuntimeFieldHandle) tokenValue);
        }

        if (tokenValue.GetType() == DynamicILReaderProvider.genericFieldInfoType)
        {
            return FieldInfo.GetFieldFromHandle(
                (RuntimeFieldHandle) genericFieldInfoHandle.GetValue(tokenValue),
                (RuntimeTypeHandle) genericFieldInfoContext.GetValue(tokenValue));
        }

        return null;
    }

    public MemberInfo ResolveMember(int metadataToken)
    {
        if ((metadataToken & TypeRidPrefix) != 0)
        {
            return this.ResolveType(metadataToken);
        }

        if ((metadataToken & MethodRidPrefix) != 0)
        {
            return this.ResolveMethod(metadataToken);
        }

        if ((metadataToken & FieldRidPrefix) != 0)
        {
            return this.ResolveField(metadataToken);
        }

        return null;
    }

    public MethodBase ResolveMethod(int metadataToken)
    {
        var tokenValue = this[metadataToken];
        DynamicMethod dynamicMethod = tokenValue as DynamicMethod;
        if (dynamicMethod != null)
        {
            return dynamicMethod;
        }

        if (tokenValue is RuntimeMethodHandle)
        {
            return MethodBase.GetMethodFromHandle((RuntimeMethodHandle) this[metadataToken]);
        }

        if (tokenValue.GetType() == genericFieldInfoType)
        {
            return MethodBase.GetMethodFromHandle(
                (RuntimeMethodHandle) genericMethodHandleField.GetValue(tokenValue),
                (RuntimeTypeHandle) genericMethodContextField.GetValue(tokenValue));
        }

        if (tokenValue.GetType() == varArgMethodType)
        {
            return DynamicILReaderProvider.varArgMethodMethod.GetValue(tokenValue) as MethodInfo;
        }

        return null;
    }

    public byte[] ResolveSignature(int metadataToken)
    {
        return this[metadataToken] as byte[];
    }

    public string ResolveString(int metadataToken)
    {
        return this[metadataToken] as string;
    }

    public Type ResolveType(int metadataToken)
    {
        return Type.GetTypeFromHandle((RuntimeTypeHandle) this[metadataToken]);
    }
}

internal class ILReaderProvider : IILReaderProvider
{
    public ILReaderProvider(MethodInfo method)
    {
        this.Method = method;
        this.MethodBody = method.GetMethodBody();
        this.MethodModule = method.Module;
    }

    public MethodInfo Method { get; private set; }

    public MethodBody MethodBody { get; private set; }

    public Module MethodModule { get; private set; }

    public byte[] GetMethodBody()
    {
        return this.MethodBody.GetILAsByteArray();
    }

    public FieldInfo ResolveField(int metadataToken)
    {
        return this.MethodModule.ResolveField(metadataToken);
    }

    public MemberInfo ResolveMember(int metadataToken)
    {
        return this.MethodModule.ResolveMember(metadataToken);
    }

    public MethodBase ResolveMethod(int metadataToken)
    {
        return this.MethodModule.ResolveMethod(metadataToken);
    }

    public byte[] ResolveSignature(int metadataToken)
    {
        return this.MethodModule.ResolveSignature(metadataToken);
    }

    public string ResolveString(int metadataToken)
    {
        return this.MethodModule.ResolveString(metadataToken);
    }

    public Type ResolveType(int metadataToken)
    {
        return this.MethodModule.ResolveType(metadataToken);
    }
}