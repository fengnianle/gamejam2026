// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Gaussian Blur" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BlurSize ("Blur Size", Float) = 1.0
	}
	SubShader {
		// 第一次用 CGINCLUDE，不需要包含Pass语义块
		CGINCLUDE
		
		#include "UnityCG.cginc"
		
		sampler2D _MainTex;  
		half4 _MainTex_TexelSize;
		float _BlurSize;
		  
		struct v2f {
			float4 pos : SV_POSITION;
			half2 uv[5]: TEXCOORD0; // 5*5维高斯核可以拆分成两个大小为5维高斯核，只需要计算5个纹理坐标即可
		};

		// 把计算采样纹理坐标的代码从片元着色器中转移到顶点着色器中，可以减少运算，提高性能
		 // 从顶点若色器到片元右色器的插值是线性的，因此这样的转移并不会影响纹理坐标的计算结果。
		v2f vertBlurVertical(appdata_img v) { 
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			
			half2 uv = v.texcoord;
			
			// 和BlurSize相乘来控制采样距离。在高斯核维数不变的情况下， BlurSize 越大，模糊程度越高但采样数却不会受到影响。但过大的 BlurSize 值会造成虚影
			
			o.uv[0] = uv; // 当前采样纹理
			o.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize; // 邻域
			o.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
			o.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
			o.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
					 
			return o;
		}
		
		v2f vertBlurHorizontal(appdata_img v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			
			half2 uv = v.texcoord;
			
			o.uv[0] = uv;
			o.uv[1] = uv + float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
			o.uv[2] = uv - float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
			o.uv[3] = uv + float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
			o.uv[4] = uv - float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
					 
			return o;
		}
		
		fixed4 fragBlur(v2f i) : SV_Target {
			float weight[3] = {0.4026, 0.2442, 0.0545}; // 对称性，我们只需要记录 3 个高斯权重
			
			fixed3 sum = tex2D(_MainTex, i.uv[0]).rgb * weight[0]; // 当前像素*权重值
			
			for (int it = 1; it < 3; it++) { // 两次迭代
				sum += tex2D(_MainTex, i.uv[it*2-1]).rgb * weight[it];
				sum += tex2D(_MainTex, i.uv[it*2]).rgb * weight[it];
			}
			
			return fixed4(sum, 1.0);
		}
		    
		ENDCG
		
		ZTest Always Cull Off ZWrite Off
		
		Pass {
			// 设置了渲染状态，name语义
			// 因为高斯模糊是非常常见的图像处理操作，在其他Shader 中直接通过它们的名字来使用该 Pass, 而不需要再重复编写代码NAME "GAUSSIAN_BLUR_VERTICAL"
			NAME "GAUSSIAN_BLUR_VERTICAL"

			CGPROGRAM
			
			#pragma vertex vertBlurVertical  
			#pragma fragment fragBlur
			  
			ENDCG  
		}
		
		Pass {  
			NAME "GAUSSIAN_BLUR_HORIZONTAL" // 方便别人调用该Pass
			
			CGPROGRAM  
			
			#pragma vertex vertBlurHorizontal  
			#pragma fragment fragBlur
			
			ENDCG
		}
	} 
	FallBack Off
}

