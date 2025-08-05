using Biohazrd;
using Biohazrd.Transformation;

namespace PortAudioNet.Generator;

// Workaround for https://github.com/MochiLibraries/Biohazrd/issues/115
internal sealed class Issue115WorkaroundTransformation : TransformationBase
{
    protected override TransformationResult TransformParameter(TransformationContext context, TranslatedParameter declaration)
    {
        if (declaration.Type is PointerTypeReference { Inner: FunctionPointerTypeReference functionPointer })
        {
            return declaration with
            {
                Type = functionPointer,
                Diagnostics = declaration.Diagnostics.Add(Severity.Note, "Manually fixed function pointer type."),
            };
        }

        return declaration;
    }
}
