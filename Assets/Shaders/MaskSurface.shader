Shader "Custom/MaskSurface"
{

    SubShader
    {
        Tags {  "RenderType" = "Opaque" 
                "RenderPipeline" = "UniversalPipeline" 
                "Queue" = "Geometry"
            }

        Pass
        {
            ZWrite Off
            ZTest Always
            ColorMask 0

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
        }
    }
}
