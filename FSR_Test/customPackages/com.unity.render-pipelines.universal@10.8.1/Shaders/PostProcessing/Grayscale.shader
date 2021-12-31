Shader "Hidden/Universal Render Pipeline/Grayscale"
{
    HLSLINCLUDE
        #include "./Common.hlsl"
        //#include "./StdLib.hlsl"


      TEXTURE2D_X(_SourceTex);
      float _Blend;

      float4 Frag(Varyings i) : SV_Target
      {
          UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            float2 uv = UnityStereoTransformScreenSpaceTex(i.uv);
          float4 color = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv);

          float luminance = dot(color.rgb, float3(0.2126729, 0.7151522, 0.0721750));
          color.rgb = lerp(color.rgb, luminance.xxx, _Blend.xxx);

          return color;
      }
  ENDHLSL
  SubShader
  {
      Cull Off ZWrite Off ZTest Always
      Pass
      {
          HLSLPROGRAM
              #pragma vertex FullscreenVert
              #pragma fragment Frag
          ENDHLSL
      }
  }
}
