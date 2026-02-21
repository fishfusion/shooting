Shader "Custom/WardCloth"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.04, 0.11, 0.32, 1)
        _SpecularColor ("Specular Color", Color) = (0.88, 0.92, 1.0, 1)
        _SpecularStrength ("Specular Strength", Range(0, 2)) = 0.95
        _RoughnessX ("Roughness X", Range(0.02, 1.0)) = 0.22
        _RoughnessY ("Roughness Y", Range(0.02, 1.0)) = 0.62
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Color;
            fixed4 _SpecularColor;
            float _SpecularStrength;
            float _RoughnessX;
            float _RoughnessY;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 N : TEXCOORD1;
                float3 T : TEXCOORD2;
                float3 B : TEXCOORD3;
                UNITY_LIGHTING_COORDS(4, 5)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.N = UnityObjectToWorldNormal(v.normal);
                float3 T = UnityObjectToWorldDir(float3(1, 0, 0));
                o.T = normalize(T);
                o.B = normalize(cross(o.N, o.T));
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            float WardSpec(float3 N, float3 T, float3 B, float3 L, float3 V, float ax, float ay)
            {
                float3 H = normalize(L + V);
                float NL = saturate(dot(N, L));
                float NV = saturate(dot(N, V));
                float NH = saturate(dot(N, H));
                if (NL <= 0 || NV <= 0 || NH <= 0) return 0;

                float TH = dot(T, H);
                float BH = dot(B, H);
                float tan2 = (1.0 - NH * NH) / max(1e-4, NH * NH);
                float expo = -(tan2 * ((TH * TH) / max(1e-4, ax * ax) + (BH * BH) / max(1e-4, ay * ay)));
                float denom = 4.0 * 3.14159265 * ax * ay * sqrt(max(1e-4, NL * NV));
                return exp(expo) / max(1e-4, denom);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.N);
                float3 T = normalize(i.T);
                float3 B = normalize(i.B);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

                float NdotL = saturate(dot(N, L));
                float3 diffuse = _Color.rgb * _LightColor0.rgb * NdotL;
                float ward = WardSpec(N, T, B, L, V, _RoughnessX, _RoughnessY);
                float3 spec = _SpecularColor.rgb * _LightColor0.rgb * ward * _SpecularStrength;

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;
                float3 finalCol = ambient + (diffuse + spec) * atten;
                return fixed4(finalCol, _Color.a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
