using UnityEngine;
using YX;

namespace ST.HUD
{
    /// <summary>
    /// 使用Instanced批量渲染对象
    /// </summary>
    public class HUDInstanced : MonoBehaviour
    {
        [SerializeField]
        private Material _instanceMat;
        [SerializeField]
        private FontRender2Texture _font2Texture;
        
        private Mesh _instanceMesh;

        private Matrix4x4[] _matrices;
        private MaterialPropertyBlock _block;
        public GameObject Cube;
        public GameObject Quad;
        
        public int instanceCount = 500;     // 总实例数量
        
        public ComputeShader frustumCulling;
        private int _kernel;
        private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };

        private struct InstanceData
        {
            public Vector3 position;
            public Color color;
        }
        
        private ComputeBuffer _instanceBuffer;    // 存储所有实例数据
        private ComputeBuffer _visibleBuffer;     // 存储可见实例索引
        private ComputeBuffer _indirectArgsBuffer;// 间接绘制参数

        private void Start()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            {
                _instanceMesh = HudTemplate.GenerateMesh();
                for (int i = 0; i < 500; ++i)
                {
                    _font2Texture.Draw("Test:" + i);
                }
            }
            stopwatch.Stop();
            Debug.LogFormat("初始化字体耗时:{0}ms", stopwatch.ElapsedMilliseconds);

            _instanceMat.SetTexture("_FontTex", _font2Texture.TextureArray);
            
            BuildMatrixAndBlock();
            
            // 1. 创建_instanceBuffer并填充数据
            InstanceData[] instances = new InstanceData[instanceCount];
            for (int i = 0; i < instanceCount; i++)
            {
                instances[i].position = new Vector3(Random.Range(-100f, 100f), 0, Random.Range(-100f, 100f));
                instances[i].color = Random.ColorHSV();
                
                var cube = Instantiate(Cube);
                cube.transform.position = instances[i].position;
                
                var quad = Instantiate(Quad);
                quad.transform.position = instances[i].position;
            }
            int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(InstanceData));
            _instanceBuffer = new ComputeBuffer(instanceCount, stride);
            _instanceBuffer.SetData(instances);

            // 2. 创建_visibleBuffer（最大可见数量=总实例数）
            _visibleBuffer = new ComputeBuffer(instanceCount, sizeof(int), ComputeBufferType.Append);

            // 3. 创建间接绘制参数缓冲区
            _indirectArgsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            _args[0] = _instanceMesh.GetIndexCount(0);  // 网格索引数量
            _args[1] = 0;                       // 可见实例数量（由GPU更新）
            _indirectArgsBuffer.SetData(_args);
            
            // 绑定ComputeShader参数
            _kernel = frustumCulling.FindKernel("FrustumCulling");
            frustumCulling.SetBuffer(_kernel, "_instanceBuffer", _instanceBuffer);
            frustumCulling.SetBuffer(_kernel, "_visibleBuffer", _visibleBuffer);
        }
        void BuildMatrixAndBlock()
        {
            _block = new MaterialPropertyBlock();
            _matrices = new Matrix4x4[500];
            Vector4[] parms = new Vector4[500];
            for (var i = 0; i < 32; i++)
            {
                for (var j = 0; j < 32; j++)
                {
                    var ind = i * 32 + j;
                    if(ind >= 500) break;
                    _matrices[ind] = Matrix4x4.TRS(new Vector3((i - 8) * 2, j - 16, 0), Quaternion.identity, Vector3.one);
                    parms[ind].x = ind / 500f;
                    parms[ind].y = ind;
                }
            }
            _block.SetVectorArray("_Parms", parms);
        }
        void Update()
        {
            _visibleBuffer.SetCounterValue(0);
            
            var viewMatrix = Camera.main.worldToCameraMatrix;
            var projMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
            var vpMatrix = projMatrix * viewMatrix;
            frustumCulling.SetMatrix("_VPMatrix", vpMatrix);
            
            var threadGroups = Mathf.CeilToInt(instanceCount / 64f);
            frustumCulling.Dispatch(_kernel, threadGroups, 1, 1);

            // 将可见实例数量复制到间接参数缓冲区
            ComputeBuffer.CopyCount(_visibleBuffer, _indirectArgsBuffer, sizeof(uint));

            // 渲染可见实例
            _instanceMat.SetBuffer("_instanceBuffer", _instanceBuffer);
            _instanceMat.SetBuffer("_visibleBuffer", _visibleBuffer);
            _instanceMat.SetVector("_TargetDirection", Camera.main.transform.forward);
            Graphics.DrawMeshInstancedIndirect(_instanceMesh, 0, _instanceMat, new Bounds(Vector3.zero, Vector3.one * 100f), _indirectArgsBuffer);
        }
    }
}
