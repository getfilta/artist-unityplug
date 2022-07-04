// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
Shader "Filta/Internal/UnlitScreenspace"{
	//show values to edit in inspector
	Properties{
		_Color ("Tint", Color) = (0, 0, 0, 1)
		_MainTex ("Texture", 2D) = "white" {}
		_SizeX ("SizeX", float) = 1
		_SizeY ("SizeY", float) = 1
		_SizeZ ("SizeZ", float) = 1
	}

	SubShader{
		//the material is completely non-transparent and is rendered at the same time as the other opaque geometry
		Tags{ "RenderType"="Opaque" "Queue"="Geometry"}

		Pass{
			CGPROGRAM

			//include useful shader functions
			#include "UnityCG.cginc"

			//define vertex and fragment shader
			#pragma vertex vert
			#pragma fragment frag

			//texture and transforms of the texture
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
			float _SizeX;
			float _SizeY;
			float _SizeZ;

			float4 _Screen;

			float4x4 _Matrix;


			//tint of the texture
			fixed4 _Color;

			//the object data that's put into the vertex shader
			struct appdata{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			//the data that's used to generate fragments and can be read by the fragment shader
			struct v2f{
				float4 position : SV_POSITION;
				float4 screenPosition : TEXCOORD0;
			};

			//the vertex shader
			v2f vert(appdata v){
				v2f o;
				//convert the vertex positions from object space to clip space so they can be rendered
				//o.position = UnityObjectToClipPos(v.vertex);
				o.position = mul(_Matrix,v.vertex);
				o.screenPosition = ComputeScreenPos(o.position);
				//o.position = mul(_Matrix, float4((v.uv.x * 2.0 - 1.0) * _SizeX, (v.uv.y * 2.0 - 1.0) * _SizeY, 0.5 * _SizeZ, 1.0));
				o.position = UnityObjectToClipPos(float4((v.uv.x * 2.0 - 1.0) * _SizeX, (v.uv.y * 2.0 - 1.0) * _SizeY, 0.5 * _SizeZ, 1.0));
				return o;
			}

			//the fragment shader
			fixed4 frag(v2f i) : SV_TARGET{
                float2 textureCoordinate = i.screenPosition.xy/ i.screenPosition.w;
                float aspect = _Screen.x / _Screen.y;
				float textureAspect = _MainTex_TexelSize.z/_MainTex_TexelSize.w;
				float realAspect = aspect/textureAspect;
                textureCoordinate.x = textureCoordinate.x * realAspect;
                textureCoordinate = TRANSFORM_TEX(textureCoordinate, _MainTex);
				textureCoordinate.x = textureCoordinate.x + ((realAspect - 1) * -0.5);
				fixed4 col = tex2D(_MainTex, textureCoordinate);
				col *= _Color;
				return col;
			}

			ENDCG
		}
	}
}