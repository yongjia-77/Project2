//  VhsFx © NullTale - https://x.com/NullTale
Shader "Hidden/VolFx/Blit"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 0
        ZTest Always
        ZWrite Off
        ZClip false
        Cull Off
        
        Pass     // 0
        {
            name "Blit"
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            sampler2D    _MainTex;
            
            struct vert_in
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct frag_in
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // =======================================================================
            frag_in vert(vert_in v)
            {
                frag_in o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            half4 frag(frag_in i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.uv);
                return color;
            }
            ENDHLSL
        }
        Pass     // 1
        {
            name "Blit with Alpha"
            
            Blend SrcAlpha OneMinusSrcAlpha
            //Blend Zero OneMinusSrcAlpha
            
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            sampler2D    _MainTex;
            
            struct vert_in
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct frag_in
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // =======================================================================
            frag_in vert(vert_in v)
            {
                frag_in o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            half4 frag(frag_in i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.uv);
                color.a = saturate(color.a);
                return color;
            }
            ENDHLSL
        }
        Pass     // 2
        {
            name "Depth"
            
            ZTest Always ZWrite On ColorMask R
            Cull Off
            
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/CopyDepthPass.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag
                        
            struct vert_in
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct frag_in
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // =======================================================================
            frag_in vert(vert_in v)
            {
                frag_in o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }
            
            float frag(frag_in i) : SV_Depth
            {
                return SampleDepth(i.uv);
            }
            ENDHLSL
        }
    }
}