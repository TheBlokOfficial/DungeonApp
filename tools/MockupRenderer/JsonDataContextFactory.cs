using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Threading;

namespace MockupRenderer;

/// <summary>
/// Buduje w locie prosty obiekt CLR z właściwościami odpowiadającymi płaskim polom
/// pliku JSON, żeby renderowana kontrolka miała do czego się bindować bez
/// prawdziwego ViewModelu. Zastępuje doraźne klasy atrapy wpisywane w kod na sztywno
/// jeden mechanizm działający dla dowolnego mockupu.
///
/// Wspierane są tylko płaskie pola najwyższego poziomu o wartości tekstowej,
/// liczbowej lub logicznej — to wystarcza do zasilenia bindingów w rodzaju
/// {Binding TotalSteps} czy {Binding StartupMessage}. Zagnieżdżone obiekty
/// i tablice są pomijane, bo atrapa nie modeluje hierarchii ViewModelu.
/// </summary>
internal static class JsonDataContextFactory
{
    private static int _typeCounter;

    public static object Create(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "Plik kontekstu danych musi zawierać obiekt JSON na najwyższym poziomie.");
        }

        var typeName = $"MockDataContext_{Interlocked.Increment(ref _typeCounter)}";
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("MockupRenderer.DynamicDataContexts"),
            AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);

        var pendingValues = new List<(string Name, object? Value)>();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var (clrType, value) = MapJsonProperty(property.Value);
            if (clrType is null)
            {
                continue; // pole zagnieżdżone lub null — poza zakresem atrapy
            }

            DefineAutoProperty(typeBuilder, property.Name, clrType);
            pendingValues.Add((property.Name, value));
        }

        var type = typeBuilder.CreateType();
        var instance = Activator.CreateInstance(type)!;

        foreach (var (name, value) in pendingValues)
        {
            type.GetProperty(name)!.SetValue(instance, value);
        }

        return instance;
    }

    private static (Type? ClrType, object? Value) MapJsonProperty(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => (typeof(string), element.GetString()),
        JsonValueKind.Number => (typeof(double), element.GetDouble()),
        JsonValueKind.True => (typeof(bool), true),
        JsonValueKind.False => (typeof(bool), false),
        _ => (null, null),
    };

    private static void DefineAutoProperty(TypeBuilder typeBuilder, string name, Type clrType)
    {
        var field = typeBuilder.DefineField($"_{name}", clrType, FieldAttributes.Private);
        var property = typeBuilder.DefineProperty(name, PropertyAttributes.None, clrType, null);

        const MethodAttributes accessorAttributes =
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

        var getter = typeBuilder.DefineMethod($"get_{name}", accessorAttributes, clrType, Type.EmptyTypes);
        var getterIl = getter.GetILGenerator();
        getterIl.Emit(OpCodes.Ldarg_0);
        getterIl.Emit(OpCodes.Ldfld, field);
        getterIl.Emit(OpCodes.Ret);

        var setter = typeBuilder.DefineMethod($"set_{name}", accessorAttributes, null, new[] { clrType });
        var setterIl = setter.GetILGenerator();
        setterIl.Emit(OpCodes.Ldarg_0);
        setterIl.Emit(OpCodes.Ldarg_1);
        setterIl.Emit(OpCodes.Stfld, field);
        setterIl.Emit(OpCodes.Ret);

        property.SetGetMethod(getter);
        property.SetSetMethod(setter);
    }
}
