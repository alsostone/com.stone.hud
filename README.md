# com.stone.hud
# 如何使用
1. 把Template/HUDRoot拖入场景中
2. 添加HUD调用HudRenderer.Instance.AddInstance方法
3. 移除HUD调用HudRenderer.Instance.RemoveInstance方法

# 性能测试
- 1000个或10000个HUD实例，性能上没有差别
- 在mac测试机上几十万HUD实例性能几乎没有下降
![性能](img/performance.gif)

- 摄像机视椎剔除使用ComputeShader
![摄像机剔除](img/frustumCulling.gif)

- 实际效果
![摄像机剔除](img/blood.gif)