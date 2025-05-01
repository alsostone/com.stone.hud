using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ST.HUD
{
    public class HudRenderer : MonoBehaviour
    {
        private static HudRenderer sInstance = default;
        public static HudRenderer Instance
        {
            get
            {
                if (sInstance == null)
                {
                    Debug.LogError("HudRenderer is not initialized. Please add HudRoot gameObject in the scene.");
                }
                return sInstance;
            }
        }
        
        public int maxRenderCount = 102400;
        public Material instanceMat;
        public FontRender2Texture font2Texture;
        public ComputeShader frustumCulling;
        public HudTemplate hudTemplate;
        
        private Camera _frustumCamera;
        private int _kernel;
        private readonly uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
        
        private Mesh _instanceMesh;
        private HudInstanceDataBuffer _instanceDataBuffer;
        private ComputeBuffer _instanceBuffer;              // 存储所有实例数据
        private ComputeBuffer _visibleBuffer;               // 存储可见实例索引
        private ComputeBuffer _indirectArgsBuffer;          // 间接绘制参数
        private Dictionary<string, int> _nameIndexMapping;

        private static readonly int FontTex = Shader.PropertyToID("_FontTex");
        private static readonly int VpMatrix = Shader.PropertyToID("_VPMatrix");
        private static readonly int InstanceBuffer = Shader.PropertyToID("_instanceBuffer");
        private static readonly int VisibleBuffer = Shader.PropertyToID("_visibleBuffer");

        private void Awake()
        {
            _frustumCamera = Camera.main;
            sInstance = this;
        }
        
        public void SetCamara(Camera frustumCamera)
        {
            _frustumCamera = frustumCamera;
        }
        
        public int GetCount()
        {
            return _instanceDataBuffer.Count;
        }
        
        public int TextDraw(string text)
        {
            if (!_nameIndexMapping.TryGetValue(text, out var index))
            {
                index = font2Texture.Draw(text);
                _nameIndexMapping.Add(text, index);
            }
            return index;
        }
        
        public int AddInstance(string text, Vector3 position, float progress = 1f)
        {
            var instanceData = new HudInstanceData
            {
                Visible = 1,
                Position = position,
                NameIndex = TextDraw(text),
                Progress = progress
            };
            return _instanceDataBuffer.Insert(instanceData);
        }
        
        public void RemoveInstance(int index)
        {
            _instanceDataBuffer.Remove(index);
        }

        private void Start()
        {
            _nameIndexMapping = new Dictionary<string, int>(maxRenderCount / 10);
            _instanceDataBuffer = new HudInstanceDataBuffer(maxRenderCount);
            _instanceBuffer = new ComputeBuffer(maxRenderCount, Marshal.SizeOf(typeof(HudInstanceData)));
            _visibleBuffer = new ComputeBuffer(maxRenderCount, sizeof(uint), ComputeBufferType.Append);

            _instanceMesh = hudTemplate.GenerateMesh();
            _indirectArgsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            _args[0] = _instanceMesh.GetIndexCount(0);
            _indirectArgsBuffer.SetData(_args);
            
            _kernel = frustumCulling.FindKernel("FrustumCulling");
            frustumCulling.SetBuffer(_kernel, InstanceBuffer, _instanceBuffer);
            frustumCulling.SetBuffer(_kernel, VisibleBuffer, _visibleBuffer);
            
            instanceMat.SetTexture(FontTex, font2Texture.TextureArray);
            instanceMat.SetBuffer(InstanceBuffer, _instanceBuffer);
            instanceMat.SetBuffer(VisibleBuffer, _visibleBuffer);
        }
        
        private void LateUpdate()
        {
            _visibleBuffer.SetCounterValue(0);
            _instanceDataBuffer.TryAppendData(_instanceBuffer);
            
            var viewMatrix = _frustumCamera.worldToCameraMatrix;
            var projMatrix = GL.GetGPUProjectionMatrix(_frustumCamera.projectionMatrix, false);
            var vpMatrix = projMatrix * viewMatrix;
            frustumCulling.SetMatrix(VpMatrix, vpMatrix);
            
            var threadGroups = Mathf.CeilToInt(maxRenderCount / 64f);
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
