Shader "Custom/ArrowShade"
{
    Properties
    {
        _Color ("Albedo", Color) = (1,0,0,1)  // Red color by default
        _MainTex ("Albedo (RGB)", 2D) = "red" {}  // Texture for the arrow
        _Glossiness ("Smoothness", Range(0,1)) = 0.5  // Glossiness control
        _Metallic ("Metallic", Range(0,1)) = 0.0  // Metallic control
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }  // Opaque rendering type
        LOD 200
        ZTest Always  // Always render on top of other objects

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;  // UV coordinates for texture sampling
        };

        half _Glossiness;  // Smoothness value
        half _Metallic;    // Metallic value
        fixed4 _Color;     // Base color

        // Instancing support
        UNITY_INSTANCING_BUFFER_START(Props)
            // Add per-instance properties here if needed
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;  // Apply color and texture to Albedo
            o.Metallic = _Metallic;  // Set the metallic value
            o.Smoothness = _Glossiness;  // Set the smoothness
            o.Alpha = c.a;  // Handle alpha if needed
        }
        ENDCG
    }
    FallBack "Diffuse"  // Fallback shader if the system doesn't support the above features
}
