using Biohazrd;
using Biohazrd.CSharp;
using Biohazrd.OutputGeneration;
using Biohazrd.Transformation.Common;
using Biohazrd.Utilities;
using PortAudioNet.Generator;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("    PortAudioNet.Generator <path-to-portaudio-source> <path-to-portaudio-lib> <path-to-output>");
    return 1;
}

string portAudioSourceDirectoryPath = Path.GetFullPath(args[0]);
string portAudioHeaderFilePath = Path.Combine(portAudioSourceDirectoryPath, "include", "portaudio.h");

string portAudioLibraryFilePath = Path.GetFullPath(args[1]);

string outputDirectoryPath = Path.GetFullPath(args[2]);

if (!Directory.Exists(portAudioSourceDirectoryPath))
{
    Console.Error.WriteLine($"PortAudio directory '{portAudioSourceDirectoryPath}' not found.");
    return 1;
}

if (!File.Exists(portAudioHeaderFilePath))
{
    Console.Error.WriteLine($"PortAudio header file not found at '{portAudioHeaderFilePath}'.");
    return 1;
}

if (!File.Exists(portAudioLibraryFilePath))
{
    Console.Error.WriteLine($"PortAudio library '{portAudioLibraryFilePath}' not found. Has PortAudio been built?");
    return 1;
}

// Create the library
TranslatedLibraryBuilder libraryBuilder = new()
{
    Options = new TranslationOptions()
    {
        // The only template that appears on the public API is ImVector<T>, which we special-case as a C# generic.
        // ImPool<T>, ImChunkStream<T>, and ImSpan<T> do appear on the internal API but for now we just want them to be dropped.
        //TODO: In theory this could be made to work, but there's a few wrinkles that need to be ironed out and these few API points are not a high priority.
        EnableTemplateSupport = false,
    }
};
libraryBuilder.AddCommandLineArgument("--language=c++");
libraryBuilder.AddCommandLineArgument($"-DPA_LITTLE_ENDIAN");
libraryBuilder.AddCommandLineArgument($"-D_CRT_SECURE_NO_WARNINGS");
//libraryBuilder.AddCommandLineArgument($"-DPA_USE_JACK=1");
//libraryBuilder.AddCommandLineArgument($"-DPA_USE_ASIO=1");
libraryBuilder.AddCommandLineArgument($"-DPA_USE_DS=1");
libraryBuilder.AddCommandLineArgument($"-DPAWIN_USE_DIRECTSOUNDFULLDUPLEXCREATE");
libraryBuilder.AddCommandLineArgument($"-DPA_USE_WMME=1");
libraryBuilder.AddCommandLineArgument($"-DPA_USE_WASAPI=1");
libraryBuilder.AddCommandLineArgument($"-DPA_USE_WDMKS=1");
libraryBuilder.AddCommandLineArgument($"-DPAWIN_USE_WDMKS_DEVICE_INFO");
//libraryBuilder.AddCommandLineArgument($"-DPA_USE_COREAUDIO=1");
//libraryBuilder.AddCommandLineArgument($"-DPA_USE_ALSA=1");
//libraryBuilder.AddCommandLineArgument($"-DPA_USE_OSS=1");
//libraryBuilder.AddCommandLineArgument($"-DPA_USE_AUDIOIO=1");
//libraryBuilder.AddCommandLineArgument($"-DPA_USE_PULSEAUDIO=1");
//libraryBuilder.AddCommandLineArgument($"-DPA_USE_SNDIO=1");

libraryBuilder.AddFile(portAudioHeaderFilePath);

TranslatedLibrary library = libraryBuilder.Create();
TranslatedLibraryConstantEvaluator constantEvaluator = libraryBuilder.CreateConstantEvaluator();

// Start output session
using OutputSession outputSession = new()
{
    AutoRenameConflictingFiles = true,
    BaseOutputDirectory = outputDirectoryPath,
    ConservativeFileLogging = false
};

CSharpGenerationOptions generationOptions = CSharpGenerationOptions.Default with
{
    // Biohazrd doesn't officially support .NET Framework 4.7.2 but PortAudio is simple enough that it works so we select .NET 5 as theoretically close enough
    TargetRuntime = TargetRuntime.Net5,
    TargetLanguageVersion = TargetLanguageVersion.CSharp11,
    InfrastructureTypesNamespace = "PortAudioNet.Infrastructure",
};

// Apply transformations
Console.WriteLine("==============================================================================");
Console.WriteLine("Performing library-specific transformations...");
Console.WriteLine("==============================================================================");

// Start
BrokenDeclarationExtractor brokenDeclarationExtractor = new();
library = brokenDeclarationExtractor.Transform(library);

// Eliminate Low-Level C++ Details
library = new RemoveExplicitBitFieldPaddingFieldsTransformation().Transform(library);
library = new AddBaseVTableAliasTransformation().Transform(library);
library = new ConstOverloadRenameTransformation().Transform(library);
library = new MakeEverythingPublicTransformation().Transform(library);

// Type Reduction
library = new CSharpTypeReductionTransformation().Transform(library);
library = new LiftAnonymousRecordFieldsTransformation().Transform(library);

// PortAudioNet-specific
library = new MacroTransformation(constantEvaluator).Transform(library);
library = new PaErrorCodeTransformation().Transform(library);
library = new RemovePrefixesTransformation().Transform(library);
library = new PaStreamHandleTransformation().Transform(library);
library = new NamespaceAllDeclarationsTransformation().Transform(library);

// Typedef Elimination
library = new ResolveTypedefsTransformation().Transform(library);

//TODO: Remove when fixed upstream
library = new Issue115WorkaroundTransformation().Transform(library);

// Finalize Structure
library = new MoveLooseDeclarationsIntoTypesTransformation
(
    (c, d) => "PortAudio"
).Transform(library);

// Generate Function Trampolines
library = new AutoNameUnnamedParametersTransformation().Transform(library);
library = new CreateTrampolinesTransformation()
{
    TargetRuntime = TargetRuntime.Net6
}.Transform(library);

// Apply Final C#-isms
library = new StripUnreferencedLazyDeclarationsTransformation().Transform(library);
library = new DeduplicateNamesTransformation().Transform(library);
library = new OrganizeOutputFilesByNamespaceTransformation("PortAudioNet").Transform(library);
library = new AddTrampolineMethodOptionsTransformation(MethodImplOptions.AggressiveInlining).Transform(library);

// Linking
LinkImportsTransformation linkImports = new()
{
    ErrorOnMissing = true,
    TrackVerboseImportInformation = true,
    WarnOnAmbiguousSymbols = true
};
linkImports.AddLibrary(portAudioLibraryFilePath);
library = linkImports.Transform(library);

// Verification
library = new CSharpTranslationVerifier().Transform(library);
library = brokenDeclarationExtractor.Transform(library);

// Emit the translation
Console.WriteLine("==============================================================================");
Console.WriteLine("Emitting translation...");
Console.WriteLine("==============================================================================");
ImmutableArray<TranslationDiagnostic> generationDiagnostics = CSharpLibraryGenerator.Generate(generationOptions, outputSession, library);

// Write out diagnostics log
DiagnosticWriter diagnostics = new();
diagnostics.AddFrom(library);
diagnostics.AddFrom(brokenDeclarationExtractor);
diagnostics.AddCategory("Generation Diagnostics", generationDiagnostics, "Generation completed successfully");

using StreamWriter diagnosticsOutput = outputSession.Open<StreamWriter>("Diagnostics.log");
diagnostics.WriteOutDiagnostics(diagnosticsOutput, writeToConsole: true);

outputSession.Dispose();
return 0;
