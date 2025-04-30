using System.Runtime.InteropServices;
using UnityEngine;
using YX;
using Random = UnityEngine.Random;

namespace ST.HUD
{
    public class HudRenderer : MonoBehaviour
    {
        public int MAX_RENDER_COUNT = 102400;
        public Material instanceMat;
        public FontRender2Texture font2Texture;
        
        private Matrix4x4[] _matrices;
        private MaterialPropertyBlock _block;
        public GameObject Cube;

        public Camera frustumCamera;
        public ComputeShader frustumCulling;
        
        private int _kernel;
        private readonly uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
        
        private Mesh _instanceMesh;
        private HudInstanceDataBuffer _instanceDataBuffer;
        private ComputeBuffer _instanceBuffer;              // 存储所有实例数据
        private ComputeBuffer _visibleBuffer;               // 存储可见实例索引
        private ComputeBuffer _indirectArgsBuffer;          // 间接绘制参数
        
        private static readonly int FontTex = Shader.PropertyToID("_FontTex");
        private static readonly int VpMatrix = Shader.PropertyToID("_VPMatrix");
        private static readonly int InstanceBuffer = Shader.PropertyToID("_instanceBuffer");
        private static readonly int VisibleBuffer = Shader.PropertyToID("_visibleBuffer");

        private void Start()
        {
            _instanceDataBuffer = new HudInstanceDataBuffer(MAX_RENDER_COUNT);
            _instanceBuffer = new ComputeBuffer(MAX_RENDER_COUNT, Marshal.SizeOf(typeof(HudInstanceData)));
            _visibleBuffer = new ComputeBuffer(MAX_RENDER_COUNT, sizeof(uint), ComputeBufferType.Append);

            _instanceMesh = HudTemplate.GenerateMesh();
            _indirectArgsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            _args[0] = _instanceMesh.GetIndexCount(0);
            _indirectArgsBuffer.SetData(_args);
            
            _kernel = frustumCulling.FindKernel("FrustumCulling");
            frustumCulling.SetBuffer(_kernel, InstanceBuffer, _instanceBuffer);
            frustumCulling.SetBuffer(_kernel, VisibleBuffer, _visibleBuffer);
            
            instanceMat.SetTexture(FontTex, font2Texture.TextureArray);
            instanceMat.SetBuffer(InstanceBuffer, _instanceBuffer);
            instanceMat.SetBuffer(VisibleBuffer, _visibleBuffer);

            BuildMatrixAndBlock();
        }
        
        void BuildMatrixAndBlock()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            {
                for (int i = 0; i < 500; ++i)
                {
                    font2Texture.Draw("Test:" + i);
                }
            }
            stopwatch.Stop();
            Debug.LogFormat("初始化字体耗时:{0}ms", stopwatch.ElapsedMilliseconds);
            
            for (int i = 0; i < MAX_RENDER_COUNT / 10; i++)
            {
                var data = new HudInstanceData();
                data.position = new Vector3(Random.Range(-100f, 100f), 0, Random.Range(-100f, 100f));
                data.progress = Random.Range(0, 1);
                _instanceDataBuffer.Insert(data);
                //
                // var cube = Instantiate(Cube);
                // cube.transform.position = data.position + Vector3.down;
            }
            //
            //
            // _block = new MaterialPropertyBlock();
            // _matrices = new Matrix4x4[500];
            // Vector4[] parms = new Vector4[500];
            // for (var i = 0; i < 32; i++)
            // {
            //     for (var j = 0; j < 32; j++)
            //     {
            //         var ind = i * 32 + j;
            //         if(ind >= 500) break;
            //         _matrices[ind] = Matrix4x4.TRS(new Vector3((i - 8) * 2, j - 16, 0), Quaternion.identity, Vector3.one);
            //         parms[ind].x = ind / 500f;
            //         parms[ind].y = ind;
            //     }
            // }
            // _block.SetVectorArray("_Parms", parms);
        }
        
        void LateUpdate()
        {
            _visibleBuffer.SetCounterValue(0);
            _instanceDataBuffer.TryAppendData(_instanceBuffer);
            
            var viewMatrix = frustumCamera.worldToCameraMatrix;
            var projMatrix = GL.GetGPUProjectionMatrix(frustumCamera.projectionMatrix, false);
            var vpMatrix = projMatrix * viewMatrix;
            frustumCulling.SetMatrix(VpMatrix, vpMatrix);
            
            var threadGroups = Mathf.CeilToInt(MAX_RENDER_COUNT / 64f);
            frustumCulling.Dispatch(_kernel, threadGroups, 1, 1);
            ComputeBuffer.CopyCount(_visibleBuffer, _indirectArgsBuffer, sizeof(uint));
            
            Graphics.DrawMeshInstancedIndirect(_instanceMesh, 0, instanceMat, new Bounds(Vector3.zero, Vector3.one * 100f), _indirectArgsBuffer);
        }

        private void OnDestroy()
        {
            _instanceBuffer.Dispose();
            _visibleBuffer.Dispose();
            _indirectArgsBuffer.Dispose();
        }
    }
}
