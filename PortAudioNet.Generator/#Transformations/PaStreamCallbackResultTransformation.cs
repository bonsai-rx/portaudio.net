using Biohazrd;
using Biohazrd.CSharp;
using Biohazrd.Transformation;

namespace PortAudioNet.Generator;

internal sealed class PaStreamCallbackResultTransformation : TransformationBase
{
    private TranslatedEnum? PaStreamCallbackResult;

    protected override TranslatedLibrary PreTransformLibrary(TranslatedLibrary library)
    {
        PaStreamCallbackResult = null;
        foreach (TranslatedDeclaration declaration in library.EnumerateRecursively())
        {
            if (declaration is TranslatedEnum { Name: nameof(PaStreamCallbackResult) } translatedEnum)
            {
                PaStreamCallbackResult = translatedEnum;
                break;
            }
        }

        return library;
    }

    protected override TransformationResult TransformTypedef(TransformationContext context, TranslatedTypedef declaration)
    {
        if (declaration is { Name: "PaStreamCallback", UnderlyingType: FunctionPointerTypeReference functionPointer })
        {
            if (PaStreamCallbackResult is null || functionPointer.ReturnType != CSharpBuiltinType.Int)
                return declaration;

            return declaration with
            {
                UnderlyingType = functionPointer with
                {
                    ReturnType = TranslatedTypeReference.Create(PaStreamCallbackResult)
                }
            };
        }

        return declaration;
    }
}
