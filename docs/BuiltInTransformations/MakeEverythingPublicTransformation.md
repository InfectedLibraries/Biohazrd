`MakeEverythingPublicTransformation`
===================================================================================================

<small>\[[Transformation Source](../../Biohazrd.Transformation/Common/MakeEverythingPublicTransformation.cs)\]</small>

Does what it says on the tin. Goes through your entire library and makes everything public.

## When this transformation is applicable

This transformation is primarily intended for debugging, but it is also currently the only way to access protected members. (See [#105](https://github.com/InfectedLibraries/Biohazrd/issues/105))

Note that while it does expose private functions as well, non-virtual private functions should generally **not** be called because they won't get exported from the DLL. (You're also violating the API contract of your native library, but that's none of Biohazrd's business.)

Note that even if you don't use this transformation, non-public members will still end up in the generated output by default. So you can use usual .NET reflection shenanigans to attempt to access them.

## Example

Nothing surprising here, but given the following C++:

```cpp
class MyClass
{
private:
    int PrivateField;
    void PrivateMethod();
protected:
    int ProtectedField;
    void ProtectedMethod();
public:
    int PublicField;
    void PublicMethod();
};
```

Biohazrd's will output the following declaraiton tree and C# before and after this transformation:

<table>
<tr><th>Before</th><th>After</th></tr>
<tr>
<td>

```
Public TranslatedRecord MyClass
    Private TranslatedNormalField PrivateField @ 0
    Private TranslatedFunction PrivateMethod
    Private TranslatedNormalField ProtectedField @ 4
    Private TranslatedFunction ProtectedMethod
    Public TranslatedNormalField PublicField @ 8
    Public TranslatedFunction PublicMethod
```
</td>
<td>

```
Public TranslatedRecord MyClass
    Public TranslatedNormalField PrivateField @ 0
    Public TranslatedFunction PrivateMethod
    Public TranslatedNormalField ProtectedField @ 4
    Public TranslatedFunction ProtectedMethod
    Public TranslatedNormalField PublicField @ 8
    Public TranslatedFunction PublicMethod
```
</td>
</tr>
<tr>
<td>

```csharp
// This file was automatically generated by Biohazrd and should not be modified by hand!
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 12)]
public unsafe partial struct MyClass
{
    [FieldOffset(0)] private int PrivateField;

    [DllImport("Example.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?PrivateMethod@MyClass@@AEAAXXZ", ExactSpelling = true)]
    private static extern void PrivateMethod_PInvoke(MyClass* @this);

    private unsafe void PrivateMethod()
    {
        fixed (MyClass* @this = &this)
        { PrivateMethod_PInvoke(@this); }
    }

    [FieldOffset(4)] private int ProtectedField;

    [DllImport("Example.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?ProtectedMethod@MyClass@@IEAAXXZ", ExactSpelling = true)]
    private static extern void ProtectedMethod_PInvoke(MyClass* @this);

    private unsafe void ProtectedMethod()
    {
        fixed (MyClass* @this = &this)
        { ProtectedMethod_PInvoke(@this); }
    }

    [FieldOffset(8)] public int PublicField;

    [DllImport("Example.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?PublicMethod@MyClass@@QEAAXXZ", ExactSpelling = true)]
    private static extern void PublicMethod_PInvoke(MyClass* @this);

    public unsafe void PublicMethod()
    {
        fixed (MyClass* @this = &this)
        { PublicMethod_PInvoke(@this); }
    }
}
```
</td>
<td>

```csharp
// This file was automatically generated by Biohazrd and should not be modified by hand!
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 12)]
public unsafe partial struct MyClass
{
    [FieldOffset(0)] public int PrivateField;

    [DllImport("Example.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?PrivateMethod@MyClass@@AEAAXXZ", ExactSpelling = true)]
    private static extern void PrivateMethod_PInvoke(MyClass* @this);

    public unsafe void PrivateMethod()
    {
        fixed (MyClass* @this = &this)
        { PrivateMethod_PInvoke(@this); }
    }

    [FieldOffset(4)] public int ProtectedField;

    [DllImport("Example.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?ProtectedMethod@MyClass@@IEAAXXZ", ExactSpelling = true)]
    private static extern void ProtectedMethod_PInvoke(MyClass* @this);

    public unsafe void ProtectedMethod()
    {
        fixed (MyClass* @this = &this)
        { ProtectedMethod_PInvoke(@this); }
    }

    [FieldOffset(8)] public int PublicField;

    [DllImport("Example.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?PublicMethod@MyClass@@QEAAXXZ", ExactSpelling = true)]
    private static extern void PublicMethod_PInvoke(MyClass* @this);

    public unsafe void PublicMethod()
    {
        fixed (MyClass* @this = &this)
        { PublicMethod_PInvoke(@this); }
    }
}
```
</td>
</tr>
</table>
