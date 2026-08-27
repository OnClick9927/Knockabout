Shader "Custom/HealthBar"
{
    Properties
    {
        _FixedSize ("Fixed Screen Size", Range(0.001, 0.1)) = 0.02
        _BorderSize("_BorderSize",Range(0.0001,0.3))=0.1
        _BGColor("_BGColor",Color)=(1,1,1,1)
        _ValueColor("_ValueColor",Color)=(0,1,0,1)
        _BorderColor("_BorderColor",Color)=(0.5,0.5,0.5,0.5)
        _Value ("Value", Float) = 1
        _Max ("Max", Float) = 5

    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        blend srcAlpha OneMinusSrcAlpha
        // LOD 100
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 scaleXY:TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float _FixedSize,_BorderSize;
            fixed4 _BGColor,_ValueColor,_BorderColor;
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Value)
                UNITY_DEFINE_INSTANCED_PROP(float, _Max)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 worldPos = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;

                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;

                float dist = distance(worldPos, _WorldSpaceCameraPos);

                // 物体自身缩放
                float3 objScale = float3(
                    length(unity_ObjectToWorld[0].xyz),
                    length(unity_ObjectToWorld[1].xyz),
                    length(unity_ObjectToWorld[2].xyz)
                );

                float3 scaledVertex = v.vertex.xyz * objScale * dist * _FixedSize;

                float3 finalWorldPos = worldPos 
                    + camRight * scaledVertex.x 
                    + camUp * scaledVertex.y;

                o.vertex = mul(UNITY_MATRIX_VP, float4(finalWorldPos, 1));
                o.uv = v.uv;
                o.scaleXY=objScale.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                float value = UNITY_ACCESS_INSTANCED_PROP(Props, _Value);
                float max = UNITY_ACCESS_INSTANCED_PROP(Props, _Max);
                float progress = value / max;

                float width=_BorderSize/i.scaleXY.x;
                float height=_BorderSize/i.scaleXY.y;
                float width_border= 1- step(abs(clamp(i.uv.x,width,1-width)-i.uv.x),0);
                float height_border= 1- step(abs(clamp(i.uv.y,height,1-height)-i.uv.y),0);
                float border=saturate(width_border+height_border);
                fixed4 col = lerp(_ValueColor, _BGColor, step(progress, i.uv.x));
                return lerp(col,_BorderColor,border);
            }
            ENDCG
        }
    }
}