
//=======================================================
// 作者：hannibal
// 描述：hud instance
//=======================================================
using System.Collections.Generic;
using UnityEngine;

namespace YX
{
    /// <summary>
    /// 使用Instanced批量渲染对象
    /// </summary>
    [RequireComponent(typeof(HUDMeshBuild))]
    public class HUDInstanced : MonoBehaviour
    {
        [SerializeField]
        private Material _instanceMat;
        [SerializeField]
        private FontRender2Texture _font2Texture;
        
        private HUDMeshBuild _meshBuild;
        private Mesh _instanceMesh;

        private Matrix4x4[] _matrices;
        private MaterialPropertyBlock _block;
        public GameObject Cube;
        public GameObject Quad;
        
        public int instanceCount = 500;     // 总实例数量
        public float spawnRadius = 50f;      // 实例生成范围
        public ComputeShader computeShader;  // 筛选可见实例的ComputeShader
        
        // 实例数据结构体（需与Shader内存布局匹配）
        private struct InstanceData
        {
            public Vector3 position;
            public Color color;
        }
        
        private ComputeBuffer _instanceBuffer;    // 存储所有实例数据
        private ComputeBuffer _visibleBuffer;     // 存储可见实例索引
        private ComputeBuffer _indirectArgsBuffer;// 间接绘制参数

        private int _kernel;                      // ComputeShader内核ID
        private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
        
        private void Awake()
        {
            _meshBuild = GetComponent<HUDMeshBuild>();
        }

        private void Start()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            {
                _instanceMesh = _meshBuild.BuildMesh();
                for (int i = 0; i < 500; ++i)
                {
                    _font2Texture.Draw("Test:" + i);
                }
            }
            stopwatch.Stop();
            Debug.LogFormat("初始化字体耗时:{0}ms", stopwatch.ElapsedMilliseconds);

            _instanceMat.SetTexture("_FontTex", _font2Texture.TextureArray);
            _instanceMat.SetVector("_PivotPoint", new Vector3(3.5f * 0.5f, 0.4f, 0));
            
            BuildMatrixAndBlock();
            
            // 1. 创建_instanceBuffer并填充数据
            InstanceData[] instances = new InstanceData[instanceCount];
            for (int i = 0; i < instanceCount; i++)
            {
                instances[i].position = new Vector3(Random.Range(-100f, 100f), 0, Random.Range(-100f, 100f));
                instances[i].color = Random.ColorHSV();
                var cube = Instantiate(Cube);
                cube.transform.position = instances[i].position + Vector3.down * 0.5f;
                
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
            _kernel = computeShader.FindKernel("CSMain");
            computeShader.SetBuffer(_kernel, "_instanceBuffer", _instanceBuffer);
            computeShader.SetBuffer(_kernel, "_visibleBuffer", _visibleBuffer);
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
            //Graphics.DrawMeshInstanced(_instanceMesh, 0, _instanceMat, _matrices, 500, _block, UnityEngine.Rendering.ShadowCastingMode.Off, false);
            
            
            // 每帧重置可见缓冲区并调度ComputeShader
            _visibleBuffer.SetCounterValue(0);  // 重置Append Buffer的计数器
        
            // 传递视锥体参数
            Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
            Matrix4x4 vpMatrix = projMatrix * viewMatrix;
            
            computeShader.SetMatrix("_VPMatrix", vpMatrix);
            
            // 传递参数到ComputeShader（例如摄像机位置、视锥体等）
            computeShader.SetFloat("_Time", Time.time);
        
            // 调度ComputeShader线程组（每个线程处理一个实例）
            int threadGroups = Mathf.CeilToInt(instanceCount / 64f);
            computeShader.Dispatch(_kernel, threadGroups, 1, 1);

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
