Shader "Custom/SpatialMagnifier"
{
    Properties
    {
        _Tint ("Tint (rgb) & Edge Intensity (a)", Color) = (1, 1, 1, 0.6)
        _Magnification ("Magnification", Range(1.0, 4.0)) = 1.5
        _Ior ("Index of Refraction", Range(1.0, 2.0)) = 1.2
        _Radius ("Lens Radius (world units)", Float) = 0.08
        _Thickness ("Lens Thickness (world units)", Float) = 0.01
        _EdgeFeather ("Edge Feather", Range(0.0, 1.0)) = 0.25
    }

    SubShader
    {
        Tags { 
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "Magnifier"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _STEREO_MULTIVIEW _STEREO_INSTANCING _SINGLE_PASS_STEREO

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos : TEXCOORD0; // for screen uv
                float3 positionWS : TEXCOORD1; // for local lens coords
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; // rgb tint, a edge intensity
                float _Magnification; // 1..4
                float _Ior; // ~1.2 for glass
                float _Radius; // lens radius in object/world units (match your mesh scale)
                float _Thickness; // lens bulge height at center
                float _EdgeFeather; // 0..1, normalized to radius
            CBUFFER_END

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionWS = positionWS;
                o.positionHCS = TransformWorldToHClip(positionWS);
                o.screenPos = ComputeScreenPos(o.positionHCS);
                o.uv = v.uv;
                return o;
            }


            // Stereo-correct screen uv
            float2 GetScreenUV(float4 screenPos)
            {
                float2 uv = screenPos.xy / screenPos.w;
                // #if defined(UNITY_SINGLE_PASS_STEREO)
                //     uv = UnityStereoTransformScreenSpaceTex(uv);
                // #endif
                return uv;
            }


            // Soft circular mask: 1 inside, 0 outside
            float softCircle(float2 p, float r, float feather)
            {
                // distance from center
                float d = length(p);
                // feather is in world/object units; convert to soft edge
                return saturate(1.0 - smoothstep(r - feather, r, d));
            }


            half4 frag (Varyings i) : SV_Target
            {
                // Object-space position of this fragment (reconstruct via world->object)
                float3 posOS = TransformWorldToObject(i.positionWS);
                float2 lensXY = posOS.xy; // assume lens lies in object XY plane
                
                
                float r = max(1e-4, _Radius);
                float feather = saturate(_EdgeFeather) * r; // convert 0..1 to world units
                
                
                // Edge mask for nice falloff
                float mask = softCircle(lensXY, r, max(1e-4, feather));
                if (mask <= 0.001)
                {
                    // early out: fully transparent（裁掉透镜外）
                    return half4(0,0,0,0.3);
                }


                // Approximate a spherical cap to derive a pseudo normal of the lens surface
                // z = sqrt(r^2 - x^2 - y^2) - (r - thickness)
                float rr = r * r;
                float x2y2 = saturate(1.0 - (dot(lensXY, lensXY) / rr));
                float z = sqrt(max(0.0, rr * x2y2)) - (r - _Thickness);


                float3 n = normalize(float3(lensXY * (_Thickness / r), z));


                // View direction: use -Z in view/tangent approximation (screen-space refraction hack)
                float3 V = float3(0,0,-1);
                float eta = 1.0 / max(1.0001, _Ior);
                float3 refr = refract(-V, n, eta);


                float2 screenUV = GetScreenUV(i.screenPos);


                // Sample scene depth to reduce parallax artifacts with distance
                float rawDepth = SampleSceneDepth(screenUV);
                float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);


                // Convert refraction vector into a screen-space offset, scaled by magnification
                float2 offset = refr.xy * (_Magnification - 1.0);
                offset /= max(1.0, eyeDepth); // distant pixels offset less


                float2 uv = screenUV + offset * mask; // blend to edges


                float4 sceneCol = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);

                // 如果编辑器下没有 OpaqueTexture，就退化到 _MainTex
                #if UNITY_EDITOR
                if (sceneCol.a == 0) {
                    sceneCol = tex2D(_MainTex, magnifiedUV);
                }
                #endif

                // Edge tint subtlety
                sceneCol.rgb = lerp(sceneCol.rgb, sceneCol.rgb * _Tint.rgb, _Tint.a * (1.0 - mask));
                sceneCol.a = mask; // alpha follows mask
                return sceneCol;
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
