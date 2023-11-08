Shader "GPU/PointCloudShader"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR0;
            };

            uniform float4x4 _ObjectToWorld;
            uniform StructuredBuffer<float3> _positions;
            uniform float2 _minMaxHeight;

            v2f vert(appdata_base v, uint svInstanceID : SV_InstanceID)
            {
                
                InitIndirectDrawArgs(0);
                v2f o;

                const uint instanceID = GetIndirectInstanceID(svInstanceID);
                
                const float4 wpos = mul(_ObjectToWorld, v.vertex + float4(_positions[instanceID][0], _positions[instanceID][1], _positions[instanceID][2], 0));
                o.pos = mul(UNITY_MATRIX_VP, wpos);

                const float heightDistance = abs(_positions[instanceID][1] - _minMaxHeight[0]);

                const float g = heightDistance < 1.f ? 0.f: 0.75f;
                const float b = 1-g;
                const float r = g * 3.f * (heightDistance/(_minMaxHeight[1]-_minMaxHeight[0]));
                o.color = float4(0.75f*r, g, 0.75f*b, 0.f);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}