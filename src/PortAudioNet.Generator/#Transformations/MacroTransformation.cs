using Biohazrd;
using Biohazrd.Expressions;
using Biohazrd.Transformation;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PortAudioNet.Generator;

internal sealed class MacroTransformation : TransformationBase
{
    private readonly TranslatedLibraryConstantEvaluator ConstantEvaluator;

    private readonly HashSet<TranslatedMacro> UsedMacros = new();
    private ImmutableDictionary<string, TranslatedMacro> AllMacros = ImmutableDictionary<string, TranslatedMacro>.Empty;

    public MacroTransformation(TranslatedLibraryConstantEvaluator constantEvaluator)
        => ConstantEvaluator = constantEvaluator;

    protected override TranslatedLibrary PreTransformLibrary(TranslatedLibrary library)
    {
        Debug.Assert(UsedMacros.Count == 0);
        UsedMacros.Clear();

        Debug.Assert(AllMacros.Count == 0);
        ImmutableDictionary<string, TranslatedMacro>.Builder builder = ImmutableDictionary.CreateBuilder<string, TranslatedMacro>();
        foreach (TranslatedMacro macro in library.Macros)
            builder.Add(macro.Name, macro);
        AllMacros = builder.ToImmutable();

        // Synthesize constants from macros
        List<TranslatedDeclaration> newDeclarations = new();
        List<TranslationDiagnostic> newDiagnostics = new();

        List<TranslatedMacro> constantMacros = new();
        List<string> constantMacroEvaluationStrings = new();

        void AddConstant(string? cast, string macroName)
        {
            if (!AllMacros.TryGetValue(macroName, out TranslatedMacro? macro))
            {
                newDiagnostics.Add(Severity.Error, $"Macro constant '{macroName}' does not exist and will not be emitted.");
                return;
            }

            constantMacros.Add(macro);
            constantMacroEvaluationStrings.Add(cast is null ? macro.Name : $"{cast}{macro.Name}");
        }

        AddConstant(null, "paNoDevice");
        AddConstant(null, "paUseHostApiSpecificDeviceSpecification");
        AddConstant(null, "paFormatIsSupported");
        AddConstant("(unsigned long)", "paFramesPerBufferUnspecified");

        ImmutableArray<ConstantEvaluationResult> values = ConstantEvaluator.EvaluateBatch(constantMacroEvaluationStrings);
        Debug.Assert(values.Length == constantMacroEvaluationStrings.Count && values.Length == constantMacros.Count);

        for (int i = 0; i < constantMacros.Count; i++)
        {
            TranslatedMacro macro = constantMacros[i];
            UsedMacros.Add(macro);
            ConstantEvaluationResult result = values[i];

            if (result.Value is not null)
            {
                TranslatedConstant synthesizedConstant = new(macro.Name, result);
                newDeclarations.Add(synthesizedConstant);
            }
            else
            {
                newDiagnostics.Add(Severity.Error, $"Could not synthesize constant for macro '{macro.Name}', evaluating it did not produce a constant value.");
                newDiagnostics.AddRange(result.Diagnostics);
            }
        }

        return library with
        {
            Declarations = library.Declarations.AddRange(newDeclarations),
            ParsingDiagnostics = library.ParsingDiagnostics.AddRange(newDiagnostics),
        };
    }

    protected override TransformationResult TransformTypedef(TransformationContext context, TranslatedTypedef declaration)
    {
        // Check if this typedef should be promoted to an enum made of macros
        (bool isFlags, string[]? valueNames) = declaration.Name switch
        {
            "PaSampleFormat" => (true, new[] { "paFloat32", "paInt32", "paInt24", "paInt16", "paInt8", "paUInt8", "paCustomFormat", "paNonInterleaved" }),
            "PaStreamFlags" => (true, new[] { "paNoFlag", "paClipOff", "paDitherOff", "paNeverDropInput", "paPrimeOutputBuffersUsingStreamCallback", "paPlatformSpecificFlags" }),
            "PaStreamCallbackFlags" => (true, new[] { "paInputUnderflow", "paInputOverflow", "paOutputUnderflow", "paOutputOverflow", "paPrimingOutput" }),
            _ => (false, null),
        };

        if (valueNames is null)
            return declaration;

        // Evaluate macros to create enum constants
        ImmutableList<TranslatedEnumConstant>.Builder valuesBuilder = ImmutableList.CreateBuilder<TranslatedEnumConstant>();
        ImmutableArray<TranslationDiagnostic>.Builder enumDiagnosticsBuilder = ImmutableArray.CreateBuilder<TranslationDiagnostic>();

        List<TranslatedMacro> macros = new(valueNames.Length);
        foreach (string value in valueNames)
        {
            if (AllMacros.TryGetValue(value, out TranslatedMacro? macro))
            {
                macros.Add(macro);
                UsedMacros.Add(macro);
            }
            else
            { enumDiagnosticsBuilder.Add(Severity.Error, $"Macro '{value}' for enum '{declaration.Name}' does not exist and will not be emitted."); }
        }

        ImmutableArray<ConstantEvaluationResult> evaluatedValues = ConstantEvaluator.EvaluateBatch(macros);
        Debug.Assert(evaluatedValues.Length == macros.Count);

        for (int i = 0; i < macros.Count; i++)
        {
            TranslatedMacro macro = macros[i];
            ConstantEvaluationResult result = evaluatedValues[i];

            if (result.Value is null)
            { enumDiagnosticsBuilder.AddRange(result.Diagnostics); }
            else if (result.Value is not IntegerConstant integerValue)
            { enumDiagnosticsBuilder.Add(Severity.Error, $"Could not make enum value from '{macro.Name}' because it evaluated to a {result.Value.GetType()}."); }
            else
            {
                TranslatedEnumConstant enumValue = new(macro.Name, integerValue.Value)
                {
                    HasExplicitValue = true,
                    IsHexValue = isFlags,
                    Accessibility = AccessModifier.Public,
                    Diagnostics = result.Diagnostics,
                };
                valuesBuilder.Add(enumValue);
            }
        }

        // Add a `None` value to flags enums if necessary
        if (isFlags && !valuesBuilder.Any(c => c.Value == 0) && !valuesBuilder.Any(c => c.Name == "None"))
        {
            valuesBuilder.Insert(0, new TranslatedEnumConstant("None", 0)
            {
                HasExplicitValue = true,
                IsHexValue = true,
                Accessibility = AccessModifier.Public
            });
        }

        // Synthesize the enum
        return new TranslatedEnum(declaration.Name, declaration.UnderlyingType)
        {
            IsFlags = isFlags,
            Accessibility = AccessModifier.Public,
            Diagnostics = enumDiagnosticsBuilder.MoveToImmutableSafe(),
            Values = valuesBuilder.ToImmutable(),
            ReplacedDeclarations = [declaration],
        };
    }

    protected override TranslatedLibrary PostTransformLibrary(TranslatedLibrary library)
    {
        List<TranslationDiagnostic> newDiagnostics = new();

        TranslatedFile? wasapiHeader = library.Files.FirstOrDefault(f => Path.GetFileName(f.FilePath) == "pa_win_wasapi.h");

        // Emit diagnostics for unused non-function macros
        foreach (TranslatedMacro macro in library.Macros)
        {
            if (macro.IsFunctionLike || macro.WasUndefined || macro.IsUsedForHeaderGuard || UsedMacros.Contains(macro))
                continue;

            // Ignore macros from pa_win_waspi.h, none of them are relevant
            if (macro.File == wasapiHeader)
                continue;

            newDiagnostics.Add(Severity.Warning, $"Constant-like macro '{macro.Name}' went unused, it may need to be added to {nameof(MacroTransformation)}.");
        }

        // Emit library with any new top-level diagnostics
        return library with
        {
            ParsingDiagnostics = library.ParsingDiagnostics.AddRange(newDiagnostics)
        };
    }
}
