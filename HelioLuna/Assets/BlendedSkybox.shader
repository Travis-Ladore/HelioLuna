Shader "Custom/Blended Skybox"
{
    Properties
    {
        _DayTex ("Day Cubemap", Cube) = "" {}
        _NightTex ("Night Cubemap", Cube) = "" {}
        _Blend ("Blend", Range(0, 1)) = 0
        _Exposure ("Exposure", Range(0, 8)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            samplerCUBE _DayTex;
            samplerCUBE _NightTex;
            float _Blend;
            float _Exposure;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 dayColor = texCUBE(_DayTex, i.texcoord);
                fixed4 nightColor = texCUBE(_NightTex, i.texcoord);

                fixed4 finalColor = lerp(dayColor, nightColor, _Blend);
                finalColor.rgb *= _Exposure;

                return finalColor;
            }

            ENDCG
        }
    }
}