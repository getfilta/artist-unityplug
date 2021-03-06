
Shader "Filta/BackgroundOcclusion"
{
    Properties
    {
        _MainTex ("Replacement Texture", 2D) = "white" {}
        _CameraFeed ("Camera Feed", 2D) = "white" {}
        _OcclusionStencil ("Occlusion Stencil", 2D) = "white" {}
        _UVMultiplierLandScape ("UV MultiplerLandScape", Float) = 0.0
        _UVMultiplierPortrait ("UV MultiplerPortrait", Float) = 1.63 //ratio value
        _UVFlip ("Flip UV", Float) = 0.0
        _ONWIDE("Onwide", Int) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType" = "Transparent" }
        Cull Off ZWrite OFF
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _OcclusionStencil;
            float4 _OcclusionStencil_ST;
            sampler2D _CameraFeed;
            float4 _CameraFeed_ST;
            float _UVMultiplierLandScape;
            float _UVMultiplierPortrait;
            float _UVFlip;
            int _ONWIDE;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                if(_ONWIDE == 1)
                {
                    o.uv1 = float2(v.uv.x, (1.0 - (_UVMultiplierLandScape * 0.5f)) + (v.uv.y / _UVMultiplierLandScape));
                    o.uv2 = float2(lerp(1.0 - o.uv1.x, o.uv1.x, _UVFlip), lerp(o.uv1.y, 1.0 - o.uv1.y, _UVFlip));
                }
                else
                {
                    o.uv1 = float2((1.0 - (_UVMultiplierPortrait * 0.5f)) + (v.uv.x / _UVMultiplierPortrait), v.uv.y);
                    o.uv2 = float2(lerp(1.0 - o.uv1.y, o.uv1.y, 0), lerp(o.uv1.x, 1.0 - o.uv1.x, 1));
                }
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 cameraFeedCol = tex2D(_CameraFeed, i.uv * _CameraFeed_ST.xy + _CameraFeed_ST.zw);
                fixed4 col = tex2D(_MainTex, i.uv1 * _MainTex_ST.xy + _MainTex_ST.zw);
                float4 stencilCol = tex2D(_OcclusionStencil, i.uv2 * _OcclusionStencil_ST.xy + _OcclusionStencil_ST.zw);
                float showOccluder = (stencilCol.r - 1) * -1 ;

                return lerp(cameraFeedCol, col, showOccluder);
            }

            ENDCG
        }
    }
}